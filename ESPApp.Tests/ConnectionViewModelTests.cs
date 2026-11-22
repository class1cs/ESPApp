using ESPApp.Infrastructure.Models;
using ESPApp.UI.ViewModels;
using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class ConnectionViewModelTests
{
    [Fact(DisplayName = "Подключение: создаётся с ожидаемыми настройками по умолчанию")]
    public void NewViewModel_HasExpectedDefaults()
    {
        var vm = new ConnectionViewModel();

        vm.SelectedPort.Should().BeNull();
        vm.SelectedChip.Should().Be("auto");
        vm.SelectedBaud.Should().Be("460800");
        vm.SelectedBefore.Should().Be("default-reset");
        vm.SelectedAfter.Should().Be("hard-reset");
        vm.ConnectAttempts.Should().Be(7);
        vm.NoStub.Should().BeFalse();
        vm.Trace.Should().BeFalse();
    }

    [Fact(DisplayName = "Подключение: ApplyDefaults применяет поддерживаемый чип и скорость")]
    public void ApplyDefaults_AppliesSupportedChipAndBaud()
    {
        var vm = new ConnectionViewModel();
        var settings = new AppSettings
        {
            DefaultChip = "esp32s3",
            DefaultBaud = "921600"
        };

        vm.ApplyDefaults(settings);

        vm.SelectedChip.Should().Be("esp32s3");
        vm.SelectedBaud.Should().Be("921600");
    }

    [Fact(DisplayName = "Подключение: ApplyDefaults игнорирует неподдерживаемые значения")]
    public void ApplyDefaults_IgnoresUnsupportedValues()
    {
        var vm = new ConnectionViewModel();
        var settings = new AppSettings
        {
            DefaultChip = "unknown-chip",
            DefaultBaud = "12345"
        };

        vm.ApplyDefaults(settings);

        vm.SelectedChip.Should().Be("auto");
        vm.SelectedBaud.Should().Be("460800");
    }

    [Fact(DisplayName = "Подключение: списки чипов, скоростей и режимов не пустые")]
    public void OptionLists_ContainSupportedValues()
    {
        var vm = new ConnectionViewModel();

        vm.Chips.Should().Contain("auto").And.Contain("esp32").And.Contain("esp32s3");
        vm.Bauds.Should().Contain("115200").And.Contain("460800").And.Contain("921600");
        vm.BeforeModes.Should().Contain("default-reset").And.Contain("no-reset");
        vm.AfterModes.Should().Contain("hard-reset").And.Contain("no-reset");
    }
}
