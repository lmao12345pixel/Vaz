using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vazer.Core.Models;
using Vazer.Core.Services;
using Xunit;

namespace Vazer.Core.Tests;

/// <summary>
/// NotificationHistoryStore: atomic write, roundtrip, debounce, corrupt JSON backup, concurrency.
/// Dùng temp directory — không đụng %APPDATA% thật.
/// </summary>
public sealed class NotificationHistoryStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _jsonPath;

    public NotificationHistoryStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "vaz-notif-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _jsonPath = Path.Combine(_tempDir, "notifications.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesFieldsCorrectly()
    {
        // 1. SaveAndLoad_RoundTrip: dto 3 items -> save -> new store load -> 3 items, đúng field, callback null
        using var store1 = new NotificationHistoryStore(_jsonPath);
        var originalDto = new NotificationRecordDto
        {
            Version = 1,
            UnreadCount = 2,
            Items =
            {
                new NotificationRecordItem
                {
                    Id = Guid.NewGuid(),
                    Type = "Success",
                    Priority = "Normal",
                    Title = "Mod Installed",
                    Message = "Sodium was installed successfully.",
                    ActionLabel1 = "View",
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationRecordItem
                {
                    Id = Guid.NewGuid(),
                    Type = "Error",
                    Priority = "High",
                    Title = "Download Failed",
                    Message = "Network timeout.",
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationRecordItem
                {
                    Id = Guid.NewGuid(),
                    Type = "Info",
                    Priority = "Low",
                    Title = "Sync Complete",
                    Message = "Profile synchronized.",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        await store1.SaveAsync(originalDto);

        using var store2 = new NotificationHistoryStore(_jsonPath);
        var loaded = await store2.LoadAsync();

        loaded.Version.Should().Be(1);
        loaded.UnreadCount.Should().Be(2);
        loaded.Items.Should().HaveCount(3);

        var first = loaded.Items[0];
        first.Id.Should().Be(originalDto.Items[0].Id);
        first.Type.Should().Be("Success");
        first.Title.Should().Be("Mod Installed");
        first.Message.Should().Be("Sodium was installed successfully.");
        first.ActionLabel1.Should().Be("View");
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsEmptyAndCreatesBakFile()
    {
        // 2. LoadAsync_CorruptJson_ReturnsEmpty: ghi JSON hỏng -> load -> empty, .bak tạo.
        await File.WriteAllTextAsync(_jsonPath, "{ invalid json content: [ truncated ");

        using var store = new NotificationHistoryStore(_jsonPath);
        var loaded = await store.LoadAsync();

        loaded.Should().NotBeNull();
        loaded.Items.Should().BeEmpty();
        loaded.UnreadCount.Should().Be(0);

        var bakFiles = Directory.GetFiles(_tempDir, "notifications.json.bak-*");
        bakFiles.Should().NotBeEmpty("A backup file (.bak-*) must be created on corrupt JSON");
    }

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsEmpty()
    {
        // 3. LoadAsync_NoFile_ReturnsEmpty: file không tồn tại -> empty.
        using var store = new NotificationHistoryStore(_jsonPath);
        var loaded = await store.LoadAsync();

        loaded.Should().NotBeNull();
        loaded.Items.Should().BeEmpty();
        loaded.UnreadCount.Should().Be(0);
        loaded.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_AtomicWrite_LeavesValidFileAndTmpCleaned()
    {
        // 4. SaveAsync_AtomicWrite: atomic via .tmp + File.Move, original is intact or atomically replaced
        using var store = new NotificationHistoryStore(_jsonPath);
        var initialDto = new NotificationRecordDto
        {
            Version = 1,
            UnreadCount = 1,
            Items = { new NotificationRecordItem { Id = Guid.NewGuid(), Title = "Old Item" } }
        };
        await store.SaveAsync(initialDto);

        var updatedDto = new NotificationRecordDto
        {
            Version = 1,
            UnreadCount = 0,
            Items = { new NotificationRecordItem { Id = Guid.NewGuid(), Title = "New Item" } }
        };
        await store.SaveAsync(updatedDto);

        File.Exists(_jsonPath).Should().BeTrue();
        File.Exists(_jsonPath + ".tmp").Should().BeFalse(".tmp file should be moved atomically");

        var loaded = await store.LoadAsync();
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].Title.Should().Be("New Item");
    }

    [Fact]
    public async Task RequestSave_Debounce_CoalescesRapidCallsIntoLastState()
    {
        // 5. RequestSave_Debounce: gọi 5 lần <500ms -> chỉ lượt cuối cùng được ghi
        using var store = new NotificationHistoryStore(_jsonPath);

        for (int i = 1; i <= 5; i++)
        {
            var dto = new NotificationRecordDto
            {
                Version = 1,
                UnreadCount = i,
                Items = { new NotificationRecordItem { Id = Guid.NewGuid(), Title = $"Burst #{i}" } }
            };
            store.RequestSave(dto);
            await Task.Delay(40); // 40ms < 500ms debounce
        }

        // Đợi debounce hoàn tất (> 500ms)
        await Task.Delay(800);

        var loaded = await store.LoadAsync();
        loaded.UnreadCount.Should().Be(5);
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].Title.Should().Be("Burst #5");
    }

    [Fact]
    public async Task SaveAsync_Concurrent_NoRaceCondition()
    {
        // 6. SaveAsync_Concurrent_NoRace: nhiều SaveAsync song song -> SemaphoreSlim tuần tự, không corrupt
        using var store = new NotificationHistoryStore(_jsonPath);

        var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(async () =>
        {
            var dto = new NotificationRecordDto
            {
                Version = 1,
                UnreadCount = i,
                Items = { new NotificationRecordItem { Id = Guid.NewGuid(), Title = $"Concurrent #{i}" } }
            };
            await store.SaveAsync(dto);
        }));

        await Task.WhenAll(tasks);

        var loaded = await store.LoadAsync();
        loaded.Should().NotBeNull();
        loaded.Items.Should().ContainSingle();
        loaded.Items[0].Title.Should().StartWith("Concurrent #");
    }
}
