using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class ToolsPageViewModelTests
{
    [Fact(DisplayName = "Инструменты: чтение памяти передаёт указанный адрес отдельным аргументом")]
    public async Task ReadMem_SendsAddress()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.MemAddress = "0x3ff00000";

        await host.Tools.ReadMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(2).Should().Equal("read-mem", "0x3ff00000");
    }

    [Fact(DisplayName = "Инструменты: запись в память без значения не запускается")]
    public async Task WriteMem_WithoutValue_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.MemAddress = "0x3ff00000";

        await host.Tools.WriteMemCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Инструменты: запись в память без маски не добавляет аргумент маски")]
    public async Task WriteMem_WithoutMask_OmitsMask()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.MemAddress = "0x3ff00000";
        host.Tools.MemValue = "0x1";

        await host.Tools.WriteMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(3).Should().Equal("write-mem", "0x3ff00000", "0x1");
    }

    [Fact(DisplayName = "Инструменты: запись в память добавляет маску последним аргументом")]
    public async Task WriteMem_WithMask_AppendsMask()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.MemAddress = "0x3ff00000";
        host.Tools.MemValue = "0x1";
        host.Tools.MemMask = "0xff";

        await host.Tools.WriteMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(4).Should().Equal("write-mem", "0x3ff00000", "0x1", "0xff");
    }

    [Fact(DisplayName = "Инструменты: дамп памяти передаёт путь с пробелами одним аргументом")]
    public async Task DumpMem_PreservesOutputPathAsSingleArgument()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.DumpAddress = "0x40000000";
        host.Tools.DumpSize = "0x1000";
        host.Tools.DumpPath = "C:/firmware backups/dump image.bin";

        await host.Tools.DumpMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(4).Should().Equal(
            "dump-mem", "0x40000000", "0x1000", "C:/firmware backups/dump image.bin");
    }

    [Fact(DisplayName = "Инструменты: загрузка в RAM передаёт путь с пробелами одним аргументом")]
    public async Task LoadRam_PreservesPathAsSingleArgument()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.LoadRamPath = "C:/my firmware/app image.bin";

        await host.Tools.LoadRamCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(2).Should().Equal(
            "load-ram", "C:/my firmware/app image.bin");
    }

    [Fact(DisplayName = "Инструменты: run вызывает одноимённую подкоманду")]
    public async Task RunCode_SendsRunCommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Tools.RunCodeCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith("run");
    }

    [Fact(DisplayName = "Инструменты: image-info работает без COM-порта")]
    public async Task ImageInfo_RunsOffline()
    {
        using var host = TestApplicationHost.Create();
        host.Tools.ImageInfoPath = "firmware image.bin";

        await host.Tools.ImageInfoCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Equal("image-info", "firmware image.bin");
        host.EspToolService.LastArguments.Should().NotContain("--port");
    }

    [Fact(DisplayName = "Инструменты: ELF2image отклоняется, пока чип выбран как auto")]
    public async Task Elf2Image_WithAutoChip_IsRejected()
    {
        using var host = TestApplicationHost.Create();
        host.Tools.ElfPath = "app.elf";
        host.Tools.ElfOutputPath = "app.bin";

        await host.Tools.Elf2ImageCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Theory(DisplayName = "Инструменты: ELF2image требует входной и выходной файлы")]
    [InlineData("", "app.bin")]
    [InlineData("app.elf", "")]
    public async Task Elf2Image_WithMissingFile_DoesNotCallService(string elfPath, string outputPath)
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.ElfPath = elfPath;
        host.Tools.ElfOutputPath = outputPath;

        await host.Tools.Elf2ImageCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Инструменты: ELF2image с выбранным чипом работает без COM-порта")]
    public async Task Elf2Image_WithConcreteChip_RunsOffline()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.ElfPath = "app.elf";
        host.Tools.ElfOutputPath = "app.bin";

        await host.Tools.Elf2ImageCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().ContainInOrder("--chip", "esp32", "elf2image");
        host.EspToolService.LastArguments.Should().NotContain("--port");
    }

    [Fact(DisplayName = "Инструменты: ELF2image передаёт выходной файл перед входным")]
    public async Task Elf2Image_PassesOutputThenInput()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.ElfPath = "app.elf";
        host.Tools.ElfOutputPath = "generated output/app image.bin";

        await host.Tools.Elf2ImageCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments!.TakeLast(3).Should().Equal(
            "-o", "generated output/app image.bin", "app.elf");
    }

    [Fact(DisplayName = "Инструменты: merge-bin требует конкретный чип")]
    public async Task MergeBin_WithAutoChip_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Tools.MergeOutputPath = "merged.bin";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Инструменты: merge-bin требует выходной файл")]
    public async Task MergeBin_WithoutOutputPath_DoesNotCallService()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.CallCount.Should().Be(0);
    }

    [Fact(DisplayName = "Инструменты: merge-bin использует список файлов со страницы прошивки")]
    public async Task MergeBin_UsesFirmwareItemsAndOutputPath()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.MergeOutputPath = "merged result.bin";
        host.Firmware.FlashItems[0].Address = "0x0";
        host.Firmware.FlashItems[0].FilePath = "boot image.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().ContainInOrder(
            "merge-bin", "-o", "merged result.bin", "--format", "raw");
        host.EspToolService.LastArguments.Should().ContainInOrder("0x0", "boot image.bin");
    }

    [Fact(DisplayName = "Инструменты: merge-bin передаёт формат и параметры Flash отдельными аргументами")]
    public async Task MergeBin_PassesFormatAndFlashOptions()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.MergeOutputPath = "merged.bin";
        host.Tools.SelectedMergeFormat = "uf2";
        host.Firmware.FlashMode = "dio";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().ContainInOrder("--format", "uf2");
        host.EspToolService.LastArguments.Should().ContainInOrder("--flash-mode", "dio");
    }
}
