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
        IReadOnlyList<string> commandArguments,
        bool needsPort = true,
        bool needsChip = false)
    {
        ArgumentNullException.ThrowIfNull(commandArguments);

        if (commandArguments.Count == 0 || string.IsNullOrWhiteSpace(commandArguments[0]))
        {
            const string message = "Не указана команда esptool.";
            AppendLog($"Ошибка: {message}");
            return EsptoolCommandResult.Rejected(message);
        }

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
            var arguments = BuildGlobalArguments(needsPort).ToList();
            arguments.AddRange(commandArguments);
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

    public IReadOnlyList<string> BuildGlobalArguments(bool online)
    {
        var arguments = new List<string>();

        if (_connection.SelectedChip != "auto")
        {
            arguments.Add("--chip");
            arguments.Add(_connection.SelectedChip);
        }

        if (online)
        {
            arguments.Add("--port");
            arguments.Add(_connection.SelectedPort ?? string.Empty);
            arguments.Add("--baud");
            arguments.Add(_connection.SelectedBaud);
            arguments.Add("--before");
            arguments.Add(_connection.SelectedBefore);
            arguments.Add("--after");
            arguments.Add(_connection.SelectedAfter);
            arguments.Add("--connect-attempts");
            arguments.Add(Math.Max(0, _connection.ConnectAttempts).ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (_connection.NoStub) arguments.Add("--no-stub");
            if (_connection.Trace) arguments.Add("--trace");
        }

        return arguments;
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
