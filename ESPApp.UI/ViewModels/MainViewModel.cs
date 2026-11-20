using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;

namespace ESPApp.UI.ViewModels
{
    public partial class FlashItem : ObservableObject
    {
        private readonly Func<FlashItem, Task> _browse;
        private readonly Action<FlashItem> _remove;

        [ObservableProperty]
        private string _address;

        [ObservableProperty]
        private string _filePath = string.Empty;

        public FlashItem(string address, Func<FlashItem, Task> browse, Action<FlashItem> remove)
        {
            _address = address;
            _browse = browse;
            _remove = remove;
        }

        [RelayCommand]
        private Task BrowseAsync() => _browse(this);

        [RelayCommand]
        private void Remove() => _remove(this);
    }

    public partial class MainViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

        [RelayCommand]
        private void Cancel()
        {
            if (_cts is null)
            {
                return;
            }

            AppendLog("Отмена...");
            _cts.Cancel();
        }

        private readonly IEspToolService _espToolService;

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

        public IReadOnlyList<string> FlashModes { get; } = new[]
        {
            "keep", "qio", "qout", "dio", "dout"
        };

        public IReadOnlyList<string> FlashFreqs { get; } = new[]
        {
            "keep", "80m", "64m", "60m", "48m", "40m", "32m", "30m", "26m", "24m", "20m", "16m", "15m", "12m"
        };

        public IReadOnlyList<string> FlashSizes { get; } = new[]
        {
            "keep", "detect", "256KB", "512KB", "1MB", "2MB", "2MB-c1", "4MB", "4MB-c1",
            "8MB", "16MB", "32MB", "64MB", "128MB"
        };

        public IReadOnlyList<string> StatusBytesOptions { get; } = new[] { "1", "2", "3" };

        public IReadOnlyList<string> MergeFormats { get; } = new[] { "raw", "uf2", "hex" };

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

        [ObservableProperty]
        private ObservableCollection<FlashItem> _flashItems = new();

        [ObservableProperty]
        private string _flashMode = "keep";

        [ObservableProperty]
        private string _flashFreq = "keep";

        [ObservableProperty]
        private string _flashSize = "keep";

        [ObservableProperty]
        private bool _eraseAll;

        [ObservableProperty]
        private bool _noCompress;

        [ObservableProperty]
        private bool _force;

        [ObservableProperty]
        private bool _verifyDiff;

        [ObservableProperty]
        private string _eraseRegionAddress = string.Empty;

        [ObservableProperty]
        private string _eraseRegionSize = string.Empty;

        [ObservableProperty]
        private string _readAddress = "0x0";

        [ObservableProperty]
        private string _readSize = "ALL";

        [ObservableProperty]
        private string _readPath = string.Empty;

        [ObservableProperty]
        private string _selectedStatusBytes = "2";

        [ObservableProperty]
        private string _statusValue = string.Empty;

        [ObservableProperty]
        private bool _statusNonVolatile;

        [ObservableProperty]
        private string _sfdpAddress = "0x0";

        [ObservableProperty]
        private string _sfdpLength = "4";

        [ObservableProperty]
        private string _memAddress = string.Empty;

        [ObservableProperty]
        private string _memValue = string.Empty;

        [ObservableProperty]
        private string _memMask = string.Empty;

        [ObservableProperty]
        private string _dumpAddress = string.Empty;

        [ObservableProperty]
        private string _dumpSize = string.Empty;

        [ObservableProperty]
        private string _dumpPath = string.Empty;

        [ObservableProperty]
        private string _loadRamPath = string.Empty;

        [ObservableProperty]
        private string _imageInfoPath = string.Empty;

        [ObservableProperty]
        private string _elfPath = string.Empty;

        [ObservableProperty]
        private string _elfOutputPath = string.Empty;

        [ObservableProperty]
        private string _mergeOutputPath = string.Empty;

        [ObservableProperty]
        private string _selectedMergeFormat = "raw";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _consoleOutput = "Готов к работе.\n";

        public MainViewModel(IEspToolService espToolService)
        {
            _espToolService = espToolService;
            FlashItems.Add(CreateFlashItem("0x1000"));
            RefreshPorts();
        }

