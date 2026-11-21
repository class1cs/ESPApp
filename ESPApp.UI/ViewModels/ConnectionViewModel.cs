using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ESPApp.UI.ViewModels;

public partial class ConnectionViewModel : ViewModelBase
{
    public event Action<string>? LogMessage;

    public IReadOnlyList<string> Chips { get; } = new[]
    {
        "auto", "esp8266", "esp32", "esp32s2", "esp32s3", "esp32c3",
        "esp32c2", "esp32c6", "esp32c61", "esp32c5", "esp32e22",
        "esp32h2", "esp32h21", "esp32p4", "esp32h4", "esp32s31"
    };

    public IReadOnlyList<string> Bauds { get; } = new[]
    {
        "115200", "230400", "460800", "921600", "1500000", "2000000"
    };

    public IReadOnlyList<string> BeforeModes { get; } = new[]
    {
        "default-reset", "usb-reset", "no-reset", "no-reset-no-sync"
    };

    public IReadOnlyList<string> AfterModes { get; } = new[]
    {
        "hard-reset", "soft-reset", "no-reset", "no-reset-stub", "watchdog-reset"
    };

    [ObservableProperty]
    private ObservableCollection<string> _availablePorts = new();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private string _selectedChip = "auto";

    [ObservableProperty]
    private string _selectedBaud = "460800";

    [ObservableProperty]
    private string _selectedBefore = "default-reset";

    [ObservableProperty]
    private string _selectedAfter = "hard-reset";

    [ObservableProperty]
    private bool _noStub;

    [ObservableProperty]
    private bool _trace;

    [ObservableProperty]
    private int _connectAttempts = 7;

    public void ApplyDefaults(ESPApp.Infrastructure.Models.AppSettings settings)
    {
        if (Chips.Contains(settings.DefaultChip))
        {
            SelectedChip = settings.DefaultChip;
        }

        if (Bauds.Contains(settings.DefaultBaud))
        {
            SelectedBaud = settings.DefaultBaud;
        }
    }

    [RelayCommand]
    public void RefreshPorts()
    {
        var previous = SelectedPort;
        AvailablePorts.Clear();

        foreach (var port in SerialPort.GetPortNames().OrderBy(
                     port => port, StringComparer.OrdinalIgnoreCase))
        {
            AvailablePorts.Add(port);
        }

        if (AvailablePorts.Count > 0)
        {
            SelectedPort = previous != null && AvailablePorts.Contains(previous)
                ? previous
                : AvailablePorts[0];
            LogMessage?.Invoke($"Найдено портов: {AvailablePorts.Count}");
        }
        else
        {
            SelectedPort = null;
            LogMessage?.Invoke("Предупреждение: COM-порты не обнаружены. Вероятно, ESP32 не подключён.");
        }
    }
}
