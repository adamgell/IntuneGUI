using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Classic.Avalonia.Theme;
using IntuneManager.Core.Extensions;
using IntuneManager.Desktop.Models;
using IntuneManager.Desktop.Services;
using IntuneManager.Desktop.ViewModels;
using IntuneManager.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Licensing;

namespace Intune.Commander.Desktop;

public partial class App : Application
{
    public static ServiceProvider? Services { get; private set; }
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Fluent;

    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        var app = Application.Current!;

        if (theme == AppTheme.Classic)
        {
            app.Styles[0] = new ClassicTheme();
            app.Styles[1] = new StyleInclude(new Uri("avares://IntuneManager.Desktop"))
            {
                Source = new Uri("avares://Classic.Avalonia.Theme.DataGrid/Classic.axaml")
            };
        }
        else
        {
            app.Styles[0] = new FluentTheme();
            app.Styles[1] = new StyleInclude(new Uri("avares://IntuneManager.Desktop"))
            {
                Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
            };
        }

        AppSettingsService.Save(new AppSettings { Theme = theme });
    }

    public override void Initialize()
    {
        // Register Syncfusion license using key from environment variable (see project documentation for licensing details)
        var syncfusionLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (!string.IsNullOrEmpty(syncfusionLicense))
        {
            SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
        }
        
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Apply saved theme (replaces default FluentTheme from AXAML if Classic was saved)
            var savedSettings = AppSettingsService.Load();
            if (savedSettings.Theme != AppTheme.Fluent)
                ApplyTheme(savedSettings.Theme);
            else
                CurrentTheme = AppTheme.Fluent;

            var services = new ServiceCollection();
            services.AddIntuneCommanderCore();
            services.AddTransient<MainWindowViewModel>();
            Services = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
