
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.UI.ViewModels;

/// <summary>
/// Coordinates esptool execution and owns the shared progress/log state.
/// Feature-specific command construction stays in the corresponding page VM.
/// </summary>
public partial class EspToolExecutionViewModel : ViewModelBase
{
    private readonly IEspToolService _espToolService;
    private readonly ConnectionViewModel _connection;
    private CancellationTokenSource? _cts;

    public EspToolExecutionViewModel(IEspToolService espToolService, ConnectionViewModel connection)
    {
        _espToolService = espToolService;
        _connection = connection;
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _consoleOutput = "Готов к работе.\n";

    [RelayCommand]
    private void ClearLog() => ConsoleOutput = string.Empty;

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

    public async Task<EsptoolCommandResult> ExecuteAsync(
        string title,
        string command,
        bool needsPort = true,
        bool needsChip = false)
    {
        if (IsBusy)
        {
            const string message = "Другая операция уже выполняется.";
            AppendLog(message);
            return EsptoolCommandResult.Rejected(message);
        }

        if (needsPort && string.IsNullOrWhiteSpace(_connection.SelectedPort))
        {
            const string message = "Не выбран COM-порт.";
            AppendLog($"Ошибка: {message}");
            return EsptoolCommandResult.Rejected(message);
        }

        if (needsChip && _connection.SelectedChip == "auto")
        {
            const string message = "Для этой команды нужно выбрать конкретный чип.";
            AppendLog($"Ошибка: {message}");
            return EsptoolCommandResult.Rejected(message);
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();
        AppendLog(title);

        try
        {
            var arguments = $"{BuildGlobalArguments(needsPort)} {command}".Trim();
            return await _espToolService.ExecuteCommandAsync(arguments, AppendLog, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            const string message = "Операция отменена.";
            AppendLog(message);
            return EsptoolCommandResult.Cancelled(message);
        }
        catch (Exception ex)
        {
            AppendLog($"Ошибка: {ex.Message}");
            return EsptoolCommandResult.Failure(ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsBusy = false;
        }
    }

    public bool Require(params (string? Value, string Name)[] fields)
    {
        var missing = fields
            .Where(field => string.IsNullOrWhiteSpace(field.Value))
            .Select(field => field.Name)
            .ToList();

        if (missing.Count == 0)
        {
            return true;
        }

        AppendLog($"Ошибка: заполните поля: {string.Join(", ", missing)}");
        return false;
    }

    public string BuildGlobalArguments(bool online)
    {
        var parts = new List<string>();

        if (_connection.SelectedChip != "auto")
        {
            parts.Add($"--chip {_connection.SelectedChip}");
        }

        if (online)
        {
            parts.Add($"--port {_connection.SelectedPort}");
            parts.Add($"--baud {_connection.SelectedBaud}");
            parts.Add($"--before {_connection.SelectedBefore}");
            parts.Add($"--after {_connection.SelectedAfter}");
            parts.Add($"--connect-attempts {Math.Max(0, _connection.ConnectAttempts)}");

            if (_connection.NoStub) parts.Add("--no-stub");
            if (_connection.Trace) parts.Add("--trace");
        }

        return string.Join(" ", parts);
    }

    public void AppendLog(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} > {message}\n";

        if (Dispatcher.UIThread.CheckAccess())
        {
            ConsoleOutput += line;
        }
        else
        {
            Dispatcher.UIThread.Post(() => ConsoleOutput += line);
        }
    }
}
