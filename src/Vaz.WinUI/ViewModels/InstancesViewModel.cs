using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.IO;
using Vaz.WinUI.Messages;
using Vaz.WinUI.Services;
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
    private bool _isActiveTarget;

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

public sealed partial class InstancesViewModel : ObservableObject, IDisposable
{
    private readonly IInstanceManager _instances;
    private readonly ActiveInstanceService _activeInstance;
    private readonly MainViewModel _mainViewModel;

    public ObservableCollection<InstanceCardItem> Instances { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstances))]
    [NotifyPropertyChangedFor(nameof(HasNoInstances))]
    private int _instanceCount;

    public bool HasInstances => InstanceCount > 0;
    public bool HasNoInstances => InstanceCount == 0;

    public InstancesViewModel(
        IInstanceManager instances,
        ActiveInstanceService activeInstance,
        MainViewModel mainViewModel)
    {
        _instances = instances;
        _activeInstance = activeInstance;
        _mainViewModel = mainViewModel;

        _activeInstance.CurrentChanged += OnActiveInstanceChanged;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
    }

    private void OnActiveInstanceChanged(object? sender, EventArgs e)
    {
        var dispatcher = App.Dispatcher;
        if (dispatcher != null && !dispatcher.HasThreadAccess)
        {
            dispatcher.TryEnqueue(UpdateActiveTarget);
        }
        else
        {
            UpdateActiveTarget();
        }
    }

    private void OnMainViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsRunning))
        {
            var dispatcher = App.Dispatcher;
            void UpdateStatus()
            {
                if (_mainViewModel.IsRunning)
                {
                    var selectedId = _mainViewModel.SelectedInstance?.Id;
                    var status = _mainViewModel.CurrentProcessId.HasValue
                        ? $"Đang chạy (PID {_mainViewModel.CurrentProcessId})"
                        : "Đang chạy";

                    foreach (var card in Instances)
                    {
                        if (card.Id == selectedId)
                        {
                            card.StatusText = status;
                        }
                        else if (card.StatusText.StartsWith("Đang chạy"))
                        {
                            card.StatusText = string.Empty;
                        }
                    }
                }
                else
                {
                    foreach (var card in Instances)
                    {
                        card.StatusText = string.Empty;
                    }
                }
            }

            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(UpdateStatus);
            }
            else
            {
                UpdateStatus();
            }
        }
    }

    public void UpdateActiveTarget()
    {
        var currentId = _activeInstance.CurrentInstanceId;
        foreach (var item in Instances)
        {
            item.IsActiveTarget = (item.Id == currentId);
        }
    }

    public void SetActiveTarget(string id)
    {
        _activeInstance.SetCurrent(id);
        UpdateActiveTarget();
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
        UpdateActiveTarget();
    }

    /// <summary>Xóa instance (kèm file trên disk) rồi refresh danh sách và cập nhật Dock.</summary>
    public async Task DeleteInstanceAsync(InstanceCardItem? item)
    {
        if (item == null) return;
        await DeleteInstanceAsync(item.Id);
    }

    public async Task DeleteInstanceAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if ((_mainViewModel.IsRunning || _mainViewModel.IsLaunching) && _mainViewModel.RunningInstanceId == id)
        {
            var runningCard = Instances.FirstOrDefault(i => i.Id == id);
            if (runningCard != null)
            {
                runningCard.StatusText = "Không thể xóa khi đang chạy";
            }
            return;
        }

        try
        {
            await _instances.DeleteAsync(id, deleteFiles: true);

            var item = Instances.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                Instances.Remove(item);
                InstanceCount = Instances.Count;
            }

            if (_activeInstance.CurrentInstanceId == id)
            {
                var next = Instances.FirstOrDefault();
                _activeInstance.SetCurrent(next?.Id);
                UpdateActiveTarget();
            }

            WeakReferenceMessenger.Default.Send(new InstancesChangedMessage());
        }
        catch (Exception ex)
        {
            var failedCard = Instances.FirstOrDefault(i => i.Id == id);
            if (failedCard != null)
            {
                failedCard.StatusText = $"Lỗi xóa: {ex.Message}";
            }
        }
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
        if (_mainViewModel.IsRunning || _mainViewModel.IsLaunching) return;

        SetActiveTarget(item.Id);

        try
        {
            item.IsLaunching = true;
            item.StatusText = "Đang khởi chạy...";

            await _mainViewModel.LaunchInstanceAsync(item.Instance);

            if (_mainViewModel.IsRunning)
            {
                item.StatusText = _mainViewModel.CurrentProcessId.HasValue
                    ? $"Đang chạy (PID {_mainViewModel.CurrentProcessId})"
                    : "Đang chạy";
            }
            else
            {
                item.StatusText = string.Empty;
            }
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

    public void Dispose()
    {
        _activeInstance.CurrentChanged -= OnActiveInstanceChanged;
        _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
    }
}
