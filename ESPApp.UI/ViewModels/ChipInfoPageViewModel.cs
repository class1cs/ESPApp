using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ESPApp.UI.ViewModels;

public partial class ChipInfoPageViewModel : PageViewModelBase
{
    private readonly ConnectionViewModel _connection;

    public ChipInfoPageViewModel(ConnectionViewModel connection, EspToolExecutionViewModel execution)
        : base("О чипе", "Идентификаторы и параметры платы", "◈")
    {
        _connection = connection;
        Execution = execution;
    }

    public EspToolExecutionViewModel Execution { get; }

    [ObservableProperty] private string _sfdpAddress = "0x0";
    [ObservableProperty] private string _sfdpLength = "4";

    [RelayCommand]
    private async Task ChipIdAsync() =>
        await Execution.ExecuteAsync($"[{_connection.SelectedPort}] Чтение параметров чипа...", "chip-id");

    [RelayCommand]
    private async Task FlashIdAsync() =>
        await Execution.ExecuteAsync($"[{_connection.SelectedPort}] Чтение ID флеш-памяти...", "flash-id");

    [RelayCommand]
    private async Task ReadMacAsync() =>
        await Execution.ExecuteAsync($"[{_connection.SelectedPort}] Чтение MAC-адреса...", "read-mac");

    [RelayCommand]
    private async Task SecurityInfoAsync() =>
        await Execution.ExecuteAsync($"[{_connection.SelectedPort}] Чтение параметров безопасности...", "get-security-info");

    [RelayCommand]
    private async Task VersionAsync() =>
        await Execution.ExecuteAsync("Версия esptool...", "version", needsPort: false);

    [RelayCommand]
    private async Task ReadSfdpAsync()
    {
        if (!Execution.Require((SfdpAddress, "адрес"), (SfdpLength, "длина"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Чтение SFDP...",
            $"read-flash-sfdp {SfdpAddress.Trim()} {SfdpLength.Trim()}");
    }
}
