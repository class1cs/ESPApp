using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;
using ESPApp.UI.Services;

namespace ESPApp.UI.ViewModels;


public partial class MainViewModel : ViewModelBase
{
    private readonly IPageNavigationService _navigationService;

    public ConnectionViewModel Connection { get; }
    public EspToolExecutionViewModel Execution { get; }

    public FirmwarePageViewModel Firmware { get; }
    public MemoryPageViewModel Memory { get; }
    public ChipInfoPageViewModel ChipInfo { get; }
    public ToolsPageViewModel Tools { get; }

    public IReadOnlyList<PageViewModelBase> NavigationPages { get; }
    public WelcomeViewModel Welcome { get; }

    [ObservableProperty]
    private PageViewModelBase _currentPage = null!;

    [ObservableProperty]
    private bool _isWelcomeVisible;

    public MainViewModel(
        ConnectionViewModel connection,
        EspToolExecutionViewModel execution,
        FirmwarePageViewModel firmware,
        MemoryPageViewModel memory,
        ChipInfoPageViewModel chipInfo,
        ToolsPageViewModel tools,
        WelcomeViewModel welcome,
        IPageNavigationService navigationService,
        AppSettings settings,
        IEspToolConfigurator configurator)
    {
        Connection = connection;
        Execution = execution;
        Firmware = firmware;
        Memory = memory;
        ChipInfo = chipInfo;
        Tools = tools;
        Welcome = welcome;
        _navigationService = navigationService;

        NavigationPages = new PageViewModelBase[]
        {
            Firmware,
            Memory,
            ChipInfo,
            Tools
        };

        Connection.LogMessage += Execution.AppendLog;
        _navigationService.CurrentPageChanged
            += HandleNavigationPageChanged;
        CurrentPage = _navigationService.CurrentPage ?? Firmware;
        _navigationService.NavigateTo(CurrentPage);

        Connection.RefreshPorts();
        Connection.ApplyDefaults(settings);

        if (settings.IsConfigured)
        {
            configurator.Use(settings.ToSource());
        }

        Welcome.SettingsApplied += OnWelcomeApplied;
        Welcome.CloseRequested += OnWelcomeClosed;

        IsWelcomeVisible = !settings.IsConfigured || settings.ShowWelcomeOnStart;
        if (IsWelcomeVisible)
        {
            _ = Welcome.DetectAsync();
        }
    }

    [RelayCommand]
    private void ShowWelcome()
    {
        Welcome.Reset();
        IsWelcomeVisible = true;
        _ = Welcome.DetectAsync();
    }

    private void HandleNavigationPageChanged(
        PageViewModelBase? page)
    {
        if (page is not null)
            CurrentPage = page;
    }

    private void OnWelcomeClosed() => IsWelcomeVisible = false;

    private void OnWelcomeApplied(AppSettings settings, bool saved)
    {
        Connection.ApplyDefaults(settings);
        IsWelcomeVisible = false;
        Execution.AppendLog(saved
            ? "Настройки сохранены."
            : "Предупреждение: не удалось сохранить настройки на диск.");
    }
}
