using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI.ViewModels;

public sealed partial class InstancesViewModel : ObservableObject
{
    private readonly IInstanceManager _instances;

    public ObservableCollection<MinecraftInstance> Instances { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstances))]
    [NotifyPropertyChangedFor(nameof(HasNoInstances))]
    private int _instanceCount;

    public bool HasInstances => InstanceCount > 0;
    public bool HasNoInstances => InstanceCount == 0;

    public InstancesViewModel(IInstanceManager instances) => _instances = instances;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var list = await _instances.ListAsync();
        Instances.Clear();
        foreach (var inst in list) Instances.Add(inst);
        InstanceCount = Instances.Count;
    }
}
