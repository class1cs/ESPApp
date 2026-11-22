using ESPApp.Infrastructure.Models;
using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class EspToolExecutionViewModelTests
{
    [Fact(DisplayName = "Выполнение: без COM-порта операция отклоняется до вызова сервиса")]
    public async Task Execute_WithoutPort_ReturnsRejected()
    {
        using var host = TestApplicationHost.Create();

        var result = await host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });

        result.Status.Should().Be(EsptoolCommandStatus.Rejected);
        result.Message.Should().Contain("COM-порт");
        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Выполнение: порт из пробелов считается невыбранным")]
    public async Task Execute_WithWhitespacePort_ReturnsRejected()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "   ";

        var result = await host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });

        result.Status.Should().Be(EsptoolCommandStatus.Rejected);
        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Выполнение: команды, которым не нужен порт, могут запускаться без COM")]
    public async Task Execute_WithoutPort_WhenPortIsNotRequired_CallsService()
    {
        using var host = TestApplicationHost.Create();

        var result = await host.Execution.ExecuteAsync("Версия", new[] { "version" }, needsPort: false);

        result.Status.Should().Be(EsptoolCommandStatus.Succeeded);
        host.EspToolService.LastArguments.Should().Equal("version");
        host.EspToolService.CallCount.Should().Be(1);
    }

    [Fact(DisplayName = "Выполнение: для команд с требованием конкретного чипа auto отклоняется")]
    public async Task Execute_WithAutoChip_WhenConcreteChipIsRequired_ReturnsRejected()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        var result = await host.Execution.ExecuteAsync("Тест", new[] { "elf2image" }, needsPort: false, needsChip: true);

        result.Status.Should().Be(EsptoolCommandStatus.Rejected);
        result.Message.Should().Contain("конкретный чип");
        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Аргументы подключения: передаются порт, скорость, режимы сброса и число попыток")]
    public void BuildGlobalArguments_IncludesConnectionSettings()
    {
        using var host = TestApplicationHost.Create();
        var connection = host.Connection;
        connection.SelectedPort = "COM3";
        connection.SelectedBaud = "921600";
        connection.SelectedBefore = "usb-reset";
        connection.SelectedAfter = "no-reset";
        connection.ConnectAttempts = 3;

        var arguments = host.Execution.BuildGlobalArguments(online: true);

        arguments.Should().ContainInOrder(
            "--port", "COM3",
            "--baud", "921600",
            "--before", "usb-reset",
            "--after", "no-reset",
            "--connect-attempts", "3");
    }

    [Fact(DisplayName = "Аргументы подключения: отрицательное число попыток ограничивается нулём")]
    public void BuildGlobalArguments_ClampsNegativeAttemptsToZero()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.ConnectAttempts = -5;

        host.Execution.BuildGlobalArguments(online: true)
            .Should().ContainInOrder("--connect-attempts", "0");
    }

    [Theory(DisplayName = "Аргументы подключения: --chip добавляется только для конкретного чипа")]
    [InlineData("auto", false)]
    [InlineData("esp32s3", true)]
    public void BuildGlobalArguments_AddsChipOnlyWhenNotAuto(string chip, bool expected)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = chip;

        var arguments = host.Execution.BuildGlobalArguments(online: true);

        arguments.Contains("--chip").Should().Be(expected);
        if (expected)
        {
            arguments.Should().ContainInOrder("--chip", chip);
        }
    }

    [Fact(DisplayName = "Аргументы подключения: флаги no-stub и trace по умолчанию отсутствуют")]
    public void BuildGlobalArguments_OmitsDisabledFlags()
    {
        using var host = TestApplicationHost.Create();

        var arguments = host.Execution.BuildGlobalArguments(online: true);

        arguments.Should().NotContain("--no-stub").And.NotContain("--trace");
    }

    [Fact(DisplayName = "Аргументы подключения: включённые no-stub и trace добавляются")]
    public void BuildGlobalArguments_IncludesEnabledFlags()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.NoStub = true;
        host.Connection.Trace = true;

        var arguments = host.Execution.BuildGlobalArguments(online: true);

        arguments.Should().Contain("--no-stub").And.Contain("--trace");
    }

    [Fact(DisplayName = "Аргументы подключения: глобальные параметры располагаются до подкоманды")]
    public async Task Execute_PutsGlobalArgumentsBeforeSubcommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });

        var arguments = host.EspToolService.LastArguments!.ToList();
        var portIndex = arguments.IndexOf("--port");
        var commandIndex = arguments.IndexOf("chip-id");
        portIndex.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(commandIndex);
    }

    [Fact(DisplayName = "Выполнение: IsBusy включён на время работы сервиса и сбрасывается после завершения")]
    public async Task Execute_WhileServiceRuns_TracksBusyState()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var gate = new TaskCompletionSource<EsptoolCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.EspToolService.Handler = (_, _, _) => gate.Task;

        var running = host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });
        host.Execution.IsBusy.Should().BeTrue();

        gate.SetResult(EsptoolCommandResult.Success());
        await running;

        host.Execution.IsBusy.Should().BeFalse();
    }

    [Fact(DisplayName = "Выполнение: параллельная вторая команда отклоняется")]
    public async Task Execute_WhileBusy_RejectsSecondCommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var gate = new TaskCompletionSource<EsptoolCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.EspToolService.Handler = (_, _, _) => gate.Task;

        var first = host.Execution.ExecuteAsync("Первая", new[] { "chip-id" });
        var second = await host.Execution.ExecuteAsync("Вторая", new[] { "flash-id" });

        second.Status.Should().Be(EsptoolCommandStatus.Rejected);
        host.EspToolService.CallCount.Should().Be(1);

        gate.SetResult(EsptoolCommandResult.Success());
        await first;
    }

    [Fact(DisplayName = "Выполнение: исключение сервиса превращается в Failure и сбрасывает IsBusy")]
    public async Task Execute_WhenServiceThrows_ReturnsFailureAndResetsBusy()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.EspToolService.Handler = (_, _, _) =>
            Task.FromException<EsptoolCommandResult>(new InvalidOperationException("boom"));

        var result = await host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });

        result.Status.Should().Be(EsptoolCommandStatus.Failed);
        result.Message.Should().Contain("boom");
        host.Execution.IsBusy.Should().BeFalse();
    }

    [Fact(DisplayName = "Отмена: Cancel прерывает ожидающий сервис и сбрасывает IsBusy")]
    public async Task Cancel_WhileRunning_CancelsTokenAndResetsBusy()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.EspToolService.Handler = async (_, _, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return EsptoolCommandResult.Success();
        };

        var running = host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });
        await started.Task;
        host.Execution.CancelCommand.Execute(null);

        var result = await running;

        result.Status.Should().Be(EsptoolCommandStatus.Cancelled);
        host.EspToolService.LastCancellationToken.IsCancellationRequested.Should().BeTrue();
        host.Execution.IsBusy.Should().BeFalse();
    }

    [Fact(DisplayName = "Отмена: Cancel без выполняющейся операции ничего не ломает")]
    public void Cancel_WhenIdle_DoesNotThrow()
    {
        using var host = TestApplicationHost.Create();

        var act = () => host.Execution.CancelCommand.Execute(null);

        act.Should().NotThrow();
        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Проверка полей: Require возвращает false, если обязательное поле пустое")]
    public void Require_ReturnsFalseWhenRequiredValueIsMissing()
    {
        using var host = TestApplicationHost.Create();

        host.Execution.Require(("", "адрес"), ("0x1000", "размер")).Should().BeFalse();
    }

    [Fact(DisplayName = "Проверка полей: Require возвращает true, если все поля заполнены")]
    public void Require_ReturnsTrueWhenAllValuesArePresent()
    {
        using var host = TestApplicationHost.Create();

        host.Execution.Require(("0x1000", "адрес"), ("0x100", "размер")).Should().BeTrue();
    }

    [Fact(DisplayName = "Лог: команда очистки удаляет всё содержимое консоли")]
    public void ClearLog_ClearsConsoleOutput()
    {
        using var host = TestApplicationHost.Create();
        host.Execution.ConsoleOutput = "some output";

        host.Execution.ClearLogCommand.Execute(null);

        host.Execution.ConsoleOutput.Should().BeEmpty();
    }


    [Fact(DisplayName = "Аргументы подключения: значения с пробелами остаются отдельным аргументом")]
    public async Task Execute_PreservesPortAsSingleArgument()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM PORT 3";

        await host.Execution.ExecuteAsync("Тест", new[] { "chip-id" });

        host.EspToolService.LastArguments.Should().ContainInOrder("--port", "COM PORT 3", "chip-id");
    }
}
