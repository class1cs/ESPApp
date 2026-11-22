using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.UI.Services;

namespace ESPApp.UI.ViewModels;

public partial class MemoryPageViewModel : PageViewModelBase
{
    private readonly ConnectionViewModel _connection;

    public MemoryPageViewModel(ConnectionViewModel connection, EspToolExecutionViewModel execution, IPageNavigationService navigationService)
        : base("Память", "Чтение, стирание и защита flash", "▦", navigationService)
    {
        _connection = connection;
        Execution = execution;
    }

    public EspToolExecutionViewModel Execution { get; }
    public IReadOnlyList<string> StatusBytesOptions { get; } = new[] { "1", "2", "3" };

    [ObservableProperty] private string _eraseRegionAddress = string.Empty;
    [ObservableProperty] private string _eraseRegionSize = string.Empty;
    [ObservableProperty] private string _readAddress = "0x0";
    [ObservableProperty] private string _readSize = "ALL";
    [ObservableProperty] private string _readPath = string.Empty;
    [ObservableProperty] private string _selectedStatusBytes = "2";
    [ObservableProperty] private string _statusValue = string.Empty;
    [ObservableProperty] private bool _statusNonVolatile;

    [RelayCommand]
    private async Task EraseFlashAsync() =>
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Полное стирание памяти чипа...",
            new[] { "erase-flash" });

    [RelayCommand]
    private async Task EraseRegionAsync()
    {
        if (!Execution.Require((EraseRegionAddress, "адрес"), (EraseRegionSize, "размер"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Стирание региона {EraseRegionAddress}...",
            new[] { "erase-region", EraseRegionAddress.Trim(), EraseRegionSize.Trim() });
    }

    [RelayCommand]
    private async Task ReadFlashAsync()
    {
        if (!Execution.Require((ReadAddress, "адрес"), (ReadSize, "размер"), (ReadPath, "файл"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Чтение flash в файл...",
            new[] { "read-flash", ReadAddress.Trim(), ReadSize.Trim(), ReadPath.Trim() });
    }

    [RelayCommand]
    private async Task ReadFlashStatusAsync() =>
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Чтение регистра статуса...",
            new[] { "read-flash-status", "--bytes", SelectedStatusBytes.Trim() });

    [RelayCommand]
    private async Task WriteFlashStatusAsync()
    {
        if (!Execution.Require((StatusValue, "значение"))) return;

        var commandArguments = new List<string> { "write-flash-status" };
        if (StatusNonVolatile) commandArguments.Add("--non-volatile");
        commandArguments.Add("--bytes");
        commandArguments.Add(SelectedStatusBytes.Trim());
        commandArguments.Add(StatusValue.Trim());

        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Запись регистра статуса...",
            commandArguments);
    }

    [RelayCommand]
    private async Task BrowseSaveAsync(string? target)
    {
        if (target != nameof(ReadPath)) return;
        var path = await FilePickerHelper.PickSaveAsync("Сохранить дамп flash", "flash-backup.bin");
        if (path is null) return;
        ReadPath = path;
        Execution.AppendLog($"Файл для сохранения: {path}");
    }
}
