using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.ViewModels;
using Vaz.WinUI.Views;

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

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private readonly SUBCLASSPROC? _subclassProc;

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
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

        ViewModel.ShowErrorMessageAction = async msg =>
        {
            if (Content is FrameworkElement root && root.XamlRoot != null)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = root.XamlRoot,
                    Title = "Thông báo",
                    Content = msg,
                    CloseButtonText = "Đóng",
                    DefaultButton = ContentDialogButton.Close
                };
                await dialog.ShowAsync();
            }
        };

        ContentFrame.Navigated += (s, e) =>
        {
            _ = ViewModel.RefreshActiveAccountAsync();
        };

        _ = ViewModel.InitializeAsync();
        Navigate(typeof(InstancesPage));
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
            mmi.ptMinTrackSize.x = (int)(960 * scale);
            mmi.ptMinTrackSize.y = (int)(600 * scale);
            Marshal.StructureToPtr(mmi, lParam, true);
            return IntPtr.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
