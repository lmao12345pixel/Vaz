using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI.ViewModels;

public sealed partial class InstanceCardItem : ObservableObject
{
    public MinecraftInstance Instance { get; }
    public string Id => Instance.Id;
    public string Name => Instance.Name;
    public string MinecraftVersion => Instance.MinecraftVersion;
    public string LoaderType => Instance.LoaderType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLaunching))]
    private bool _isLaunching;

    public bool IsNotLaunching => !IsLaunching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusText))]
    private string _statusText = string.Empty;

    public bool HasStatusText => !string.IsNullOrEmpty(StatusText);

    public InstanceCardItem(MinecraftInstance instance) => Instance = instance;
}

public sealed partial class InstancesViewModel : ObservableObject
{
    private readonly IInstanceManager _instances;
    private readonly IAccountStore _accountStore;
    private readonly IGameLauncher _launcher;
    private readonly IJavaResolver _javaResolver;

    public ObservableCollection<InstanceCardItem> Instances { get; } = new();

    public Func<Task>? RequireAccountAction { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstances))]
    [NotifyPropertyChangedFor(nameof(HasNoInstances))]
    private int _instanceCount;

    public bool HasInstances => InstanceCount > 0;
    public bool HasNoInstances => InstanceCount == 0;

    public InstancesViewModel(
        IInstanceManager instances,
        IAccountStore accountStore,
        IGameLauncher launcher,
        IJavaResolver javaResolver)
    {
        _instances = instances;
        _accountStore = accountStore;
        _launcher = launcher;
        _javaResolver = javaResolver;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var list = await _instances.ListAsync();
        Instances.Clear();
        foreach (var inst in list)
        {
            Instances.Add(new InstanceCardItem(inst));
        }
        InstanceCount = Instances.Count;
    }

    /// <summary>Xóa instance (kèm file trên disk) rồi refresh danh sách.</summary>
    public async Task DeleteInstanceAsync(InstanceCardItem? item)
    {
        if (item == null) return;

        await _instances.DeleteAsync(item.Id, deleteFiles: true);
        Instances.Remove(item);
        InstanceCount = Instances.Count;
    }

    /// <summary>Mở thư mục instance trong Explorer.</summary>
    public static void OpenFolder(InstanceCardItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Instance.Path)) return;
        if (!Directory.Exists(item.Instance.Path)) return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", item.Instance.Path)
        {
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public async Task PlayAsync(InstanceCardItem? item)
    {
        if (item == null) return;

        var account = await _accountStore.GetActiveAsync();
        if (account == null)
        {
            if (RequireAccountAction != null)
            {
                await RequireAccountAction.Invoke();
            }
            return;
        }

        try
        {
            item.IsLaunching = true;
            item.StatusText = "Đang khởi chạy...";

            var java = await _javaResolver.EnsureAsync(
                _javaResolver.RequiredJava(item.Instance.MinecraftVersion),
                progress: null);

            var result = await _launcher.LaunchAsync(new LaunchParameters
            {
                InstanceId = item.Instance.Id,
                InstancePath = item.Instance.Path,
                Account = account,
                Java = java,
                MinecraftVersion = item.Instance.MinecraftVersion,
                MaxRamMb = 4096
            });

            item.StatusText = $"Đang chạy (PID {result.ProcessId})";
            await _instances.UpdateLastPlayedAsync(item.Instance.Id);
        }
        catch (Exception ex)
        {
            item.StatusText = $"Lỗi: {ex.Message}";
        }
        finally
        {
            item.IsLaunching = false;
        }
    }
}
