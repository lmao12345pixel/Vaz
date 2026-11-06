using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views;

/// <summary>
/// Wizard tạo instance mới 3 bước:
/// Bước 1: Chọn Minecraft version.
/// Bước 2: Chọn Mod Loader.
/// Bước 3: Xác nhận & Tạo instance.
/// </summary>
public sealed partial class CreateInstanceDialog : ContentDialog
{
    /// <summary>
    /// Argument change-action (chế độ "Đổi phiên bản"): nhận WizardState + tên pinned +
    /// option (giữ/xóa mods) + progress; trả instance sau khi đổi.
    /// </summary>
    public delegate Task<MinecraftInstance?> ChangeVersionAction(
        WizardState state,
        string pinnedName,
        InstanceDetailViewModel.ModChangeOption option,
        IProgress<InstallerProgress>? progress);

    private readonly CreateInstanceViewModel _vm;
    private readonly WizardState _wizardState = new();
    private readonly MinecraftInstance? _changeInstance;
    private readonly ChangeVersionAction? _changeAction;
    private string? _preselectMc;
    private int _currentStepIndex = 1;

    /// <summary>Id instance vừa tạo (null nếu user hủy hoặc tạo thất bại).</summary>
    public string? CreatedInstanceId { get; private set; }

    /// <summary>Chế độ "Đổi phiên bản" (khác null khi mở từ Instance Detail).</summary>
    public bool ChangeMode => _changeInstance != null;

