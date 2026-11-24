using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.UI.Services;

namespace ESPApp.UI.ViewModels;

public partial class ToolsPageViewModel : PageViewModelBase
{
    private readonly ConnectionViewModel _connection;
    private readonly FirmwarePageViewModel _firmware;

    public ToolsPageViewModel(
        ConnectionViewModel connection,
        EspToolExecutionViewModel execution,
        FirmwarePageViewModel firmware,
        IPageNavigationService navigationService)
        : base("Ещё", "", "", navigationService)
    {
        _connection = connection;
        Execution = execution;
        _firmware = firmware;
    }

    public EspToolExecutionViewModel Execution { get; }
    public IReadOnlyList<string> MergeFormats { get; } = new[] { "raw", "uf2", "hex" };

    [ObservableProperty] private string _memAddress = string.Empty;
    [ObservableProperty] private string _memValue = string.Empty;
    [ObservableProperty] private string _memMask = string.Empty;
    [ObservableProperty] private string _dumpAddress = string.Empty;
    [ObservableProperty] private string _dumpSize = string.Empty;
    [ObservableProperty] private string _dumpPath = string.Empty;
    [ObservableProperty] private string _loadRamPath = string.Empty;
    [ObservableProperty] private string _imageInfoPath = string.Empty;
    [ObservableProperty] private string _elfPath = string.Empty;
    [ObservableProperty] private string _elfOutputPath = string.Empty;
    [ObservableProperty] private string _mergeOutputPath = string.Empty;
    [ObservableProperty] private string _selectedMergeFormat = "raw";

    [RelayCommand]
    private async Task BrowseOpenAsync(string? target)
    {
        var path = await FilePickerHelper.PickOpenAsync("Выберите файл");
        if (path is null) return;

        switch (target)
        {
            case nameof(LoadRamPath): LoadRamPath = path; break;
            case nameof(ImageInfoPath): ImageInfoPath = path; break;
            case nameof(ElfPath): ElfPath = path; break;
        }

        Execution.AppendLog($"Выбран файл: {path}");
    }

    [RelayCommand]
    private async Task BrowseSaveAsync(string? target)
    {
        var suggested = target switch
        {
            nameof(ElfOutputPath) => "firmware.bin",
            nameof(MergeOutputPath) => "merged.bin",
            nameof(DumpPath) => "dump.bin",
            _ => "flash.bin"
        };

        var path = await FilePickerHelper.PickSaveAsync("Сохранить как", suggested);
        if (path is null) return;

        switch (target)
        {
            case nameof(DumpPath): DumpPath = path; break;
            case nameof(ElfOutputPath): ElfOutputPath = path; break;
            case nameof(MergeOutputPath): MergeOutputPath = path; break;
        }

        Execution.AppendLog($"Файл для сохранения: {path}");
    }

    [RelayCommand]
    private async Task ReadMemAsync()
    {
        if (!Execution.Require((MemAddress, "адрес"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Чтение памяти {MemAddress}...",
            new[] { "read-mem", MemAddress.Trim() });
    }

    [RelayCommand]
    private async Task WriteMemAsync()
    {
        if (!Execution.Require((MemAddress, "адрес"), (MemValue, "значение"))) return;

        var commandArguments = new List<string>
        {
            "write-mem",
            MemAddress.Trim(),
            MemValue.Trim()
        };
        if (!string.IsNullOrWhiteSpace(MemMask)) commandArguments.Add(MemMask.Trim());

        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Запись в память {MemAddress}...",
            commandArguments);
    }

    [RelayCommand]
    private async Task DumpMemAsync()
    {
        if (!Execution.Require((DumpAddress, "адрес"), (DumpSize, "размер"), (DumpPath, "файл"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Дамп памяти...",
            new[] { "dump-mem", DumpAddress.Trim(), DumpSize.Trim(), DumpPath.Trim() });
    }

    [RelayCommand]
    private async Task LoadRamAsync()
    {
        if (!Execution.Require((LoadRamPath, "файл"))) return;
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Загрузка в RAM...",
            new[] { "load-ram", LoadRamPath.Trim() });
    }

    [RelayCommand]
    private async Task RunCodeAsync() =>
        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Запуск загруженного кода...",
            new[] { "run" });

    [RelayCommand]
    private async Task ImageInfoAsync()
    {
        if (!Execution.Require((ImageInfoPath, "файл"))) return;
        await Execution.ExecuteAsync(
            "Чтение информации об образе...",
            new[] { "image-info", ImageInfoPath.Trim() },
            needsPort: false);
    }

    [RelayCommand]
    private async Task Elf2ImageAsync()
    {
        if (!Execution.Require((ElfPath, "ELF-файл"), (ElfOutputPath, "выходной файл"))) return;

        var commandArguments = new List<string> { "elf2image" };
        commandArguments.AddRange(_firmware.FlashOptions());
        commandArguments.Add("-o");
        commandArguments.Add(ElfOutputPath.Trim());
        commandArguments.Add(ElfPath.Trim());

        await Execution.ExecuteAsync(
            "Конвертация ELF в образ...",
            commandArguments,
            needsPort: false,
            needsChip: true);
    }

    [RelayCommand]
    private async Task MergeBinAsync()
    {
        if (!Execution.Require((MergeOutputPath, "выходной файл"))
            || !_firmware.TryGetFlashPairs(out var pairs)) return;

        var commandArguments = new List<string>
        {
            "merge-bin",
            "-o",
            MergeOutputPath.Trim(),
            "--format",
            SelectedMergeFormat.Trim()
        };
        commandArguments.AddRange(_firmware.FlashOptions());
        commandArguments.AddRange(pairs);

        await Execution.ExecuteAsync(
            "Слияние образов...",
            commandArguments,
            needsPort: false,
            needsChip: true);
    }
}
