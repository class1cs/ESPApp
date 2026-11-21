using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.UI.ViewModels;


public partial class MainViewModel : ViewModelBase
{
    public ConnectionViewModel Connection { get; }
    public EspToolExecutionViewModel Execution { get; }

    public FirmwarePageViewModel Firmware { get; }
    public MemoryPageViewModel Memory { get; }
    public ChipInfoPageViewModel ChipInfo { get; }
    public ToolsPageViewModel Tools { get; }

    public IReadOnlyList<PageNavigationItemViewModel> NavigationPages { get; }
    public WelcomeViewModel? Welcome { get; }

    [ObservableProperty]
    private PageViewModelBase _currentPage = null!;

    [ObservableProperty]
    private bool _isWelcomeVisible;

    public MainViewModel(
        IEspToolService espToolService,
        IEspToolConfigurator? configurator = null,
        ISettingsService? settingsService = null)
    {
        Connection = new ConnectionViewModel();
        Execution = new EspToolExecutionViewModel(espToolService, Connection);
        Connection.LogMessage += Execution.AppendLog;

        Firmware = new FirmwarePageViewModel(Connection, Execution);
        Memory = new MemoryPageViewModel(Connection, Execution);
        ChipInfo = new ChipInfoPageViewModel(Connection, Execution);
        Tools = new ToolsPageViewModel(Connection, Execution, Firmware);

        NavigationPages = new PageNavigationItemViewModel[]
        {
            new(Firmware, NavigateTo),
            new(Memory, NavigateTo),
            new(ChipInfo, NavigateTo),
            new(Tools, NavigateTo)
        };

        CurrentPage = Firmware;
        NavigationPages[0].IsSelected = true;

        Connection.RefreshPorts();

        if (configurator is null || settingsService is null)
        {
            return;
        }

        var settings = settingsService.Load();
        Connection.ApplyDefaults(settings);

        if (settings.IsConfigured)
        {
            configurator.Use(settings.ToSource());
        }

        Welcome = new WelcomeViewModel(
            configurator,
            settingsService,
            settings,
            Connection.Chips,
            Connection.Bauds,
            OnWelcomeApplied,
            () => IsWelcomeVisible = false);

        IsWelcomeVisible = !settings.IsConfigured || settings.ShowWelcomeOnStart;
        if (IsWelcomeVisible)
        {
            _ = Welcome.DetectAsync();
        }
    }

    [RelayCommand]
    private void ShowWelcome()
    {
        if (Welcome is null)
        {
            return;
        }

        Welcome.Reset();
        IsWelcomeVisible = true;
        _ = Welcome.DetectAsync();
    }

    private void NavigateTo(PageViewModelBase page)
    {
        foreach (var item in NavigationPages)
        {
            item.IsSelected = ReferenceEquals(item.Page, page);
        }

        CurrentPage = page;
    }

    private void OnWelcomeApplied(AppSettings settings, bool saved)
    {
        Connection.ApplyDefaults(settings);
        IsWelcomeVisible = false;
        Execution.AppendLog(saved
            ? "Настройки сохранены."
            : "Предупреждение: не удалось сохранить настройки на диск.");
    }
}
