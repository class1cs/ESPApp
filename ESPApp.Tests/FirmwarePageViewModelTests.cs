using ESPApp.Infrastructure.Models;
using FluentAssertions;
using FluentAssertions.Execution;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class FirmwarePageViewModelTests
{
    [Fact(DisplayName = "Прошивка: создаётся с одной строкой по адресу 0x1000 и параметрами по умолчанию")]
    public void NewViewModel_HasExpectedDefaults()
    {
        using var host = TestApplicationHost.Create();
        var vm = host.Firmware;

        using (new AssertionScope())
        {
            vm.FlashItems.Should().ContainSingle().Which.Address.Should().Be("0x1000");
            vm.FlashMode.Should().Be("keep");
            vm.FlashFreq.Should().Be("keep");
            vm.FlashSize.Should().Be("keep");
            vm.EraseAll.Should().BeFalse();
            vm.NoCompress.Should().BeFalse();
            vm.Force.Should().BeFalse();
            vm.VerifyDiff.Should().BeFalse();
        }
    }

    [Fact(DisplayName = "Прошивка: команда write-flash содержит COM-порт, адрес и путь в кавычках")]
    public async Task Flash_WithFile_BuildsWriteFlashCommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems[0].Address = "0x1000";
        host.Firmware.FlashItems[0].FilePath = "C:/my fw/boot.bin";

        await host.Firmware.FlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Contain("--port COM3")
            .And.Contain("write-flash")
            .And.Contain("0x1000 \"C:/my fw/boot.bin\"");
    }

    [Fact(DisplayName = "Прошивка: без выбранного файла команда не запускается")]
    public async Task Flash_WithoutFile_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Firmware.FlashCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Theory(DisplayName = "Прошивка: пустой адрес или путь блокирует запись")]
    [InlineData("", "boot.bin")]
    [InlineData("0x1000", "")]
    [InlineData("   ", "boot.bin")]
    public async Task Flash_WithEmptyAddressOrPath_DoesNotCallService(string address, string path)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems[0].Address = address;
        host.Firmware.FlashItems[0].FilePath = path;

        await host.Firmware.FlashCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Прошивка: пустой список BIN-файлов блокирует запись")]
    public async Task Flash_WithEmptyList_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems.Clear();

        await host.Firmware.FlashCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Прошивка: режим, частота и размер Flash попадают в команду")]
    public async Task Flash_PassesFlashOptions()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var vm = host.Firmware;
        vm.FlashItems[0].FilePath = "boot.bin";
        vm.FlashMode = "dio";
        vm.FlashFreq = "80m";
        vm.FlashSize = "4MB";

        await vm.FlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should()
            .Contain("--flash-mode dio --flash-freq 80m --flash-size 4MB");
    }

    [Fact(DisplayName = "Прошивка: включённые флаги добавляются в write-flash")]
    public async Task Flash_WithOptionsEnabled_AddsFlags()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var vm = host.Firmware;
        vm.FlashItems[0].FilePath = "boot.bin";
        vm.EraseAll = true;
        vm.NoCompress = true;
        vm.Force = true;

        await vm.FlashCommand.ExecuteAsync(null);

        using (new AssertionScope())
        {
            host.EspToolService.LastArguments.Should().Contain("--erase-all");
            host.EspToolService.LastArguments.Should().Contain("--no-compress");
            host.EspToolService.LastArguments.Should().Contain("--force");
        }
    }

    [Fact(DisplayName = "Прошивка: выключенные флаги не добавляются в write-flash")]
    public async Task Flash_WithOptionsDisabled_OmitsFlags()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Firmware.FlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().NotContain("--erase-all")
            .And.NotContain("--no-compress")
            .And.NotContain("--force");
    }

    [Fact(DisplayName = "Прошивка: несколько файлов передаются парами адрес/путь в исходном порядке")]
    public async Task Flash_WithSeveralFiles_PreservesPairOrder()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var vm = host.Firmware;
        vm.FlashItems[0].Address = " 0x1000 ";
        vm.FlashItems[0].FilePath = "boot.bin";
        vm.AddFlashItemCommand.Execute(null);
        vm.FlashItems[1].Address = "0x8000";
        vm.FlashItems[1].FilePath = "table.bin";

        await vm.FlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should()
            .EndWith("0x1000 \"boot.bin\" 0x8000 \"table.bin\"");
    }

    [Fact(DisplayName = "Список прошивки: команда добавляет и удаляет строку")]
    public void FlashItems_AddAndRemove_ChangesCollection()
    {
        using var host = TestApplicationHost.Create();
        var vm = host.Firmware;

        vm.AddFlashItemCommand.Execute(null);
        vm.FlashItems.Should().HaveCount(2);
        vm.FlashItems[1].RemoveCommand.Execute(null);

        vm.FlashItems.Should().ContainSingle();
    }

    [Theory(DisplayName = "Проверка прошивки: --diff зависит от соответствующего параметра")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyFlash_DiffFlagDependsOnSetting(bool withDiff)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";
        host.Firmware.VerifyDiff = withDiff;

        await host.Firmware.VerifyFlashCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Contain("verify-flash");
        host.EspToolService.LastArguments!.Contains("--diff").Should().Be(withDiff);
    }

    [Fact(DisplayName = "Проверка прошивки: успешный результат отображается в статусе")]
    public async Task VerifyFlash_OnSuccess_SetsSuccessStatus()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Firmware.FlashItems[0].FilePath = "firmware.bin";

        await host.Firmware.VerifyFlashCommand.ExecuteAsync(null);

        host.Firmware.IsVerificationSuccessful.Should().BeTrue();
        host.Firmware.IsVerificationRunning.Should().BeFalse();
        host.Firmware.VerificationStatusText.Should().Be("Проверка пройдена");
    }

    [Fact(DisplayName = "Проверка прошивки: ошибка esptool отображается в статусе")]
    public async Task VerifyFlash_OnFailure_SetsFailureStatus()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.EspToolService.Handler = (_, _, _) =>
            Task.FromResult(EsptoolCommandResult.Failure("verify mismatch", 2));
        host.Firmware.FlashItems[0].FilePath = "firmware.bin";

        await host.Firmware.VerifyFlashCommand.ExecuteAsync(null);

        host.Firmware.IsVerificationFailed.Should().BeTrue();
        host.Firmware.VerificationStatusText.Should().Be("Проверка не пройдена (код: 2)");
    }

    [Fact(DisplayName = "Проверка прошивки: отсутствие файла показывает ошибку и не запускает esptool")]
    public async Task VerifyFlash_WithoutFile_SetsValidationError()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Firmware.VerifyFlashCommand.ExecuteAsync(null);

        host.Firmware.IsVerificationFailed.Should().BeTrue();
        host.Firmware.VerificationStatusText.Should().Be("Укажите файл прошивки и адрес.");
        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Проверка прошивки: изменение файла сбрасывает предыдущий результат")]
    public async Task VerifyFlash_ChangingFile_ClearsPreviousResult()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        var vm = host.Firmware;
        vm.FlashItems[0].FilePath = "firmware.bin";
        await vm.VerifyFlashCommand.ExecuteAsync(null);
        vm.IsVerificationSuccessful.Should().BeTrue();

        vm.FlashItems[0].FilePath = "other.bin";

        vm.IsVerificationSuccessful.Should().BeFalse();
        vm.IsVerificationFailed.Should().BeFalse();
        vm.VerificationStatusText.Should().BeEmpty();
    }
}
