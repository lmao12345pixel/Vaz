using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;
using Vaz.WinUI.Views;
using Vazer.Core.Extensions;
using Vazer.Core.Settings;

namespace Vaz.WinUI;

/// <summary>
/// Application entry point. Builds the DI container from Vazer.Core's
/// AddMinecraftLauncher plus UI services/viewmodels.
/// </summary>
public partial class App : Application
{
    /// <summary>Root service provider.</summary>
    public IServiceProvider Services { get; }

    /// <summary>Dispatcher of the UI thread (set in OnLaunched).</summary>
    public static DispatcherQueue Dispatcher { get; private set; } = null!; // Assigned in OnLaunched (WinUI lifecycle)

    /// <summary>Main window instance (set in OnLaunched). Needed for pickers.</summary>
    public static MainWindow MainWindow { get; private set; } = null!; // Assigned in OnLaunched (WinUI lifecycle)

    public App()
    {
        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMinecraftLauncher(opt => config.GetSection("Minecraft").Bind(opt));

        // UI services (singletons)
        services.AddSingleton<Vazer.Core.Services.NotificationHistoryStore>();
        services.AddSingleton<ActiveInstanceService>();
        services.AddSingleton<IModrinthSearchService, ModrinthSearchService>();
        services.AddSingleton<IModDownloadManager, ModDownloadManager>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<GpuTierDetector>();
        services.AddSingleton<JavaCompatibilityNotifier>();
        services.AddSingleton<Vazer.Core.Java.IJavaCompatibilityNotifier>(
            sp => sp.GetRequiredService<JavaCompatibilityNotifier>());
        services.AddSingleton<CoverImageCacheService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<InstancesViewModel>();
        services.AddTransient<ModBrowserViewModel>();
        services.AddTransient<ModrinthStyleBrowserViewModel>();
        services.AddTransient<ModrinthCatalogViewModel>();
        services.AddTransient<InstanceDetailViewModel>();
        services.AddTransient<AccountViewModel>();
        services.AddTransient<CreateInstanceViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Pages / window
        services.AddTransient<InstancesPage>();
        services.AddTransient<InstanceDetailPage>();
        services.AddTransient<AccountPage>();
        services.AddTransient<SettingsPage>();
        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();

        // Đợt 21: mọi control/code-behind đọc chung một instance detector (đã detect GPU 1 lần).
        GpuTierDetector.Current = Services.GetRequiredService<GpuTierDetector>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UnhandledException += (s, e) =>
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer");
                var errPath = Path.Combine(dir, "crash.txt");

                // Rotate: nếu >1MB, rename thành crash-{timestamp}.txt, giữ tối đa 5 file cũ
                RotateCrashLog(errPath, dir);

                File.AppendAllText(errPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [FATAL] {e.Message}: {e.Exception}\n{e.Exception?.StackTrace}\n\n");
                File.AppendAllText(Path.Combine(dir, "lifecycle.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [FATAL_EXCEPTION] {e.Message}: {e.Exception?.GetType().Name}\n");
            }
            catch { }
            e.Handled = true;
        };

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer");
                File.AppendAllText(Path.Combine(dir, "lifecycle.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [LIFECYCLE] ProcessExit (ExitCode={Environment.ExitCode})\n");
            }
            catch { }
        };

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "lifecycle.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [LIFECYCLE] App OnLaunched\n");
        }
        catch { }

        // P2.1b Approach 2: window hiện NGAY với default state, RỜI sau đó load settings async.
        // Không block UI thread; không chờ settings load để Activate.
        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Closed += (s, e) =>
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer");
                File.AppendAllText(Path.Combine(dir, "lifecycle.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [LIFECYCLE] MainWindow Closed\n");
            }
            catch { }
        };
        window.Activate();
        Dispatcher = DispatcherQueue.GetForCurrentThread();

        // Fire-and-forget: load settings + áp vào UI sau khi window đã hiện.
        // Safe because LoadAndApplySettingsAsync has try/catch fail-safe (app không crash).
        _ = LoadAndApplySettingsAsync();
    }

    /// <summary>
    /// P2.1b Approach 2: async load settings và áp UI sau khi window đã Activate.
    /// Load vào UI thread (không ConfigureAwait(false)) để gọi thực thi UI trực tiếp.
    /// </summary>
    private async Task LoadAndApplySettingsAsync()
    {
        try
        {
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var globalSettings = await settingsService.LoadAsync();

            // Chuyển theme/accent/reduce-motion từ file settings sang hiện tại.
            AppThemeManager.Initialize(globalSettings);

            // Lấy window hiện tại (có thể chưa Activate nếu chưa khỏi hằng) và áp settings.
            var window = MainWindow;
            if (window != null)
            {
                // Gắn shell theme nếu chưa gắn (có thể đã ở OnLaunched từ AttachShell)
                AppThemeManager.AttachShell((FrameworkElement)window.Content);
                window.ApplySettings(globalSettings);

                // Lắng nghe SettingsChanged để update UI luôn khi user toggle settings sau này.
                settingsService.SettingsChanged += (s) => window.ApplySettings(s);
            }

            // Initialize notifications (fire-and-forget)
            _ = Services.GetRequiredService<INotificationService>().InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadAndApplySettingsAsync failed: {ex}");
            // Fail-safe: app dùng default settings, không crash.
        }
    }

    /// <summary>Rotate crash.txt khi >1MB. Giữ tối đa 5 file backup.</summary>
    private static void RotateCrashLog(string currentPath, string dir)
    {
        const long maxSize = 1_000_000; // 1MB
        const int maxBackups = 5;

        try
        {
            if (!File.Exists(currentPath)) return;
            var info = new FileInfo(currentPath);
            if (info.Length <= maxSize) return;

            // Rename current → crash-{timestamp}.txt
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(dir, $"crash-{timestamp}.txt");
            File.Move(currentPath, backupPath);

            // Xoá backup cũ nhất nếu >5
            var backups = Directory.GetFiles(dir, "crash-*.txt")
                .OrderByDescending(f => f)
                .Skip(maxBackups)
                .ToArray();
            foreach (var old in backups)
            {
                try { File.Delete(old); } catch { }
            }
        }
        catch { }
    }
}