        [RelayCommand]
        private void RefreshPorts()
        {
            var previous = SelectedPort;
            AvailablePorts.Clear();

            foreach (var port in SerialPort.GetPortNames().OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                AvailablePorts.Add(port);
            }

            if (AvailablePorts.Count > 0)
            {
                SelectedPort = previous != null && AvailablePorts.Contains(previous) ? previous : AvailablePorts[0];
                AppendLog($"Найдено портов: {AvailablePorts.Count}");
            }
            else
            {
                SelectedPort = null;
                AppendLog("Предупреждение: COM-порты не обнаружены. Вероятно, ESP32 не подключён.");
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            ConsoleOutput = string.Empty;
        }

        [RelayCommand]
        private void AddFlashItem()
        {
            FlashItems.Add(CreateFlashItem("0x1"));
        }

        [RelayCommand]
        private async Task BrowseOpenAsync(string? target)
        {
            var path = await PickOpenAsync("Выберите файл");
            if (path is null)
            {
                return;
            }

            switch (target)
            {
                case nameof(LoadRamPath):
                    LoadRamPath = path;
                    break;
                case nameof(ImageInfoPath):
                    ImageInfoPath = path;
                    break;
                case nameof(ElfPath):
                    ElfPath = path;
                    break;
            }

            AppendLog($"Выбран файл: {path}");
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

            var path = await PickSaveAsync("Сохранить как", suggested);
            if (path is null)
            {
                return;
            }

            switch (target)
            {
                case nameof(ReadPath):
                    ReadPath = path;
                    break;
                case nameof(DumpPath):
                    DumpPath = path;
                    break;
                case nameof(ElfOutputPath):
                    ElfOutputPath = path;
                    break;
                case nameof(MergeOutputPath):
                    MergeOutputPath = path;
                    break;
            }

            AppendLog($"Файл для сохранения: {path}");
        }

        [RelayCommand]
        private async Task FlashAsync()
        {
            if (!TryGetFlashPairs(out var pairs))
            {
                return;
            }

            var sb = new StringBuilder("write-flash ");
            sb.Append(FlashOptions());

            if (EraseAll) sb.Append(" --erase-all");
            if (NoCompress) sb.Append(" --no-compress");
            if (Force) sb.Append(" --force");

            sb.Append(' ').Append(pairs);

            await ExecuteAsync($"[{SelectedPort}] Запись прошивки...", sb.ToString());
        }

        [RelayCommand]
        private async Task VerifyFlashAsync()
        {
            if (!TryGetFlashPairs(out var pairs))
            {
                return;
            }

            var command = "verify-flash " + (VerifyDiff ? "--diff " : string.Empty) + pairs;
            await ExecuteAsync($"[{SelectedPort}] Проверка прошивки...", command);
        }

        [RelayCommand]
        private async Task EraseFlashAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Полное стирание памяти чипа...", "erase-flash");
        }

        [RelayCommand]
        private async Task EraseRegionAsync()
        {
            if (!Require((EraseRegionAddress, "адрес"), (EraseRegionSize, "размер")))
            {
                return;
            }

            await ExecuteAsync(
                $"[{SelectedPort}] Стирание региона {EraseRegionAddress}...",
                $"erase-region {EraseRegionAddress.Trim()} {EraseRegionSize.Trim()}");
        }

        [RelayCommand]
        private async Task ReadFlashAsync()
        {
            if (!Require((ReadAddress, "адрес"), (ReadSize, "размер"), (ReadPath, "файл")))
            {
                return;
            }

            await ExecuteAsync(
                $"[{SelectedPort}] Чтение flash в файл...",
                $"read-flash {ReadAddress.Trim()} {ReadSize.Trim()} {Q(ReadPath)}");
        }

        [RelayCommand]
        private async Task ReadFlashStatusAsync()
        {
            await ExecuteAsync(
                $"[{SelectedPort}] Чтение регистра статуса...",
                $"read-flash-status --bytes {SelectedStatusBytes}");
        }

