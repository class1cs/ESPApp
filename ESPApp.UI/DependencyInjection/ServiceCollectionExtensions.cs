using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;
using ESPApp.Infrastructure.Services;
using ESPApp.UI.Services;
using ESPApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ESPApp.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddEspApp(this IServiceCollection services)
    {
        services.TryAddSingleton<EspToolService>(_ => new EspToolService());
        services.TryAddSingleton<IEspToolService>(sp => sp.GetRequiredService<EspToolService>());
        services.TryAddSingleton<IEspToolConfigurator>(sp => sp.GetRequiredService<EspToolService>());
        services.TryAddSingleton<ISettingsService>(_ => new SettingsService());
        services.TryAddSingleton<AppSettings>(sp => sp.GetRequiredService<ISettingsService>().Load());

        services.TryAddSingleton<IPageNavigationService, PageNavigationService>();

        services.TryAddSingleton<ConnectionViewModel>();
        services.TryAddSingleton<EspToolExecutionViewModel>();

        services.TryAddSingleton<FirmwarePageViewModel>();
        services.TryAddSingleton<MemoryPageViewModel>();
        services.TryAddSingleton<ChipInfoPageViewModel>();
        services.TryAddSingleton<ToolsPageViewModel>();
        services.TryAddSingleton<WelcomeViewModel>();

        services.TryAddSingleton<MainViewModel>();

        return services;
    }
}
