using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Vaz.WinUI.Messages;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Launch;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ViewModel quản lý thanh điều khiển Minecraft Play Dock và trạng thái toàn cục của MainWindow.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IInstanceManager _instances;
    private readonly IAccountStore _accountStore;
    private readonly IGameLauncher _launcher;
    private readonly IJavaResolver _javaResolver;
    private readonly ActiveInstanceService _activeInstance;

    private IProcessHandle? _currentProcess;

    public ObservableCollection<MinecraftInstance> Instances { get; } = new();

    [ObservableProperty]
    private MinecraftInstance? _selectedInstance;

    [ObservableProperty]
    private string? _runningInstanceId;

    private bool _isReloading;

    [ObservableProperty]
    private string _activeAccountName = "Chưa có tài khoản";

    [ObservableProperty]
    private string _accountKindText = "Bấm để chọn";

    [ObservableProperty]
    private bool _hasActiveAccount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLaunching))]
    [NotifyPropertyChangedFor(nameof(LaunchingVisibility))]
    [NotifyPropertyChangedFor(nameof(NotLaunchingVisibility))]
    [NotifyPropertyChangedFor(nameof(CanChangeInstance))]
    private bool _isLaunching;

    public bool IsNotLaunching => !IsLaunching;
    public Visibility LaunchingVisibility => IsLaunching ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotLaunchingVisibility => IsLaunching ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayButtonBackground))]
    [NotifyPropertyChangedFor(nameof(PlayButtonGlyph))]
    [NotifyPropertyChangedFor(nameof(CanChangeInstance))]
    private bool _isRunning;

    public bool CanChangeInstance => !IsRunning && !IsLaunching;

    [ObservableProperty]
    private int? _currentProcessId;

    [ObservableProperty]
    private string _playButtonText = "CHƠI";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlayButtonSubText))]
    [NotifyPropertyChangedFor(nameof(SubTextVisibility))]
    private string _playButtonSubText = string.Empty;

    public bool HasPlayButtonSubText => !string.IsNullOrEmpty(PlayButtonSubText);
    public Visibility SubTextVisibility => HasPlayButtonSubText ? Visibility.Visible : Visibility.Collapsed;

    public string PlayButtonGlyph => IsRunning ? "\uE71A" : "\uE768";

    private static readonly SolidColorBrush FallbackEmeraldBrush = new(Color.FromArgb(255, 34, 197, 94));
    private static readonly SolidColorBrush FallbackRunningRedBrush = new(Color.FromArgb(255, 239, 68, 68));

    public Brush PlayButtonBackground
    {
        get
        {
            if (IsRunning)
            {
                if (Application.Current?.Resources.TryGetValue("MinecraftRunningRedBrush", out var red) == true && red is Brush rb)
                    return rb;
                return FallbackRunningRedBrush;
            }

            if (Application.Current?.Resources.TryGetValue("MinecraftEmeraldBrush", out var emerald) == true && emerald is Brush eb)
                return eb;
            return FallbackEmeraldBrush;
        }
    }

    public Action<string>? ShowErrorMessageAction { get; set; }

    public MainViewModel(
        IInstanceManager instances,
        IAccountStore accountStore,
        IGameLauncher launcher,
        IJavaResolver javaResolver,
        ActiveInstanceService activeInstance)
    {
        _instances = instances;
        _accountStore = accountStore;
        _launcher = launcher;
        _javaResolver = javaResolver;
        _activeInstance = activeInstance;

        _activeInstance.CurrentChanged += OnActiveInstanceChanged;

        WeakReferenceMessenger.Default.Register<InstancesChangedMessage>(this, (r, m) =>
        {
            var dispatcher = App.Dispatcher;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() => _ = LoadInstancesAsync());
            }
            else
            {
                _ = LoadInstancesAsync();
            }
        });

        WeakReferenceMessenger.Default.Register<AccountsChangedMessage>(this, (r, m) =>
        {
            var dispatcher = App.Dispatcher;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() => _ = RefreshActiveAccountAsync());
            }
            else
            {
                _ = RefreshActiveAccountAsync();
            }
        });
    }

    private void OnActiveInstanceChanged(object? sender, EventArgs e)
    {
        var dispatcher = App.Dispatcher;
        if (dispatcher != null && !dispatcher.HasThreadAccess)
        {
            dispatcher.TryEnqueue(SyncSelectedFromActiveService);
        }
        else
        {
            SyncSelectedFromActiveService();
        }
    }

    partial void OnSelectedInstanceChanged(MinecraftInstance? value)
    {
        if (_isReloading) return;

        if (value != null && _activeInstance.CurrentInstanceId != value.Id)
        {
            _activeInstance.SetCurrent(value.Id);
        }
        else if (value == null && _activeInstance.CurrentInstanceId != null)
        {
            _activeInstance.SetCurrent(null);
        }
        UpdatePlayButtonLabels();
    }

    private void SyncSelectedFromActiveService()
    {
        var currentId = _activeInstance.CurrentInstanceId;
        if (string.IsNullOrEmpty(currentId))
        {
            if (SelectedInstance != null && Instances.Count == 0)
            {
                SelectedInstance = null;
            }
            return;
        }
        if (SelectedInstance?.Id == currentId) return;

        var match = Instances.FirstOrDefault(i => i.Id == currentId);
        if (match != null)
        {
            SelectedInstance = match;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadInstancesAsync();
        await RefreshActiveAccountAsync();
    }

    public async Task LoadInstancesAsync()
    {
        var list = await _instances.ListAsync();
        var targetId = _activeInstance.CurrentInstanceId ?? SelectedInstance?.Id;

        _isReloading = true;
        try
        {
            Instances.Clear();
            foreach (var inst in list)
            {
                Instances.Add(inst);
            }

            if (!string.IsNullOrEmpty(targetId))
            {
                SelectedInstance = Instances.FirstOrDefault(i => i.Id == targetId);
            }

            if (SelectedInstance == null && Instances.Count > 0)
            {
                SelectedInstance = Instances[0];
                _activeInstance.SetCurrent(Instances[0].Id);
            }
            else if (SelectedInstance == null && Instances.Count == 0)
            {
                _activeInstance.SetCurrent(null);
            }
        }
        finally
        {
            _isReloading = false;
        }

        UpdatePlayButtonLabels();
    }

    public async Task RefreshActiveAccountAsync()
    {
        try
        {
            var active = await _accountStore.GetActiveAsync();
            if (active != null)
            {
                ActiveAccountName = active.DisplayName;
                AccountKindText = active.Kind == AccountKind.Microsoft ? "Microsoft" : "Offline";
                HasActiveAccount = true;
            }
            else
            {
                ActiveAccountName = "Chưa có tài khoản";
                AccountKindText = "Bấm để chọn";
                HasActiveAccount = false;
            }
        }
        catch
        {
            ActiveAccountName = "Chưa có tài khoản";
            AccountKindText = "Bấm để chọn";
            HasActiveAccount = false;
        }
    }

    private void UpdatePlayButtonLabels()
    {
        if (IsRunning)
        {
            PlayButtonText = "DỪNG";
            PlayButtonSubText = CurrentProcessId.HasValue ? $"PID {CurrentProcessId} • Đang chạy" : "Đang chạy";
        }
        else if (IsLaunching)
        {
            PlayButtonText = "ĐANG KHỞI CHẠY...";
            PlayButtonSubText = "Đang chuẩn bị Java...";
        }
        else
        {
            PlayButtonText = "CHƠI";
            if (SelectedInstance != null)
            {
                PlayButtonSubText = $"{SelectedInstance.MinecraftVersion} ({SelectedInstance.LoaderType})";
            }
            else
            {
                PlayButtonSubText = "Chưa chọn instance";
            }
        }
    }

    public async Task LaunchInstanceAsync(MinecraftInstance? target = null)
    {
        var instance = target ?? SelectedInstance;
        if (instance == null)
        {
            ShowErrorMessageAction?.Invoke("Vui lòng chọn một instance trước khi chơi.");
            return;
        }

        if (IsRunning || IsLaunching)
        {
            return;
        }

        RunningInstanceId = instance.Id;
        IsLaunching = true;
        UpdatePlayButtonLabels();

        if (SelectedInstance?.Id != instance.Id)
        {
            var match = Instances.FirstOrDefault(i => i.Id == instance.Id);
            SelectedInstance = match ?? instance;
        }

        if (_activeInstance.CurrentInstanceId != instance.Id)
        {
            _activeInstance.SetCurrent(instance.Id);
        }

        try
        {
            var account = await _accountStore.GetActiveAsync();
            if (account == null)
            {
                RunningInstanceId = null;
                IsLaunching = false;
                UpdatePlayButtonLabels();
                ShowErrorMessageAction?.Invoke("Vui lòng đăng nhập hoặc chọn tài khoản trước khi chơi. Nhấp vào thẻ tài khoản ở góc dưới bên phải để mở trang Tài khoản.");
                return;
            }

            var java = await _javaResolver.EnsureAsync(
                _javaResolver.RequiredJava(instance.MinecraftVersion),
                progress: null);

            PlayButtonSubText = "Đang khởi chạy tiến trình Minecraft...";

            var result = await _launcher.LaunchAsync(new LaunchParameters
            {
                InstanceId = instance.Id,
                InstancePath = instance.Path,
                Account = account,
                Java = java,
                MinecraftVersion = instance.MinecraftVersion,
                LaunchVersionName = LaunchVersionResolver.Resolve(instance),
                MaxRamMb = 4096
            });

            _currentProcess = result.Process;
            CurrentProcessId = result.ProcessId;
            IsRunning = true;
            IsLaunching = false;
            UpdatePlayButtonLabels();

            await _instances.UpdateLastPlayedAsync(instance.Id);

            _ = Task.Run(async () =>
            {
                try
                {
                    await result.Process.WaitForExitAsync();
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    result.Process.Dispose();
                    if (_currentProcess == result.Process)
                    {
                        _currentProcess = null;
                    }

                    void ResetState()
                    {
                        IsRunning = false;
                        CurrentProcessId = null;
                        RunningInstanceId = null;
                        UpdatePlayButtonLabels();
                    }

                    var dispatcher = App.Dispatcher;
                    if (dispatcher != null && !dispatcher.HasThreadAccess)
                    {
                        dispatcher.TryEnqueue(ResetState);
                    }
                    else
                    {
                        ResetState();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            IsLaunching = false;
            IsRunning = false;
            CurrentProcessId = null;
            RunningInstanceId = null;
            UpdatePlayButtonLabels();
            ShowErrorMessageAction?.Invoke($"Khởi chạy thất bại: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PlayAsync()
    {
        if (IsRunning)
        {
            try
            {
                if (_currentProcess is { HasExited: false })
                {
                    try
                    {
                        _currentProcess.Kill();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageAction?.Invoke($"Không thể dừng tiến trình: {ex.Message}");
            }
            return;
        }

        if (IsLaunching)
        {
            return;
        }

        if (SelectedInstance == null)
        {
            ShowErrorMessageAction?.Invoke("Vui lòng chọn một instance trước khi chơi.");
            return;
        }

        await LaunchInstanceAsync(SelectedInstance);
    }
}
