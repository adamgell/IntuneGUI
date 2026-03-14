using System.Windows;
using Intune.Commander.Core.Extensions;
using Intune.Commander.DesktopReact.Bridge;
using Intune.Commander.DesktopReact.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Intune.Commander.DesktopReact;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddIntuneCommanderCore();

        // Bridge services
        services.AddSingleton<ShellStateBridgeService>();
        services.AddSingleton<ProfileBridgeService>();
        services.AddSingleton<AuthBridgeService>();
        services.AddSingleton<NavigationBridgeService>();
        services.AddSingleton<SettingsCatalogBridgeService>();
        services.AddSingleton<SearchBridgeService>();
        services.AddSingleton<BridgeRouter>();

        Services = services.BuildServiceProvider();
    }
}
