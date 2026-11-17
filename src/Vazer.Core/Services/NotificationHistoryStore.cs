using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vazer.Core.Json;
using Vazer.Core.Models;

namespace Vazer.Core.Services;

public sealed class NotificationHistoryStore : IDisposable
{
    private static readonly JsonTypeInfo<NotificationRecordDto> DtoTypeInfo =
        VazJsonContext.Settings.NotificationRecordDto;

    private readonly string _filePath;
    private readonly ILogger<NotificationHistoryStore> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private bool _disposed;

    public NotificationHistoryStore(string? filePath = null, ILogger<NotificationHistoryStore>? logger = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "notifications.json")
            : filePath;
        _logger = logger ?? NullLogger<NotificationHistoryStore>.Instance;
    }

    public string FilePath => _filePath;

    public async Task<NotificationRecordDto> LoadAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
            {
                return new NotificationRecordDto();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new NotificationRecordDto();
                }

                var dto = JsonSerializer.Deserialize(json, DtoTypeInfo);
                if (dto == null)
                {
                    return new NotificationRecordDto();
                }

                if (dto.Version > 1)
                {
                    _logger.LogWarning("notifications.json version {Version} > 1 — chưa có migration system, dùng mặc định", dto.Version);
                    return new NotificationRecordDto();
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "notifications.json không hợp lệ — tạo bản sao .bak và trả về rỗng");
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var bakPath = $"{_filePath}.bak-{timestamp}";
                    File.Copy(_filePath, bakPath, overwrite: true);
                }
                catch (Exception bakEx)
                {
                    _logger.LogWarning(bakEx, "Không thể tạo file backup cho notifications.json hỏng");
                }
                return new NotificationRecordDto();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync(NotificationRecordDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(dto, DtoTypeInfo);
            var tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);
            File.Move(tmpPath, _filePath, overwrite: true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void RequestSave(NotificationRecordDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        lock (_debounceLock)
        {
            if (_disposed) return;

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            var cts = new CancellationTokenSource();
            _debounceCts = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, cts.Token).ConfigureAwait(false);
                    if (!cts.Token.IsCancellationRequested)
                    {
                        await SaveAsync(dto, cts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Debounce cancelled by next request — normal behavior
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu notification history qua debounce");
                }
            });
        }
    }

    public void Dispose()
    {
        lock (_debounceLock)
        {
            if (_disposed) return;
            _disposed = true;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
        _semaphore.Dispose();
    }
}