        [RelayCommand]
        private async Task WriteFlashStatusAsync()
        {
            if (!Require((StatusValue, "значение")))
            {
                return;
            }

            var sb = new StringBuilder("write-flash-status");
            if (StatusNonVolatile) sb.Append(" --non-volatile");
            sb.Append($" --bytes {SelectedStatusBytes} {StatusValue.Trim()}");

            await ExecuteAsync($"[{SelectedPort}] Запись регистра статуса...", sb.ToString());
        }

        [RelayCommand]
        private async Task ChipIdAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Чтение параметров чипа...", "chip-id");
        }

        [RelayCommand]
        private async Task FlashIdAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Чтение ID флеш-памяти...", "flash-id");
        }

        [RelayCommand]
        private async Task ReadMacAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Чтение MAC-адреса...", "read-mac");
        }

        [RelayCommand]
        private async Task SecurityInfoAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Чтение параметров безопасности...", "get-security-info");
        }

        [RelayCommand]
        private async Task VersionAsync()
        {
            await ExecuteAsync("Версия esptool...", "version", needsPort: false);
        }

        [RelayCommand]
        private async Task ReadSfdpAsync()
        {
            if (!Require((SfdpAddress, "адрес"), (SfdpLength, "длина")))
            {
                return;
            }

            await ExecuteAsync(
                $"[{SelectedPort}] Чтение SFDP...",
                $"read-flash-sfdp {SfdpAddress.Trim()} {SfdpLength.Trim()}");
        }

        [RelayCommand]
        private async Task ReadMemAsync()
        {
            if (!Require((MemAddress, "адрес")))
            {
                return;
            }

            await ExecuteAsync($"[{SelectedPort}] Чтение памяти {MemAddress}...", $"read-mem {MemAddress.Trim()}");
        }

        [RelayCommand]
        private async Task WriteMemAsync()
        {
            if (!Require((MemAddress, "адрес"), (MemValue, "значение")))
            {
                return;
            }

            var command = $"write-mem {MemAddress.Trim()} {MemValue.Trim()}";
            if (!string.IsNullOrWhiteSpace(MemMask))
            {
                command += $" {MemMask.Trim()}";
            }

            await ExecuteAsync($"[{SelectedPort}] Запись в память {MemAddress}...", command);
        }

        [RelayCommand]
        private async Task DumpMemAsync()
        {
            if (!Require((DumpAddress, "адрес"), (DumpSize, "размер"), (DumpPath, "файл")))
            {
                return;
            }

            await ExecuteAsync(
                $"[{SelectedPort}] Дамп памяти...",
                $"dump-mem {DumpAddress.Trim()} {DumpSize.Trim()} {Q(DumpPath)}");
        }

        [RelayCommand]
        private async Task LoadRamAsync()
        {
            if (!Require((LoadRamPath, "файл")))
            {
                return;
            }

            await ExecuteAsync($"[{SelectedPort}] Загрузка в RAM...", $"load-ram {Q(LoadRamPath)}");
        }

        [RelayCommand]
        private async Task RunCodeAsync()
        {
            await ExecuteAsync($"[{SelectedPort}] Запуск загруженного кода...", "run");
        }

        [RelayCommand]
        private async Task ImageInfoAsync()
        {
            if (!Require((ImageInfoPath, "файл")))
            {
                return;
            }

            await ExecuteAsync("Чтение информации об образе...", $"image-info {Q(ImageInfoPath)}", needsPort: false);
        }

        [RelayCommand]
        private async Task Elf2ImageAsync()
        {
            if (!Require((ElfPath, "ELF-файл"), (ElfOutputPath, "выходной файл")))
            {
                return;
            }

            var command = $"elf2image {FlashOptions()} -o {Q(ElfOutputPath)} {Q(ElfPath)}";
            await ExecuteAsync("Конвертация ELF в образ...", command, needsPort: false, needsChip: true);
        }

        [RelayCommand]
        private async Task MergeBinAsync()
        {
            if (!Require((MergeOutputPath, "выходной файл")) || !TryGetFlashPairs(out var pairs))
            {
                return;
            }

            var command = $"merge-bin -o {Q(MergeOutputPath)} --format {SelectedMergeFormat} {FlashOptions()} {pairs}";
            await ExecuteAsync("Слияние образов...", command, needsPort: false, needsChip: true);
        }

        private async Task ExecuteAsync(string title, string command, bool needsPort = true, bool needsChip = false)
        {
            if (IsBusy)
            {
                return;
            }

            if (needsPort && string.IsNullOrWhiteSpace(SelectedPort))
            {
                AppendLog("Ошибка: не выбран COM-порт!");
                return;
            }

            if (needsChip && SelectedChip == "auto")
            {
                AppendLog("Ошибка: для этой команды нужно выбрать конкретный чип.");
                return;
            }

            IsBusy = true;
            _cts = new CancellationTokenSource();
            AppendLog(title);

            try
            {
                var arguments = $"{BuildGlobalArguments(needsPort)} {command}".Trim();
                await _espToolService.ExecuteCommandAsync(arguments, AppendLog, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("Операция отменена.");
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                IsBusy = false;
            }
        }

        private string BuildGlobalArguments(bool online)
        {
            var parts = new List<string>();

            if (SelectedChip != "auto")
            {
                parts.Add($"--chip {SelectedChip}");
            }

            if (online)
            {
                parts.Add($"--port {SelectedPort}");
                parts.Add($"--baud {SelectedBaud}");
                parts.Add($"--before {SelectedBefore}");
                parts.Add($"--after {SelectedAfter}");
                parts.Add($"--connect-attempts {Math.Max(0, ConnectAttempts)}");

                if (NoStub) parts.Add("--no-stub");
                if (Trace) parts.Add("--trace");
            }

            return string.Join(" ", parts);
        }

        private string FlashOptions()
        {
            return $"--flash-mode {FlashMode} --flash-freq {FlashFreq} --flash-size {FlashSize}";
        }

        private bool TryGetFlashPairs(out string pairs)
        {
            pairs = string.Empty;

            var items = FlashItems.ToList();
            var invalid = items.Count == 0
                          || items.Any(i => string.IsNullOrWhiteSpace(i.Address) || string.IsNullOrWhiteSpace(i.FilePath));

            if (invalid)
            {
                AppendLog("Ошибка: добавьте файлы и укажите адрес каждому.");
                return false;
            }

            pairs = string.Join(" ", items.Select(i => $"{i.Address.Trim()} {Q(i.FilePath)}"));
            return true;
        }

        private bool Require(params (string? Value, string Name)[] fields)
        {
            var missing = fields.Where(f => string.IsNullOrWhiteSpace(f.Value)).Select(f => f.Name).ToList();
            if (missing.Count == 0)
            {
                return true;
            }

            AppendLog($"Ошибка: заполните поля: {string.Join(", ", missing)}");
            return false;
        }

        private FlashItem CreateFlashItem(string address)
        {
            return new FlashItem(address, BrowseFlashItemAsync, RemoveFlashItem);
        }

        private async Task BrowseFlashItemAsync(FlashItem item)
        {
            var path = await PickOpenAsync("Выберите файл прошивки");
            if (path is null)
            {
                return;
            }

            item.FilePath = path;
            AppendLog($"Выбран файл: {path}");
        }

        private void RemoveFlashItem(FlashItem item)
        {
            FlashItems.Remove(item);
        }

        private static string Q(string value)
        {
            return $"\"{value.Trim()}\"";
        }

        private static Window? GetMainWindow()
        {
            return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        private static async Task<string?> PickOpenAsync(string title)
        {
            var window = GetMainWindow();
            if (window is null)
            {
                return null;
            }

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            return files.Count > 0 ? files[0].TryGetLocalPath() : null;
        }

        private static async Task<string?> PickSaveAsync(string title, string suggestedName)
        {
            var window = GetMainWindow();
            if (window is null)
            {
                return null;
            }

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName
            });

            return file?.TryGetLocalPath();
        }

        private void AppendLog(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss} > {message}\n";

            if (Dispatcher.UIThread.CheckAccess())
            {
                AppendLine(line);
            }
            else
            {
                Dispatcher.UIThread.Post(() => AppendLine(line));
            }
        }

        private void AppendLine(string line)
        {
            ConsoleOutput += line;
        }
    }
}