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
    /// <summary>Dispatcher of the UI thread (set in OnLaunched).</summary>
    public static DispatcherQueue Dispatcher { get; private set; } = null!;

    /// <summary>Main window instance (set in OnLaunched). Needed for pickers.</summary>
    public static Window MainWindow { get; private set; } = null!;

    /// <summary>Root service provider.</summary>
    public IServiceProvider Services { get; }

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
        services.AddSingleton<ActiveInstanceService>();
        services.AddSingleton<IModrinthSearchService, ModrinthSearchService>();
        services.AddSingleton<IModDownloadManager, ModDownloadManager>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<InstancesViewModel>();
        services.AddTransient<ModBrowserViewModel>();
        services.AddTransient<ModrinthStyleBrowserViewModel>();
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
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UnhandledException += (s, e) =>
        {
            try
            {
                var errPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "crash.txt");
                File.AppendAllText(errPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [FATAL] {e.Message}: {e.Exception}\n{e.Exception?.StackTrace}\n\n");
            }
            catch { }
            e.Handled = true;
        };

        Dispatcher = DispatcherQueue.GetForCurrentThread();

        // SET-AC-04: nạp settings trước khi tạo MainWindow để theme/accent đã lưu được áp
        // TRƯỚC ContentFrame.Navigate — tránh nháy theme mặc định ở frame đầu tiên.
        var settings = Services.GetRequiredService<ISettingsService>()
            .LoadAsync()
            .GetAwaiter()
            .GetResult();
        AppThemeManager.Initialize(settings);

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Activate();
    }
}
