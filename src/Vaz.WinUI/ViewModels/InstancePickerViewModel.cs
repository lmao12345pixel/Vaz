using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// Instance picker dùng chung ở NavigationView header.
/// Khi chọn instance → set ActiveInstanceService.CurrentInstanceId.
/// </summary>
public sealed partial class InstancePickerViewModel : ObservableObject
{
    private readonly IInstanceManager _instanceManager;
    private readonly ActiveInstanceService _activeInstance;

    public ObservableCollection<MinecraftInstance> Instances { get; } = new();

    [ObservableProperty]
    private MinecraftInstance? selectedInstance;

    [ObservableProperty]
    private bool isLoading;

    public InstancePickerViewModel(IInstanceManager instanceManager, ActiveInstanceService activeInstance)
    {
        _instanceManager = instanceManager;
        _activeInstance = activeInstance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    partial void OnSelectedInstanceChanged(MinecraftInstance? value)
    {
        _activeInstance.SetCurrent(value?.Id);
    }

    public async Task LoadAsync()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            var currentId = _activeInstance.CurrentInstanceId;
            var list = await _instanceManager.ListAsync();

            Instances.Clear();
            MinecraftInstance? toSelect = null;
            foreach (var inst in list)
            {
                Instances.Add(inst);
                if (inst.Id == currentId) toSelect = inst;
            }

            SelectedInstance = toSelect ?? (Instances.Count > 0 ? Instances[0] : null);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
