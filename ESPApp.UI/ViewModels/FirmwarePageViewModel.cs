using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.UI.Services;
using ESPApp.Infrastructure.Models;

namespace ESPApp.UI.ViewModels;

public partial class FirmwarePageViewModel : PageViewModelBase
{
    private readonly ConnectionViewModel _connection;

    public FirmwarePageViewModel(ConnectionViewModel connection, EspToolExecutionViewModel execution, IPageNavigationService navigationService)
        : base("Прошивка", "Запись и проверка образов ESP", "▤", navigationService)
    {
        _connection = connection;
        Execution = execution;
        FlashItems.CollectionChanged += OnFlashItemsCollectionChanged;
        _connection.PropertyChanged += OnConnectionPropertyChanged;
        FlashItems.Add(CreateFlashItem("0x1000"));
    }

    public EspToolExecutionViewModel Execution { get; }

    public IReadOnlyList<string> FlashModes { get; } = new[] { "keep", "qio", "qout", "dio", "dout" };
    public IReadOnlyList<string> FlashFreqs { get; } = new[]
    {
        "keep", "80m", "64m", "60m", "48m", "40m", "32m", "30m", "26m", "24m", "20m", "16m", "15m", "12m"
    };
    public IReadOnlyList<string> FlashSizes { get; } = new[]
    {
        "keep", "detect", "256KB", "512KB", "1MB", "2MB", "2MB-c1", "4MB", "4MB-c1",
        "8MB", "16MB", "32MB", "64MB", "128MB"
    };

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
    private bool _isVerificationRunning;

    [ObservableProperty]
    private bool _isVerificationSuccessful;

    [ObservableProperty]
    private bool _isVerificationFailed;

    [ObservableProperty]
    private bool _isVerificationCancelled;

    [ObservableProperty]
    private string _verificationStatusText = string.Empty;

    [RelayCommand]
    private void AddFlashItem() => FlashItems.Add(CreateFlashItem("0x1"));

    [RelayCommand]
    private async Task FlashAsync()
    {
        if (!TryGetFlashPairs(out var pairs)) return;
        ClearVerificationStatus();

        var commandArguments = new List<string> { "write-flash" };
        commandArguments.AddRange(FlashOptions());
        if (EraseAll) commandArguments.Add("--erase-all");
        if (NoCompress) commandArguments.Add("--no-compress");
        if (Force) commandArguments.Add("--force");
        commandArguments.AddRange(pairs);

        await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Запись прошивки...",
            commandArguments);
    }

    [RelayCommand]
    private async Task VerifyFlashAsync()
    {
        ClearVerificationStatus();

        if (!TryGetFlashPairs(out var pairs))
        {
            IsVerificationFailed = true;
            VerificationStatusText = "Укажите файл прошивки и адрес.";
            return;
        }

        IsVerificationRunning = true;
        var commandArguments = new List<string> { "verify-flash" };
        if (VerifyDiff) commandArguments.Add("--diff");
        commandArguments.AddRange(pairs);

        var result = await Execution.ExecuteAsync(
            $"[{_connection.SelectedPort}] Проверка прошивки...",
            commandArguments);

        IsVerificationRunning = false;
        switch (result.Status)
        {
            case EsptoolCommandStatus.Succeeded:
                IsVerificationSuccessful = true;
                VerificationStatusText = "Проверка пройдена";
                break;
            case EsptoolCommandStatus.Cancelled:
                IsVerificationCancelled = true;
                VerificationStatusText = "Проверка отменена";
                break;
            case EsptoolCommandStatus.Rejected:
                IsVerificationFailed = true;
                VerificationStatusText = result.Message ?? "Проверка не запущена";
                break;
            default:
                IsVerificationFailed = true;
                VerificationStatusText = result.ExitCode.HasValue
                    ? $"Проверка не пройдена (код: {result.ExitCode.Value})"
                    : "Проверка не пройдена";
                break;
        }
    }

    private void ClearVerificationStatus()
    {
        IsVerificationRunning = false;
        IsVerificationSuccessful = false;
        IsVerificationFailed = false;
        IsVerificationCancelled = false;
        VerificationStatusText = string.Empty;
    }

    private void OnFlashItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (FlashItem item in e.OldItems)
            {
                item.PropertyChanged -= OnFlashItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (FlashItem item in e.NewItems)
            {
                item.PropertyChanged += OnFlashItemPropertyChanged;
            }
        }

        ClearVerificationStatus();
    }

    private void OnFlashItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlashItem.Address) or nameof(FlashItem.FilePath))
        {
            ClearVerificationStatus();
        }
    }

    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConnectionViewModel.SelectedPort)
            or nameof(ConnectionViewModel.SelectedChip))
        {
            ClearVerificationStatus();
        }
    }

    public IReadOnlyList<string> FlashOptions() => new[]
    {
        "--flash-mode", FlashMode.Trim(),
        "--flash-freq", FlashFreq.Trim(),
        "--flash-size", FlashSize.Trim()
    };

    public bool TryGetFlashPairs(out IReadOnlyList<string> pairs)
    {
        pairs = System.Array.Empty<string>();
        var items = FlashItems.ToList();
        var invalid = items.Count == 0
                      || items.Any(item => string.IsNullOrWhiteSpace(item.Address)
                                           || string.IsNullOrWhiteSpace(item.FilePath));
        if (invalid)
        {
            Execution.AppendLog("Ошибка: добавьте файлы и укажите адрес каждому.");
            return false;
        }

        pairs = items
            .SelectMany(item => new[] { item.Address.Trim(), item.FilePath.Trim() })
            .ToArray();
        return true;
    }

    private FlashItem CreateFlashItem(string address) =>
        new(address, BrowseFlashItemAsync, RemoveFlashItem);

    private async Task BrowseFlashItemAsync(FlashItem item)
    {
        var path = await FilePickerHelper.PickOpenAsync("Выберите файл прошивки");
        if (path is null) return;

        item.FilePath = path;
        Execution.AppendLog($"Выбран файл: {path}");
    }

    private void RemoveFlashItem(FlashItem item) => FlashItems.Remove(item);
}
