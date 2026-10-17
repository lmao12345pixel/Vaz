using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;
using Vaz.WinUI.Views;
using Vazer.Core.Extensions;

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

        // ViewModels
        services.AddSingleton<InstancePickerViewModel>();
        services.AddTransient<ModBrowserViewModel>();
        services.AddTransient<InstanceDetailViewModel>();
        services.AddTransient<AccountViewModel>();

        // Pages / window
        services.AddTransient<ModBrowserPage>();
        services.AddTransient<InstanceDetailPage>();
        services.AddTransient<AccountPage>();
        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Dispatcher = DispatcherQueue.GetForCurrentThread();
        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Activate();
    }
}
