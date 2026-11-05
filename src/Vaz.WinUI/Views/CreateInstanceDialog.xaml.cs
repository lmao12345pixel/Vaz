using System.Diagnostics;
using System.IO;
using System.Linq;
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
/// Wizard tạo instance mới (2 bước: chọn version → chọn loader).
/// Validate qua CreateInstanceViewModel, tạo qua IInstanceCreationService.
/// </summary>
public sealed partial class CreateInstanceDialog : ContentDialog
{
    private readonly CreateInstanceViewModel _vm;
    private readonly WizardState _wizardState = new();

    /// <summary>Id instance vừa tạo (null nếu user hủy hoặc tạo thất bại).</summary>
    public string? CreatedInstanceId { get; private set; }

    public CreateInstanceDialog(CreateInstanceViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        // WinUI 3 ContentDialog không tự thừa hưởng theme từ MainWindow (AppThemeManager.DialogTheme).
        RequestedTheme = AppThemeManager.DialogTheme;

        // Gán State dùng chung cho 2 step.
        Step1.State = _wizardState;
        Step2.State = _wizardState;

        // Đồng bộ khi user chọn xong ở Step 1 → chuyển sang Step 2.
        Step1.ContinueRequested += (_, _) => SwitchToStep2();
        Step1.CancelRequested += (_, _) => OnCancel();

        Step2.BackRequested += (_, _) => SwitchToStep1();
        Step2.CancelRequested += (_, _) => OnCancel();
        Step2.CreateRequested += async (_, _) => await CreateInstanceAsync();
    }

    private void OnCancel()
    {
        if (!_vm.IsInstalling)
        {
            Hide();
        }
    }

    private void SwitchToStep2()
    {
        try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt"), $"SwitchToStep2 version={_wizardState.SelectedVersion}\n"); } catch { }
        _wizardState.CurrentStep = WizardStep.Step2Loader;

        if (!string.IsNullOrWhiteSpace(_wizardState.SelectedVersion))
        {
            _vm.SelectedMinecraftVersion = _wizardState.SelectedVersion;
        }
        Step2.SetBreadcrumb($"Bước 2/2 · Minecraft {_wizardState.SelectedVersion ?? "?"}");

        // Fade + slide step1 out → step2 in.
        Step1Host.Visibility = Visibility.Collapsed;
        Step2Host.Visibility = Visibility.Visible;
        AnimateHost(Step1Host, show: false);
        AnimateHost(Step2Host, show: true);
    }

    private void SwitchToStep1()
    {
        _wizardState.CurrentStep = WizardStep.Step1Version;

        Step2Host.Visibility = Visibility.Collapsed;
        Step1Host.Visibility = Visibility.Visible;
        AnimateHost(Step2Host, show: false);
        AnimateHost(Step1Host, show: true);
    }

    private void AnimateHost(FrameworkElement host, bool show)
    {
        if (AppThemeManager.IsReduceMotion)
        {
            host.Opacity = show ? 1 : 0;
            return;
        }

        var translate = new TranslateTransform { Y = show ? 12 : 0 };
        host.RenderTransform = translate;
        host.Opacity = show ? 0 : 1;

        var sb = new Storyboard();
        var s = new Duration(TimeSpan.FromMilliseconds(220));
        var animOp = new DoubleAnimation { To = show ? 1 : 0, Duration = s, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(animOp, host);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
        var animY = new DoubleAnimation { To = 0, Duration = s, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(animY, translate);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        sb.Children.Add(animOp);
        sb.Children.Add(animY);
        sb.Begin();
    }

    /// <summary>Tạo instance từ lựa chọn của wizard — giữ nguyên flow VM + error handling cũ.</summary>
    private async Task CreateInstanceAsync()
    {
        CreateErrorBar.IsOpen = false;

        // Đồng bộ input từ wizard → VM.
        _vm.Name = Step2.InstanceName;
        _vm.SelectedLoader = _wizardState.SelectedLoader ?? "Vanilla";
        _vm.SelectedMinecraftVersion = _wizardState.SelectedVersion ?? string.Empty;
        _vm.SelectedLoaderVersion = string.IsNullOrWhiteSpace(_wizardState.SelectedLoaderVersion)
            ? null
            : _wizardState.SelectedLoaderVersion;

        Step2.SetCreateEnabled(false);

        var instance = await _vm.CreateAsync();

        if (instance == null)
        {
            CreateErrorBar.Message = _vm.ErrorMessage ?? "Không tạo được instance.";
            CreateErrorBar.IsOpen = true;
            Step2.SetCreateEnabled(true);
            return;
        }

        CreatedInstanceId = instance.Id;

        // Đóng dialog khi thành công.
        Hide();
    }

    /// <summary>Mở log NeoForge installer mới nhất (hoặc thư mục downloads nếu chưa có log).</summary>
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
            // Không chặn flow khi không mở được log — thông điệp lỗi đã hiển thị trên InfoBar.
        }
    }

    /// <summary>
    /// WinUI 3 ContentDialog không có IsCloseButtonEnabled — chặn nút Hủy khi đang cài
    /// bằng cách cancel CloseButtonClick (dialog giữ nguyên, cài đặt tiếp tục).
    /// </summary>
    private void Dialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_vm.IsInstalling)
        {
            args.Cancel = true;
        }
    }
}
