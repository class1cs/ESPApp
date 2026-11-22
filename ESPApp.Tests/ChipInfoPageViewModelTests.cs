using CommunityToolkit.Mvvm.Input;
using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class ChipInfoPageViewModelTests
{
    [Theory(DisplayName = "Информация о чипе: команда вызывает соответствующую подкоманду esptool")]
    [InlineData("chip-id")]
    [InlineData("flash-id")]
    [InlineData("read-mac")]
    [InlineData("get-security-info")]
    public async Task ReadCommands_SendExpectedSubcommand(string subcommand)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var vm = host.ChipInfo;
        IAsyncRelayCommand command = subcommand switch
        {
            "chip-id" => vm.ChipIdCommand,
            "flash-id" => vm.FlashIdCommand,
            "read-mac" => vm.ReadMacCommand,
            "get-security-info" => vm.SecurityInfoCommand,
            _ => throw new ArgumentOutOfRangeException(nameof(subcommand))
        };

        await command.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith(subcommand);
    }

    [Fact(DisplayName = "Информация о чипе: версия esptool читается без COM-порта")]
    public async Task Version_DoesNotRequirePort()
    {
        using var host = TestApplicationHost.Create();

        await host.ChipInfo.VersionCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Equal("version");
    }

    [Fact(DisplayName = "Информация о чипе: SFDP использует адрес и длину по умолчанию")]
    public async Task ReadSfdp_UsesDefaultAddressAndLength()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.ChipInfo.ReadSfdpCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(3).Should().Equal("read-flash-sfdp", "0x0", "4");
    }

    [Fact(DisplayName = "Информация о чипе: пустые параметры SFDP блокируют команду")]
    public async Task ReadSfdp_WithMissingParameters_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.ChipInfo.SfdpLength = " ";

        await host.ChipInfo.ReadSfdpCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }
}
