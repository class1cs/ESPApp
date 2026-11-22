using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;
using ESPApp.UI.DependencyInjection;
using ESPApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ESPApp.Tests;

/// <summary>
/// Builds the real application DI graph while replacing external dependencies
/// with deterministic test doubles. Tests resolve the ViewModel they actually test
/// instead of going through MainViewModel or a flattened test facade.
/// </summary>
internal sealed class TestApplicationHost : IDisposable
{
    private readonly ServiceProvider _provider;

    private TestApplicationHost(
        ServiceProvider provider,
        RecordingEspToolService espToolService,
        FakeSettingsService settingsService,
        FakeEspToolConfigurator configurator)
    {
        _provider = provider;
        EspToolService = espToolService;
        SettingsService = settingsService;
        Configurator = configurator;
    }

    public RecordingEspToolService EspToolService { get; }
    public FakeSettingsService SettingsService { get; }
    public FakeEspToolConfigurator Configurator { get; }

    public ConnectionViewModel Connection => GetRequired<ConnectionViewModel>();
    public EspToolExecutionViewModel Execution => GetRequired<EspToolExecutionViewModel>();
    public FirmwarePageViewModel Firmware => GetRequired<FirmwarePageViewModel>();
    public MemoryPageViewModel Memory => GetRequired<MemoryPageViewModel>();
    public ChipInfoPageViewModel ChipInfo => GetRequired<ChipInfoPageViewModel>();
    public ToolsPageViewModel Tools => GetRequired<ToolsPageViewModel>();
    public WelcomeViewModel Welcome => GetRequired<WelcomeViewModel>();
    public MainViewModel Main => GetRequired<MainViewModel>();

    public T GetRequired<T>() where T : notnull => _provider.GetRequiredService<T>();

    public static TestApplicationHost Create(AppSettings? settings = null)
    {
        settings ??= new AppSettings
        {
            IsConfigured = true,
            ShowWelcomeOnStart = false
        };

        var espToolService = new RecordingEspToolService();
        var settingsService = new FakeSettingsService(settings);
        var configurator = new FakeEspToolConfigurator();

        var services = new ServiceCollection();
        services.AddSingleton<IEspToolService>(espToolService);
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddSingleton<IEspToolConfigurator>(configurator);
        services.AddEspApp();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return new TestApplicationHost(provider, espToolService, settingsService, configurator);
    }

    public void Dispose() => _provider.Dispose();
}