    public CreateInstanceDialog(CreateInstanceViewModel vm, MinecraftInstance? changeInstance = null, ChangeVersionAction? changeAction = null)
    {
        _vm = vm;
        _changeInstance = changeInstance;
        _changeAction = changeAction;
        DataContext = _vm;
        InitializeComponent();

        RequestedTheme = AppThemeManager.DialogTheme;

        if (_changeInstance != null)
        {
            Title = "Đổi phiên bản instance";
            CreateErrorBar.Title = "Đổi phiên bản thất bại";
        }

        Step1.State = _wizardState;
        Step2.State = _wizardState;
        Step3.State = _wizardState;

        // Step 1 navigation
        Step1.ContinueRequested += (_, _) => SwitchToStep(2);
        Step1.CancelRequested += (_, _) => OnCancel();

        // Step 2 navigation
        Step2.BackRequested += (_, _) => SwitchToStep(1);
        Step2.CancelRequested += (_, _) => OnCancel();
        Step2.CreateRequested += (_, _) => SwitchToStep(3);

        // Step 3 navigation
        Step3.BackRequested += (_, _) => SwitchToStep(2);
        Step3.CancelRequested += (_, _) => OnCancel();
        Step3.CreateRequested += async (_, _) => await CreateInstanceAsync();
        Step3.SuccessConfirmed += (_, _) => Hide();

        _vm.PropertyChanged += OnViewModelPropertyChanged;

        // Chế độ "Đổi phiên bản": preselect MC version + loader hiện tại, khóa tên instance.
        if (_changeInstance != null)
        {
            _preselectMc = _changeInstance.MinecraftVersion;
            Step1.PendingSelectVersionId = _changeInstance.MinecraftVersion;
            _ = Step2.SelectLoaderAndVersionAsync(_changeInstance.LoaderType, _changeInstance.LoaderVersion);
        }

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AnimateDialogOpen();
        ApplyBackdropBlur();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CreateInstanceViewModel.InstallMessage) ||
            e.PropertyName == nameof(CreateInstanceViewModel.InstallerProgressPercent) ||
            e.PropertyName == nameof(CreateInstanceViewModel.InstallStage))
        {
            var msg = _vm.InstallMessage ?? "Đang tạo instance...";
            Step3.UpdateProgress(_vm.InstallerProgressPercent, msg);
        }
    }

    private void SwitchToStep(int targetStep)
    {
        if (targetStep < 1 || targetStep > 3 || targetStep == _currentStepIndex) return;

        var forward = targetStep > _currentStepIndex;
        var oldHost = GetHostForStep(_currentStepIndex);
        var newHost = GetHostForStep(targetStep);
        _currentStepIndex = targetStep;

        _wizardState.CurrentStep = (WizardStep)targetStep;
        ProgressIndicator.SetStep(targetStep);

        TopBreadcrumbText.Text = targetStep switch
        {
            1 => "Bước 1/3 · Chọn phiên bản",
            2 => "Bước 2/3 · Chọn Mod Loader",
            3 => $"Bước 3/3 · {_wizardState.SelectedLoader ?? "Fabric"} {_wizardState.SelectedVersion ?? ""}".Trim(),
            _ => ""
        };

        if (targetStep == 2)
        {
            Step2.SetBreadcrumb($"Bước 2/3 · Minecraft {_wizardState.SelectedVersion ?? "?"}");

            // Change mode: user đã đổi MC version trong Step 1 → refresh loader versions cho MC mới
            // (loại loader vẫn giữ, loader version về default mới nhất của MC mới).
            if (_changeInstance != null && _wizardState.SelectedVersion != _preselectMc)
            {
                _preselectMc = _wizardState.SelectedVersion;
                _ = Step2.SelectLoaderAndVersionAsync(
                    _wizardState.SelectedLoader ?? _changeInstance.LoaderType,
                    loaderVersion: null);
            }
        }
        else if (targetStep == 3)
        {
            var loader = _wizardState.SelectedLoader ?? "Fabric";
            var mc = _wizardState.SelectedVersion;
            if (_changeInstance != null)
            {
                Step3.SetNameReadOnly(_changeInstance.Name);
                Step3.SetActionLabel("Đổi phiên bản");
            }
            else if (!string.IsNullOrWhiteSpace(mc))
            {
                _ = Step3.SetDefaultNameAsync(null, loader, mc);
            }
            Step3.PrepareStep3();
        }

        AnimateStepTransition(oldHost, newHost, forward);
    }

    private FrameworkElement GetHostForStep(int step) => step switch
    {
        1 => Step1Host,
        2 => Step2Host,
        3 => Step3Host,
        _ => Step1Host
    };

    private void AnimateStepTransition(FrameworkElement oldHost, FrameworkElement newHost, bool forward)
    {
        oldHost.Visibility = Visibility.Collapsed;
        oldHost.Opacity = 0;
        oldHost.RenderTransform = null;

        newHost.Visibility = Visibility.Visible;
        newHost.Opacity = 1;

        if (AppThemeManager.IsReduceMotion)
        {
            newHost.RenderTransform = null;
            return;
        }

        var fromX = forward ? 140.0 : -140.0;
        var translate = new TranslateTransform { X = fromX };
        newHost.RenderTransform = translate;
        newHost.Opacity = 0.0;

        var duration = new Duration(TimeSpan.FromMilliseconds(350));
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var sb = new Storyboard();
        sb.Duration = duration;

        var animX = new DoubleAnimation { From = fromX, To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animX, translate);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));

        var animOp = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animOp, newHost);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));

        sb.Children.Add(animX);
        sb.Children.Add(animOp);
        sb.Begin();
    }

    private void AnimateDialogOpen()
    {
        if (AppThemeManager.IsReduceMotion) return;

        RootGrid.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var scale = new ScaleTransform { ScaleX = 0.92, ScaleY = 0.92 };
        RootGrid.RenderTransform = scale;
        RootGrid.Opacity = 0.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(300));
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };

        var animX = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animX, scale);
        Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));

        var animY = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animY, scale);
        Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));

        var animOp = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animOp, RootGrid);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));

        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Children.Add(animOp);
        sb.Begin();
    }

    private void ApplyBackdropBlur()
    {
        try
        {
            UiAnimationHelper.ApplyCompositionBackdrop(RootGrid, true);
        }
        catch
        {
            // Fallback graceful
        }
    }

    private async Task CreateInstanceAsync()
    {
        CreateErrorBar.IsOpen = false;

        var pinnedName = _changeInstance?.Name;
        var option = InstanceDetailViewModel.ModChangeOption.KeepMods;

        // Chế độ "Đổi phiên bản": hỏi giữ/xóa mods TRƯỚC khi bắt đầu cài.
        // (WinUI 3 không cho ContentDialog lồng trong ContentDialog → dùng overlay panel của Step3.)
        if (_changeInstance != null && _changeAction != null)
        {
            option = await Step3.RequestMigrationConfirmationAsync();
            if (option == InstanceDetailViewModel.ModChangeOption.Cancel)
            {
                // Giữ nguyên wizard, không thay đổi gì.
                return;
            }
        }

        Step3.BeginCreating();

        MinecraftInstance? instance;
        if (_changeInstance != null && _changeAction != null)
        {
            // Map progress thật của installer vào VM để Step3 hiển thị (như luồng tạo mới).
            var progress = new Progress<InstallerProgress>(p =>
            {
                _vm.InstallerProgressPercent = p.Percent;
                _vm.InstallMessage = p.Message;
                _vm.InstallStage = p.Stage.ToString();
            });

            try
            {
                instance = await _changeAction(_wizardState, pinnedName!, option, progress);
            }
            catch (Exception ex)
            {
                CreateErrorBar.Message = ex.Message;
                CreateErrorBar.IsOpen = true;
                Step3.CreationFailed();
                return;
            }
        }
        else
        {
            instance = await _vm.CreateFromWizardAsync(_wizardState, pinnedName ?? Step3.InstanceName);
        }

        if (instance == null)
        {
            CreateErrorBar.Message = _vm.ErrorMessage ?? "Không tạo được instance.";
            CreateErrorBar.IsOpen = true;
            Step3.CreationFailed();
            return;
        }

        CreatedInstanceId = instance.Id;
        Step3.CreationSucceeded();
    }



    private void OnCancel()
    {
        if (!Step3.IsCreating)
        {
            Hide();
        }
    }

    private void ViewInstallLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Vazer", "Minecraft", "java", "downloads");

            string? log = null;
            if (Directory.Exists(dir))
            {
                log = Directory.GetFiles(dir, "neoforge-*-installer.jar.log")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            var target = log ?? (Directory.Exists(dir)
                ? dir
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}
