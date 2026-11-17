using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;
using Vaz.WinUI.Views;
using Vazer.Core.Settings;

namespace Vaz.WinUI;

/// <summary>
/// Main shell window: NavigationView (Instances / Accounts), Frame content on the right,
/// cố định thanh Minecraft Play Dock ở cạnh đáy.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    private const int WM_GETMINMAXINFO = 0x0024;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private readonly SUBCLASSPROC? _subclassProc;
    private static readonly SolidColorBrush DndActiveBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
    private static readonly SolidColorBrush DndInactiveBrush = new(Windows.UI.Color.FromArgb(255, 156, 163, 175)); // Muted Gray

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppTitleBar.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
        AppTitleBar.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();
        DndToggleButton.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
        DndToggleButton.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();
        NotificationBellButton.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
        NotificationBellButton.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();
        try
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 700));
        }
        catch { }

        // Enforce MinWidth / MinHeight (960x600) via Win32 WM_GETMINMAXINFO
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (hwnd != IntPtr.Zero)
        {
            _subclassProc = WindowSubclass;
            SetWindowSubclass(hwnd, _subclassProc, UIntPtr.Zero, IntPtr.Zero);
        }

        // SET-AC-04: theme/accent đã nạp trong App.OnLaunched; gắn shell + áp màu TRƯỚC
        // khi navigate để trang Instances đầu tiên render đúng theme (kể cả nav pane + dock).
        AppThemeManager.AttachShell((FrameworkElement)Content);

        // SET-AC-05: lắng nghe thay đổi reduce-motion để tắt/bật transition toàn shell
        AppThemeManager.ReduceMotionChanged += OnReduceMotionChanged;
        ApplyReduceMotion(AppThemeManager.IsReduceMotion);

        ViewModel.NavigateToAccountsAction = () =>
        {
            ContentFrame.Navigate(typeof(AccountPage));
            NavView.SelectedItem = NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == "account");
        };

        ViewModel.NavigateToSettingsAction = () =>
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            NavView.SelectedItem = NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Tag == "settings");
        };

        ContentFrame.Navigated += (s, e) =>
        {
            _ = ViewModel.RefreshActiveAccountAsync();
        };

        var notifService = (Application.Current as App)?.Services.GetService(typeof(INotificationService)) as INotificationService;
        if (notifService != null)
        {
            NotificationCenterOverlay.Initialize(notifService);
            notifService.UnreadCountChanged += (_, count) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateUnreadBadge(count);
                });
            };
            UpdateUnreadBadge(notifService.UnreadCount);
        }

        // Settings UI: áp ở App.LoadAndApplySettingsAsync (not here), để tránh double init flush luôn.

        _ = ViewModel.InitializeAsync();

        // A4: Icon Micro-bounce cho sidebar nav icons (reduce-motion gated inside helper)
        AttachNavIconBounce();
        var startupPage = Environment.GetEnvironmentVariable("VAZER_STARTUP_PAGE");
        if (startupPage == "InstanceDetailPage")
        {
            var startupInstance = Environment.GetEnvironmentVariable("VAZER_STARTUP_INSTANCE") ?? "test fabric";
            Navigate(typeof(InstanceDetailPage), startupInstance);
        }
        else
        {
            Navigate(typeof(InstancesPage));
        }

        // Demo test notification if requested
        var testToastEnv = Environment.GetEnvironmentVariable("VAZER_TEST_TOAST");
        if (!string.IsNullOrEmpty(testToastEnv))
        {
            var notif = (Application.Current as App)?.Services.GetService(typeof(INotificationService)) as INotificationService;
            if (notif != null)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await Task.Delay(600);
                    if (testToastEnv == "launch_error")
                    {
                        notif.ShowError(
                            "Khởi chạy thất bại",
                            "Đứt kết nối mạng trong quá trình tải tài nguyên Minecraft. Đã tự động lưu tiến trình, vui lòng bấm 'Thử lại' để tiếp tục tải.",
                            "Thử lại",
                            () => { return Task.CompletedTask; });
                    }
                    else if (testToastEnv == "center")
                    {
                        notif.ShowInfo("Bản cập nhật mới", "Đã tải xong bản cập nhật VAZ Launcher 1.2.0.", "Chi tiết", () => { return Task.CompletedTask; });
                        await Task.Delay(200);
                        notif.ShowWarning("Chưa chọn tài khoản", "Vui lòng đăng nhập trước khi khởi chạy game.", "Đăng nhập", () => { NavigateToAccounts(); return Task.CompletedTask; });
                        await Task.Delay(200);
                        notif.ShowError("Khởi chạy thất bại", "Đứt kết nối mạng trong quá trình tải tài nguyên.", "Thử lại", () => { return Task.CompletedTask; });
                        await Task.Delay(200);
                        notif.ShowSuccess("Đã cài Sodium 0.6.13", "Mod đã sẵn sàng hoạt động trong instance.");
                        await Task.Delay(600);
                        await NotificationCenterOverlay.OpenAsync();
                    }
                    else if (testToastEnv == "type_success")
                    {
                        notif.ShowSuccess("Đã cài Sodium 0.6.13", "Mod đã sẵn sàng hoạt động trong instance.");
                    }
                    else if (testToastEnv == "type_warning")
                    {
                        notif.ShowWarning("RAM khả dụng thấp", "Cấu hình RAM < 2048 MB có thể gây giật lag khi chơi game.", "Đi tới Settings", () => { ViewModel.NavigateToSettingsAction?.Invoke(); return Task.CompletedTask; });
                    }
                    else if (testToastEnv == "type_error")
                    {
                        notif.ShowError("Khởi chạy thất bại", "Đứt kết nối mạng trong quá trình tải tài nguyên Minecraft. Bấm 'Thử lại' để tiếp tục tải.", "Thử lại", () => { return Task.CompletedTask; });
                    }
                    else if (testToastEnv == "type_info")
                    {
                        notif.ShowInfo("Minecraft đang chạy", "Đang chạy 'test fabric' (1.20.1) - Nhấn 'Dừng' nếu muốn tắt game.", "Dừng", () => { ViewModel.StopGame(); return Task.CompletedTask; });
                    }
                    else if (testToastEnv == "type_update")
                    {
                        notif.ShowUpdate("Có 3 bản cập nhật mod", "Các mod Sodium, Iris và Lithium đã có phiên bản tương thích mới.", "Cập nhật tất cả", () => { return Task.CompletedTask; }, "Để sau", null);
                    }
                    else if (testToastEnv == "type_progress")
                    {
                        var pid = notif.ShowProgress("Đang tải Minecraft 1.20.1", "Đang nạp client.jar và assets...");
                        notif.UpdateProgress(pid, 68, "Đang nạp client.jar và assets (68%)...");
                    }
                    else
                    {
                        notif.ShowInfo("Bản cập nhật mới", "Đã tải xong bản cập nhật VAZ Launcher 1.2.0.", "Chi tiết", () => { return Task.CompletedTask; });
                        await Task.Delay(400);
                        notif.ShowWarning("Chưa chọn tài khoản", "Vui lòng đăng nhập trước khi khởi chạy game.", "Đăng nhập", () => { NavigateToAccounts(); return Task.CompletedTask; });
                        await Task.Delay(400);
                        notif.ShowError("Khởi chạy thất bại", "Đứt kết nối mạng trong quá trình tải tài nguyên. Bấm 'Thử lại' để tiếp tục.", "Thử lại", () => { return Task.CompletedTask; });
                        await Task.Delay(400);
                        notif.ShowSuccess("Đã cài Sodium 0.6.13", "Mod đã sẵn sàng hoạt động trong instance.");
                    }
                });
            }
        }
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        Navigate(pageType, parameter);
    }

    public void NavigateToAccounts()
    {
        var accountsItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => (string)i.Tag == "accounts");

        if (accountsItem != null)
        {
            NavView.SelectedItem = accountsItem;
        }
        if (ContentFrame.CurrentSourcePageType != typeof(AccountPage))
        {
            Navigate(typeof(AccountPage));
        }
    }

    private void AccountBadge_Click(object sender, RoutedEventArgs e)
    {
        NavigateToAccounts();
    }

    private void UpdateUnreadBadge(int count)
    {
        if (count > 0)
        {
            UnreadBadgeText.Text = count > 9 ? "9+" : count.ToString();
            UnreadBadgeBorder.Visibility = Visibility.Visible;
        }
        else
        {
            UnreadBadgeBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async void NotificationBellButton_Click(object sender, RoutedEventArgs e)
    {
        if (NotificationCenterOverlay.IsOpen)
        {
            await NotificationCenterOverlay.CloseAsync();
        }
        else
        {
            await NotificationCenterOverlay.OpenAsync();
        }
    }

    private void UpdateDndVisual(bool isDnd)
    {
        DndIcon.Foreground = isDnd ? DndActiveBrush : DndInactiveBrush;
        ToolTipService.SetToolTip(DndToggleButton, isDnd ? "Không làm phiền (Đang bật)" : "Không làm phiền (DND)");
    }

    private async void DndToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = (Application.Current as App)?.Services.GetService(typeof(ISettingsService)) as ISettingsService;
        if (settingsService == null) return;

        bool newValue = !settingsService.Current.NotificationDndEnabled;
        settingsService.Current.NotificationDndEnabled = newValue;
        UpdateDndVisual(newValue);
        await settingsService.SaveAsync(settingsService.Current);
    }

    /// <summary>
    /// Áp cài đặt mới từ GlobalSettings sang UI của window (theme/accent/DND/badge).
    /// Gọi từ App.LoadAndApplySettingsAsync sau khi nạp settings xong.
    /// </summary>
    public void ApplySettings(GlobalSettings settings)
    {
        // Theme (surface brushes) — AppThemeManager thao tác trực tiếp vào ResourceDictionary
        // nên UI cập nhật ngay mà không cần reload trang.
        AppThemeManager.SetTheme(settings.Theme);
        AppThemeManager.SetAccent(settings.AccentColor);

        // DND icon
        UpdateDndVisual(settings.NotificationDndEnabled);

        // Bell badge (tạm thời 0 vì InitializeAsync/lịch sử notification chưa load)
        RefreshNotificationBadge();
    }

    /// <summary>
    /// Cập nhật số badge notification từ INotificationService.
    /// Gọi từ App.LoadAndApplySettingsAsync và mỗi lần unread count thay đổi.
    /// </summary>
    public void RefreshNotificationBadge()
    {
        var notifService = (Application.Current as App)?.Services.GetService(typeof(INotificationService)) as INotificationService;
        if (notifService == null) return;

        UpdateUnreadBadge(notifService.UnreadCount);
    }

    private void UpdateTitleBarPassthroughRegions()
    {
        try
        {
            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported() && AppTitleBar.XamlRoot != null)
            {
                double scale = AppTitleBar.XamlRoot.RasterizationScale;
                var nonClientInputSrc = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);

                var rects = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
                foreach (var element in new FrameworkElement[] { DndToggleButton, NotificationBellButton })
                {
                    if (element.ActualWidth > 0 && element.ActualHeight > 0)
                    {
                        var transform = element.TransformToVisual(null);
                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
                        rects.Add(new Windows.Graphics.RectInt32(
                            (int)Math.Round(bounds.X * scale),
                            (int)Math.Round(bounds.Y * scale),
                            (int)Math.Round(bounds.Width * scale),
                            (int)Math.Round(bounds.Height * scale)
                        ));
                    }
                }

                if (rects.Count > 0)
                {
                    nonClientInputSrc.SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough, rects.ToArray());
                }
            }
        }
        catch
        {
            // Defensive: ignore platform limitations
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            var pageType = tag switch
            {
                "instances" => typeof(InstancesPage),
                "accounts" => typeof(AccountPage),
                "settings" => typeof(SettingsPage),
                _ => null
            };
            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                Navigate(pageType);
        }
    }

    private void ApplyReduceMotion(bool reduceMotion)
    {
        if (reduceMotion)
        {
            ContentFrame.ContentTransitions = null;
        }
        else
        {
            ContentFrame.ContentTransitions = new TransitionCollection
            {
                new NavigationThemeTransition()
            };
        }
    }

    private void OnReduceMotionChanged(bool reduceMotion)
    {
        ApplyReduceMotion(reduceMotion);
    }

    /// <summary>
    /// A4: Gắn Icon Micro-bounce cho tất cả NavigationViewItem icons trong sidebar.
    /// Hover → icon nhảy lên 2px rồi về (150ms). Reduce-motion gate bên trong helper.
    /// </summary>
    private void AttachNavIconBounce()
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Icon is FontIcon icon)
            {
                item.PointerEntered += (_, _) => UiAnimationHelper.AnimateIconMicroBounce(icon, -2.0, 150);
            }
        }
        foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            if (item.Icon is FontIcon icon)
            {
                item.PointerEntered += (_, _) => UiAnimationHelper.AnimateIconMicroBounce(icon, -2.0, 150);
            }
        }
    }

    private void Navigate(Type pageType, object? parameter = null)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, $"MainWindow Navigate: pageType={pageType.Name}, parameter={parameter}, stack={new System.Diagnostics.StackTrace(1, true)}\n");
        }
        catch { }

        NavigationTransitionInfo transition = AppThemeManager.IsReduceMotion
            ? new SuppressNavigationTransitionInfo()
            : new EntranceNavigationTransitionInfo();

        if (parameter != null)
            ContentFrame.Navigate(pageType, parameter, transition);
        else
            ContentFrame.Navigate(pageType, null, transition);
    }

    private IntPtr WindowSubclass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_GETMINMAXINFO)
        {
            var dpi = GetDpiForWindow(hWnd);
            var scale = dpi > 0 ? (float)dpi / 96.0f : 1.0f;
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            int targetWidth = (int)(960 * scale);
            int targetHeight = (int)(530 * scale);

            // Clamp against monitor work area so window min size never exceeds physical screen (e.g. 1366x768 at 150%)
            var hMon = MonitorFromWindow(hWnd, 2 /* MONITOR_DEFAULTTONEAREST */);
            if (hMon != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMon, ref mi))
                {
                    int workWidth = mi.rcWork.right - mi.rcWork.left;
                    int workHeight = mi.rcWork.bottom - mi.rcWork.top;
                    if (workWidth > 0 && targetWidth > workWidth)
                    {
                        targetWidth = workWidth;
                    }
                    if (workHeight > 0 && targetHeight > workHeight)
                    {
                        targetHeight = workHeight;
                    }
                }
            }

            mmi.ptMinTrackSize.x = targetWidth;
            mmi.ptMinTrackSize.y = targetHeight;
            Marshal.StructureToPtr(mmi, lParam, true);
            return IntPtr.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
