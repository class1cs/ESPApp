using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class ToolsPageViewModelTests
{
    [Fact(DisplayName = "Инструменты: чтение памяти передаёт указанный адрес")]
    public async Task ReadMem_SendsAddress()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.MemAddress = "0x3ff00000";

        await host.Tools.ReadMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith("read-mem 0x3ff00000");
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

        host.EspToolService.LastArguments.Should().EndWith("write-mem 0x3ff00000 0x1");
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

        host.EspToolService.LastArguments.Should().EndWith("write-mem 0x3ff00000 0x1 0xff");
    }

    [Fact(DisplayName = "Инструменты: дамп памяти передаёт адрес, размер и путь в кавычках")]
    public async Task DumpMem_SendsAddressSizeAndQuotedPath()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.DumpAddress = "0x40000000";
        host.Tools.DumpSize = "0x1000";
        host.Tools.DumpPath = "dump file.bin";

        await host.Tools.DumpMemCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should()
            .EndWith("dump-mem 0x40000000 0x1000 \"dump file.bin\"");
    }

    [Fact(DisplayName = "Инструменты: загрузка в RAM передаёт путь к файлу в кавычках")]
    public async Task LoadRam_SendsQuotedPath()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";
        host.Tools.LoadRamPath = "C:/my fw/app.bin";

        await host.Tools.LoadRamCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith("load-ram \"C:/my fw/app.bin\"");
    }

    [Fact(DisplayName = "Инструменты: run вызывает одноимённую подкоманду")]
    public async Task RunCode_SendsRunCommand()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedPort = "COM3";

        await host.Tools.RunCodeCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith(" run");
    }

    [Fact(DisplayName = "Инструменты: image-info работает без COM-порта")]
    public async Task ImageInfo_RunsOffline()
    {
        using var host = TestApplicationHost.Create();
        host.Tools.ImageInfoPath = "firmware image.bin";

        await host.Tools.ImageInfoCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Be("image-info \"firmware image.bin\"");
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

        host.EspToolService.LastArguments.Should().Contain("--chip esp32")
            .And.Contain("elf2image")
            .And.NotContain("--port");
    }

    [Fact(DisplayName = "Инструменты: ELF2image передаёт выходной файл перед входным")]
    public async Task Elf2Image_PassesOutputThenInputInQuotes()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.ElfPath = "app.elf";
        host.Tools.ElfOutputPath = "app.bin";

        await host.Tools.Elf2ImageCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().EndWith("-o \"app.bin\" \"app.elf\"");
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
        host.Tools.MergeOutputPath = "merged.bin";
        host.Firmware.FlashItems[0].Address = "0x0";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Contain("merge-bin -o \"merged.bin\"")
            .And.Contain("0x0 \"boot.bin\"");
    }

    [Fact(DisplayName = "Инструменты: merge-bin передаёт формат и параметры Flash")]
    public async Task MergeBin_PassesFormatAndFlashOptions()
    {
        using var host = TestApplicationHost.Create();
        host.Connection.SelectedChip = "esp32";
        host.Tools.MergeOutputPath = "merged.bin";
        host.Tools.SelectedMergeFormat = "uf2";
        host.Firmware.FlashMode = "dio";
        host.Firmware.FlashItems[0].FilePath = "boot.bin";

        await host.Tools.MergeBinCommand.ExecuteAsync(null);

        host.EspToolService.LastArguments.Should().Contain("--format uf2")
            .And.Contain("--flash-mode dio");
    }
}
