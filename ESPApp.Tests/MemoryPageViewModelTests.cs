using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class MemoryPageViewModelTests
{
    [Fact(DisplayName = "Память: команда полного стирания вызывает erase-flash")]
    public async Task EraseFlash_SendsExpectedCommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Memory.EraseFlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith("erase-flash");
    }

    [Fact(DisplayName = "Стирание региона: пробелы по краям адреса и размера удаляются")]
    public async Task EraseRegion_TrimsWhitespace()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.EraseRegionAddress = " 0x10000 ";
        host.Memory.EraseRegionSize = " 0x1000 ";

        await host.Memory.EraseRegionCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(3).Should().Equal("erase-region", "0x10000", "0x1000");
    }

    [Theory(DisplayName = "Стирание региона: отсутствие адреса или размера блокирует команду")]
    [InlineData("0x10000", "")]
    [InlineData("", "0x1000")]
    public async Task EraseRegion_WithMissingField_DoesNotCallService(string address, string size)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.EraseRegionAddress = address;
        host.Memory.EraseRegionSize = size;

        await host.Memory.EraseRegionCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Чтение Flash: путь с пробелами передаётся одним аргументом без ручных кавычек")]
    public async Task ReadFlash_PreservesPathAsSingleArgument()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.ReadPath = "C:/backup files/flash backup.bin";

        await host.Memory.ReadFlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(4).Should().Equal(
            "read-flash", "0x0", "ALL", "C:/backup files/flash backup.bin");
    }

    [Fact(DisplayName = "Чтение регистра статуса: передаётся выбранное количество байт")]
    public async Task ReadFlashStatus_PassesSelectedByteCount()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.SelectedStatusBytes = "3";

        await host.Memory.ReadFlashStatusCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(3).Should().Equal("read-flash-status", "--bytes", "3");
    }

    [Fact(DisplayName = "Запись регистра статуса: non-volatile добавляется при включённой настройке")]
    public async Task WriteFlashStatus_NonVolatile_AddsFlag()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.StatusValue = "0x40";
        host.Memory.StatusNonVolatile = true;

        await host.Memory.WriteFlashStatusCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(5).Should().Equal(
            "write-flash-status", "--non-volatile", "--bytes", "2", "0x40");
    }

    [Fact(DisplayName = "Запись регистра статуса: non-volatile не добавляется при выключенной настройке")]
    public async Task WriteFlashStatus_Volatile_OmitsFlag()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Memory.StatusValue = "0x40";

        await host.Memory.WriteFlashStatusCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(4).Should()
            .Equal("write-flash-status", "--bytes", "2", "0x40");
        host.EspToolService.LastArguments.Should().NotContain("--non-volatile");
    }

    [Fact(DisplayName = "Запись регистра статуса: без значения команда не запускается")]
    public async Task WriteFlashStatus_WithoutValue_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Memory.WriteFlashStatusCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }
}
