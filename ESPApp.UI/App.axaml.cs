using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Services;
using ESPApp.UI.ViewModels;
using ESPApp.UI.Views;

namespace ESPApp.UI;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<EspToolService>();
        services.AddSingleton<IEspToolService>(sp => sp.GetRequiredService<EspToolService>());
        services.AddSingleton<IEspToolConfigurator>(sp => sp.GetRequiredService<EspToolService>());
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = Services.GetRequiredService<MainViewModel>();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}