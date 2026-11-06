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
    private readonly CreateInstanceViewModel _vm;
    private readonly WizardState _wizardState = new();
    private int _currentStepIndex = 1;

    /// <summary>Id instance vừa tạo (null nếu user hủy hoặc tạo thất bại).</summary>
    public string? CreatedInstanceId { get; private set; }

    public CreateInstanceDialog(CreateInstanceViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        RequestedTheme = AppThemeManager.DialogTheme;

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
        }
        else if (targetStep == 3)
        {
            var loader = _wizardState.SelectedLoader ?? "Fabric";
            var mc = _wizardState.SelectedVersion ?? "1.21.1";
            _ = Step3.SetDefaultNameAsync(null, loader, mc);
            Step3.SetBreadcrumb($"Bước 3/3 · {loader} {mc}");
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
        if (AppThemeManager.IsReduceMotion)
        {
            oldHost.Visibility = Visibility.Collapsed;
            oldHost.Opacity = 0;
            newHost.Visibility = Visibility.Visible;
            newHost.Opacity = 1;
            return;
        }

        var slideWidth = 820.0;
        var oldToX = forward ? -slideWidth : slideWidth;
        var newFromX = forward ? slideWidth : -slideWidth;

        var oldTranslate = new TranslateTransform { X = 0 };
        oldHost.RenderTransform = oldTranslate;

        var newTranslate = new TranslateTransform { X = newFromX };
        newHost.RenderTransform = newTranslate;
        newHost.Opacity = 0.0;
        newHost.Visibility = Visibility.Visible;

        var duration = new Duration(TimeSpan.FromMilliseconds(350));
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var sb = new Storyboard();
        sb.Duration = duration;

        // Old host: slide out + fade out
        var animOldX = new DoubleAnimation { To = oldToX, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animOldX, oldTranslate);
        Storyboard.SetTargetProperty(animOldX, nameof(TranslateTransform.X));

        var animOldOp = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animOldOp, oldHost);
        Storyboard.SetTargetProperty(animOldOp, nameof(UIElement.Opacity));

        // New host: slide in + fade in
        var animNewX = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animNewX, newTranslate);
        Storyboard.SetTargetProperty(animNewX, nameof(TranslateTransform.X));

        var animNewOp = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animNewOp, newHost);
        Storyboard.SetTargetProperty(animNewOp, nameof(UIElement.Opacity));

        sb.Children.Add(animOldX);
        sb.Children.Add(animOldOp);
        sb.Children.Add(animNewX);
        sb.Children.Add(animNewOp);

        sb.Completed += (_, _) =>
        {
            oldHost.Visibility = Visibility.Collapsed;
        };
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

        Step3.BeginCreating();

        var instance = await _vm.CreateFromWizardAsync(_wizardState, Step3.InstanceName);

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
