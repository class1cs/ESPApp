using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;
using ESPApp.UI.ViewModels;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;

namespace ESPApp.Tests
{
    [Trait("Category", "Unit")]
    public class MainViewModelTests
    {
        private readonly IEspToolService _service;
        private string? _args;
        private int _calls;

        public MainViewModelTests()
        {
            _args = null;
            _calls = 0;

            _service = Substitute.For<IEspToolService>();

            _service
                .ExecuteCommandAsync(
                    Arg.Do<string>(a =>
                    {
                        _args = a;
                        _calls++;
                    }),
                    Arg.Any<Action<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(EsptoolCommandResult.Success()));
        }

        private TestHarness CreateViewModel()
        {
            var main = new MainViewModel(_service);
            main.Connection.SelectedPort = "COM3";
            return new TestHarness(main);
        }

        private static IAsyncRelayCommand GetCommand(TestHarness vm, string commandName)
        {
            return (IAsyncRelayCommand)vm.GetType().GetProperty(commandName)!.GetValue(vm)!;
        }

        private sealed class TestHarness
        {
            public TestHarness(MainViewModel main) => Main = main;

            public MainViewModel Main { get; }
            public ConnectionViewModel Connection => Main.Connection;
            public EspToolExecutionViewModel Execution => Main.Execution;
            public IReadOnlyList<PageNavigationItemViewModel> NavigationPages => Main.NavigationPages;
            public PageViewModelBase CurrentPage => Main.CurrentPage;

            private FirmwarePageViewModel Firmware => Main.Firmware;
            private MemoryPageViewModel Memory => Main.Memory;
            private ChipInfoPageViewModel Chip => Main.ChipInfo;
            private ToolsPageViewModel Tools => Main.Tools;

            public string? SelectedPort { get => Connection.SelectedPort; set => Connection.SelectedPort = value; }
            public string SelectedChip { get => Connection.SelectedChip; set => Connection.SelectedChip = value; }
            public string SelectedBaud { get => Connection.SelectedBaud; set => Connection.SelectedBaud = value; }
            public string SelectedBefore { get => Connection.SelectedBefore; set => Connection.SelectedBefore = value; }
            public string SelectedAfter { get => Connection.SelectedAfter; set => Connection.SelectedAfter = value; }
            public int ConnectAttempts { get => Connection.ConnectAttempts; set => Connection.ConnectAttempts = value; }
            public bool NoStub { get => Connection.NoStub; set => Connection.NoStub = value; }
            public bool Trace { get => Connection.Trace; set => Connection.Trace = value; }

            public bool IsBusy => Execution.IsBusy;
            public string ConsoleOutput { get => Execution.ConsoleOutput; set => Execution.ConsoleOutput = value; }
            public IRelayCommand CancelCommand => Execution.CancelCommand;
            public IRelayCommand ClearLogCommand => Execution.ClearLogCommand;

            public ObservableCollection<FlashItem> FlashItems => Firmware.FlashItems;
            public string FlashMode { get => Firmware.FlashMode; set => Firmware.FlashMode = value; }
            public string FlashFreq { get => Firmware.FlashFreq; set => Firmware.FlashFreq = value; }
            public string FlashSize { get => Firmware.FlashSize; set => Firmware.FlashSize = value; }
            public bool EraseAll { get => Firmware.EraseAll; set => Firmware.EraseAll = value; }
            public bool NoCompress { get => Firmware.NoCompress; set => Firmware.NoCompress = value; }
            public bool Force { get => Firmware.Force; set => Firmware.Force = value; }
            public bool VerifyDiff { get => Firmware.VerifyDiff; set => Firmware.VerifyDiff = value; }
            public IRelayCommand AddFlashItemCommand => Firmware.AddFlashItemCommand;
            public IAsyncRelayCommand FlashCommand => Firmware.FlashCommand;
            public IAsyncRelayCommand VerifyFlashCommand => Firmware.VerifyFlashCommand;

            public string EraseRegionAddress { get => Memory.EraseRegionAddress; set => Memory.EraseRegionAddress = value; }
            public string EraseRegionSize { get => Memory.EraseRegionSize; set => Memory.EraseRegionSize = value; }
            public string ReadAddress { get => Memory.ReadAddress; set => Memory.ReadAddress = value; }
            public string ReadSize { get => Memory.ReadSize; set => Memory.ReadSize = value; }
            public string ReadPath { get => Memory.ReadPath; set => Memory.ReadPath = value; }
            public string SelectedStatusBytes { get => Memory.SelectedStatusBytes; set => Memory.SelectedStatusBytes = value; }
            public string StatusValue { get => Memory.StatusValue; set => Memory.StatusValue = value; }
            public bool StatusNonVolatile { get => Memory.StatusNonVolatile; set => Memory.StatusNonVolatile = value; }
            public IAsyncRelayCommand EraseFlashCommand => Memory.EraseFlashCommand;
            public IAsyncRelayCommand EraseRegionCommand => Memory.EraseRegionCommand;
            public IAsyncRelayCommand ReadFlashCommand => Memory.ReadFlashCommand;
            public IAsyncRelayCommand ReadFlashStatusCommand => Memory.ReadFlashStatusCommand;
            public IAsyncRelayCommand WriteFlashStatusCommand => Memory.WriteFlashStatusCommand;

            public string SfdpAddress { get => Chip.SfdpAddress; set => Chip.SfdpAddress = value; }
            public string SfdpLength { get => Chip.SfdpLength; set => Chip.SfdpLength = value; }
            public IAsyncRelayCommand ChipIdCommand => Chip.ChipIdCommand;
            public IAsyncRelayCommand FlashIdCommand => Chip.FlashIdCommand;
            public IAsyncRelayCommand ReadMacCommand => Chip.ReadMacCommand;
            public IAsyncRelayCommand SecurityInfoCommand => Chip.SecurityInfoCommand;
            public IAsyncRelayCommand VersionCommand => Chip.VersionCommand;
            public IAsyncRelayCommand ReadSfdpCommand => Chip.ReadSfdpCommand;

            public string MemAddress { get => Tools.MemAddress; set => Tools.MemAddress = value; }
            public string MemValue { get => Tools.MemValue; set => Tools.MemValue = value; }
            public string MemMask { get => Tools.MemMask; set => Tools.MemMask = value; }
            public string DumpAddress { get => Tools.DumpAddress; set => Tools.DumpAddress = value; }
            public string DumpSize { get => Tools.DumpSize; set => Tools.DumpSize = value; }
            public string DumpPath { get => Tools.DumpPath; set => Tools.DumpPath = value; }
            public string LoadRamPath { get => Tools.LoadRamPath; set => Tools.LoadRamPath = value; }
            public string ImageInfoPath { get => Tools.ImageInfoPath; set => Tools.ImageInfoPath = value; }
            public string ElfPath { get => Tools.ElfPath; set => Tools.ElfPath = value; }
            public string ElfOutputPath { get => Tools.ElfOutputPath; set => Tools.ElfOutputPath = value; }
            public string MergeOutputPath { get => Tools.MergeOutputPath; set => Tools.MergeOutputPath = value; }
            public string SelectedMergeFormat { get => Tools.SelectedMergeFormat; set => Tools.SelectedMergeFormat = value; }
            public IAsyncRelayCommand ReadMemCommand => Tools.ReadMemCommand;
            public IAsyncRelayCommand WriteMemCommand => Tools.WriteMemCommand;
            public IAsyncRelayCommand DumpMemCommand => Tools.DumpMemCommand;
            public IAsyncRelayCommand LoadRamCommand => Tools.LoadRamCommand;
            public IAsyncRelayCommand RunCodeCommand => Tools.RunCodeCommand;
            public IAsyncRelayCommand ImageInfoCommand => Tools.ImageInfoCommand;
            public IAsyncRelayCommand Elf2ImageCommand => Tools.Elf2ImageCommand;
            public IAsyncRelayCommand MergeBinCommand => Tools.MergeBinCommand;
        }

        [Fact(DisplayName = "Начальные значения: чип auto, скорость 460800, один файл по адресу 0x1000")]
        public void NewViewModel_HasExpectedDefaults()
        {
            // Arrange & Act
            var vm = CreateViewModel();

            // Assert
            using (new AssertionScope())
            {
                vm.SelectedChip.Should().Be("auto");
                vm.SelectedBaud.Should().Be("460800");
                vm.SelectedBefore.Should().Be("default-reset");
                vm.SelectedAfter.Should().Be("hard-reset");
                vm.IsBusy.Should().BeFalse();
                vm.FlashItems.Should().ContainSingle().Which.Address.Should().Be("0x1000");
            }
        }


        [Fact(DisplayName = "Навигация: по умолчанию открыта страница прошивки")]
        public void NewViewModel_StartsOnFirmwarePage()
        {
            var vm = CreateViewModel();

            vm.CurrentPage.Should().BeOfType<FirmwarePageViewModel>();
            vm.NavigationPages[0].IsSelected.Should().BeTrue();
        }

        [Fact(DisplayName = "Навигация: переход переключает выбранную страницу")]
        public void NavigateCommand_ChangesCurrentPage()
        {
            var vm = CreateViewModel();
            var memoryItem = vm.NavigationPages.Single(item => item.Page is MemoryPageViewModel);

            memoryItem.NavigateCommand.Execute(null);

            vm.CurrentPage.Should().BeSameAs(memoryItem.Page);
            memoryItem.IsSelected.Should().BeTrue();
            vm.NavigationPages
                .Where(item => !ReferenceEquals(item, memoryItem))
                .Should()
                .OnlyContain(item => !item.IsSelected);
        }

        [Fact(DisplayName = "Подключение: без порта команды для платы не запускаются")]
        public async Task ChipId_WithoutPort_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedPort = null;

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Подключение: порт из одних пробелов считается невыбранным")]
        public async Task ChipId_WithBlankPort_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedPort = "   ";

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Подключение: порт, скорость, режимы сброса и число попыток попадают в команду")]
        public async Task GlobalArguments_PassPortBaudResetModesAndAttempts()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedBaud = "921600";
            vm.SelectedBefore = "usb-reset";
            vm.SelectedAfter = "no-reset";
            vm.ConnectAttempts = 3;

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            using (new AssertionScope())
            {
                _args.Should().Contain("--port COM3");
                _args.Should().Contain("--baud 921600");
                _args.Should().Contain("--before usb-reset");
                _args.Should().Contain("--after no-reset");
                _args.Should().Contain("--connect-attempts 3");
            }
        }

        [Fact(DisplayName = "Подключение: отрицательное число попыток заменяется нулём")]
        public async Task GlobalArguments_WithNegativeAttempts_UseZero()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.ConnectAttempts = -5;

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("--connect-attempts 0");
        }

        [Theory(DisplayName = "Подключение: --chip передаётся, только если выбран не auto")]
        [InlineData("auto", false)]
        [InlineData("esp32s3", true)]
        public async Task GlobalArguments_Chip_PassedOnlyWhenNotAuto(string chip, bool expectedInCommand)
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = chip;

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _args!.Contains($"--chip {chip}").Should().Be(expectedInCommand);
        }

        [Fact(DisplayName = "Подключение: общие параметры стоят до подкоманды")]
        public async Task GlobalArguments_AreBeforeSubcommand()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            var portIndex = _args!.IndexOf("--port", StringComparison.Ordinal);
            var subcommandIndex = _args.IndexOf("chip-id", StringComparison.Ordinal);
            portIndex.Should().BeGreaterThanOrEqualTo(0);
            portIndex.Should().BeLessThan(subcommandIndex);
        }

        [Fact(DisplayName = "Подключение: без галочек --no-stub и --trace в команду не попадают")]
        public async Task GlobalArguments_WithFlagsOff_OmitNoStubAndTrace()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _args.Should().NotContain("--no-stub");
            _args.Should().NotContain("--trace");
        }

        [Fact(DisplayName = "Подключение: включённые галочки --no-stub и --trace попадают в команду")]
        public async Task GlobalArguments_WithFlagsOn_IncludeNoStubAndTrace()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.NoStub = true;
            vm.Trace = true;

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("--no-stub");
            _args.Should().Contain("--trace");
        }

        [Theory(DisplayName = "Простые команды: уходит нужная подкоманда esptool")]
        [InlineData("ChipIdCommand", "chip-id")]
        [InlineData("FlashIdCommand", "flash-id")]
        [InlineData("ReadMacCommand", "read-mac")]
        [InlineData("SecurityInfoCommand", "get-security-info")]
        [InlineData("EraseFlashCommand", "erase-flash")]
        [InlineData("RunCodeCommand", "run")]
        public async Task SimpleCommands_SendExpectedSubcommand(string commandName, string subcommand)
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await GetCommand(vm, commandName).ExecuteAsync(null);

            // Assert
            _args.Should().EndWith(subcommand);
        }

        [Fact(DisplayName = "Версия esptool: запускается без порта")]
        public async Task Version_DoesNotPassPort()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await vm.VersionCommand.ExecuteAsync(null);

            // Assert
            _args.Should().NotContain("--port");
            _args.Should().EndWith("version");
        }

        [Theory(DisplayName = "Обязательные поля: при пустых полях команда не запускается")]
        [InlineData("EraseRegionCommand")]
        [InlineData("ReadFlashCommand")]
        [InlineData("WriteFlashStatusCommand")]
        [InlineData("ReadMemCommand")]
        [InlineData("WriteMemCommand")]
        [InlineData("DumpMemCommand")]
        [InlineData("LoadRamCommand")]
        [InlineData("ImageInfoCommand")]
        public async Task CommandsWithRequiredFields_WhenFieldsEmpty_DoNotCallService(string commandName)
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await GetCommand(vm, commandName).ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Прошивка: собирает write-flash с портом, адресом и путём в кавычках")]
        public async Task Flash_WithFile_BuildsWriteFlashCommand()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].Address = "0x1000";
            vm.FlashItems[0].FilePath = "C:/my fw/boot.bin";

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("--port COM3");
            _args.Should().Contain("write-flash");
            _args.Should().Contain("0x1000 \"C:/my fw/boot.bin\"");
        }

        [Fact(DisplayName = "Прошивка: без выбранного файла команда не запускается")]
        public async Task Flash_WithoutFile_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Theory(DisplayName = "Прошивка: пустой адрес или путь — команда не запускается")]
        [InlineData("", "boot.bin")]
        [InlineData("0x1000", "")]
        [InlineData("   ", "boot.bin")]
        public async Task Flash_WithEmptyAddressOrPath_DoesNotCallService(string address, string path)
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].Address = address;
            vm.FlashItems[0].FilePath = path;

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Прошивка: при пустом списке файлов команда не запускается")]
        public async Task Flash_WithEmptyList_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems.Clear();

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Прошивка: режим, частота и размер флеша попадают в команду")]
        public async Task Flash_WithModeFreqAndSize_PassesThemToCommand()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "boot.bin";
            vm.FlashMode = "dio";
            vm.FlashFreq = "80m";
            vm.FlashSize = "4MB";

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("--flash-mode dio --flash-freq 80m --flash-size 4MB");
        }

        [Fact(DisplayName = "Прошивка: включённые галочки добавляют свои параметры")]
        public async Task Flash_WithAllCheckboxesOn_AddsAllOptions()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "boot.bin";
            vm.EraseAll = true;
            vm.NoCompress = true;
            vm.Force = true;

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            using (new AssertionScope())
            {
                _args.Should().Contain("--erase-all");
                _args.Should().Contain("--no-compress");
                _args.Should().Contain("--force");
            }
        }

        [Fact(DisplayName = "Прошивка: без галочек лишних параметров в команде нет")]
        public async Task Flash_WithAllCheckboxesOff_AddsNoExtraOptions()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "boot.bin";

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            using (new AssertionScope())
            {
                _args.Should().NotContain("--erase-all");
                _args.Should().NotContain("--no-compress");
                _args.Should().NotContain("--force");
            }
        }

        [Fact(DisplayName = "Прошивка: несколько файлов уходят парами «адрес файл» в том же порядке")]
        public async Task Flash_WithSeveralFiles_SendsPairsInOrder()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].Address = " 0x1000 ";
            vm.FlashItems[0].FilePath = "boot.bin";
            vm.AddFlashItemCommand.Execute(null);
            vm.FlashItems[1].Address = "0x8000";
            vm.FlashItems[1].FilePath = "table.bin";

            // Act
            await vm.FlashCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("0x1000 \"boot.bin\" 0x8000 \"table.bin\"");
        }

        [Fact(DisplayName = "Список прошивки: «+ Ещё файл» добавляет строку, ✕ удаляет")]
        public void FlashList_AddAndRemove_ChangesList()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            vm.AddFlashItemCommand.Execute(null);
            var countAfterAdd = vm.FlashItems.Count;
            vm.FlashItems[1].RemoveCommand.Execute(null);

            // Assert
            countAfterAdd.Should().Be(2);
            vm.FlashItems.Should().HaveCount(1);
        }

        [Theory(DisplayName = "Проверка: --diff добавляется только при включённой галочке")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyFlash_DiffFlag_DependsOnCheckbox(bool withDiff)
        {
            // Arrange
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "boot.bin";
            vm.VerifyDiff = withDiff;

            // Act
            await vm.VerifyFlashCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("verify-flash");
            _args!.Contains("--diff").Should().Be(withDiff);
        }

        [Fact(DisplayName = "Проверка прошивки: успешный результат отображается в статусе")]
        public async Task VerifyFlash_OnSuccess_SetsSuccessStatus()
        {
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "firmware.bin";

            await vm.VerifyFlashCommand.ExecuteAsync(null);

            vm.Main.Firmware.IsVerificationSuccessful.Should().BeTrue();
            vm.Main.Firmware.VerificationStatusText.Should().Be("Проверка пройдена");
        }

        [Fact(DisplayName = "Проверка прошивки: ненулевой код отображается как ошибка")]
        public async Task VerifyFlash_OnFailure_SetsFailureStatus()
        {
            _service.ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(Task.FromResult(EsptoolCommandResult.Failure("verify mismatch", 2)));
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "firmware.bin";

            await vm.VerifyFlashCommand.ExecuteAsync(null);

            vm.Main.Firmware.IsVerificationFailed.Should().BeTrue();
            vm.Main.Firmware.VerificationStatusText.Should().Be("Проверка не пройдена (код: 2)");
        }

        [Fact(DisplayName = "Стирание региона: пробелы по краям значений убираются")]
        public async Task EraseRegion_TrimsWhitespace()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.EraseRegionAddress = " 0x10000 ";
            vm.EraseRegionSize = " 0x1000 ";

            // Act
            await vm.EraseRegionCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("erase-region 0x10000 0x1000");
        }

        [Theory(DisplayName = "Стирание региона: если не заполнено одно из полей, команда не запускается")]
        [InlineData("0x10000", "")]
        [InlineData("", "0x1000")]
        public async Task EraseRegion_WithOneFieldEmpty_DoesNotCallService(string address, string size)
        {
            // Arrange
            var vm = CreateViewModel();
            vm.EraseRegionAddress = address;
            vm.EraseRegionSize = size;

            // Act
            await vm.EraseRegionCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Чтение flash: адрес, размер и путь в кавычках уходят в команду")]
        public async Task ReadFlash_SendsAddressSizeAndQuotedPath()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.ReadPath = "C:/backup.bin";

            // Act
            await vm.ReadFlashCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("read-flash 0x0 ALL \"C:/backup.bin\"");
        }

        [Fact(DisplayName = "Регистр статуса: чтение передаёт выбранное число байт")]
        public async Task ReadFlashStatus_PassesSelectedBytes()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedStatusBytes = "3";

            // Act
            await vm.ReadFlashStatusCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("read-flash-status --bytes 3");
        }

        [Fact(DisplayName = "Регистр статуса: запись «навсегда» добавляет --non-volatile")]
        public async Task WriteFlashStatus_NonVolatile_AddsFlag()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.StatusValue = "0x40";
            vm.StatusNonVolatile = true;

            // Act
            await vm.WriteFlashStatusCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("write-flash-status --non-volatile --bytes 2 0x40");
        }

        [Fact(DisplayName = "Регистр статуса: запись без галочки «навсегда» не добавляет --non-volatile")]
        public async Task WriteFlashStatus_Volatile_OmitsFlag()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.StatusValue = "0x40";

            // Act
            await vm.WriteFlashStatusCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("write-flash-status --bytes 2 0x40");
        }

        [Fact(DisplayName = "Паспорт флеша: адрес и длина по умолчанию уходят в read-flash-sfdp")]
        public async Task ReadSfdp_SendsAddressAndLength()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            await vm.ReadSfdpCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("read-flash-sfdp 0x0 4");
        }

        [Fact(DisplayName = "Чтение памяти: адрес уходит в read-mem")]
        public async Task ReadMem_SendsAddress()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.MemAddress = "0x3ff00000";

            // Act
            await vm.ReadMemCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("read-mem 0x3ff00000");
        }

        [Fact(DisplayName = "Запись в память: без значения команда не запускается")]
        public async Task WriteMem_WithoutValue_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.MemAddress = "0x3ff00000";

            // Act
            await vm.WriteMemCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Запись в память: без маски аргумент маски не добавляется")]
        public async Task WriteMem_WithoutMask_OmitsMask()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.MemAddress = "0x3ff00000";
            vm.MemValue = "0x1";

            // Act
            await vm.WriteMemCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("write-mem 0x3ff00000 0x1");
        }

        [Fact(DisplayName = "Запись в память: маска добавляется последним аргументом")]
        public async Task WriteMem_WithMask_AppendsMask()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.MemAddress = "0x3ff00000";
            vm.MemValue = "0x1";
            vm.MemMask = "0xff";

            // Act
            await vm.WriteMemCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("write-mem 0x3ff00000 0x1 0xff");
        }

        [Fact(DisplayName = "Выгрузка памяти: адрес, размер и путь уходят в dump-mem")]
        public async Task DumpMem_SendsAddressSizeAndQuotedPath()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.DumpAddress = "0x40000000";
            vm.DumpSize = "0x1000";
            vm.DumpPath = "d.bin";

            // Act
            await vm.DumpMemCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("dump-mem 0x40000000 0x1000 \"d.bin\"");
        }

        [Fact(DisplayName = "Запуск в RAM: путь к файлу уходит в load-ram в кавычках")]
        public async Task LoadRam_SendsQuotedPath()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.LoadRamPath = "C:/my fw/app.bin";

            // Act
            await vm.LoadRamCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("load-ram \"C:/my fw/app.bin\"");
        }

        [Fact(DisplayName = "Что внутри .bin: команда уходит без порта")]
        public async Task ImageInfo_IsOffline()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.ImageInfoPath = "fw.bin";

            // Act
            await vm.ImageInfoCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("image-info \"fw.bin\"");
            _args.Should().NotContain("--port");
        }

        [Fact(DisplayName = "ELF в образ: при чипе auto команда не запускается")]
        public async Task Elf2Image_WithAutoChip_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.ElfPath = "a.elf";
            vm.ElfOutputPath = "a.bin";

            // Act
            await vm.Elf2ImageCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Theory(DisplayName = "ELF в образ: без входного или выходного файла команда не запускается")]
        [InlineData("", "a.bin")]
        [InlineData("a.elf", "")]
        public async Task Elf2Image_WithMissingFile_DoesNotCallService(string elfPath, string outputPath)
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.ElfPath = elfPath;
            vm.ElfOutputPath = outputPath;

            // Act
            await vm.Elf2ImageCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "ELF в образ: с выбранным чипом команда уходит без порта")]
        public async Task Elf2Image_WithConcreteChip_BuildsOfflineCommand()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.ElfPath = "a.elf";
            vm.ElfOutputPath = "a.bin";

            // Act
            await vm.Elf2ImageCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(1);
            _args.Should().Contain("--chip esp32");
            _args.Should().Contain("elf2image");
            _args.Should().NotContain("--port");
        }

        [Fact(DisplayName = "ELF в образ: выходной и входной файлы уходят в кавычках в нужном порядке")]
        public async Task Elf2Image_SendsOutputThenInputInQuotes()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.ElfPath = "a.elf";
            vm.ElfOutputPath = "a.bin";

            // Act
            await vm.Elf2ImageCommand.ExecuteAsync(null);

            // Assert
            _args.Should().EndWith("-o \"a.bin\" \"a.elf\"");
        }

        [Fact(DisplayName = "Склейка: без выбранного чипа команда не запускается")]
        public async Task MergeBin_WithAutoChip_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.MergeOutputPath = "out.bin";
            vm.FlashItems[0].FilePath = "boot.bin";

            // Act
            await vm.MergeBinCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Склейка: без пути результата команда не запускается")]
        public async Task MergeBin_WithoutOutputPath_DoesNotCallService()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.FlashItems[0].FilePath = "boot.bin";

            // Act
            await vm.MergeBinCommand.ExecuteAsync(null);

            // Assert
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Склейка: берёт файлы из списка прошивки и путь результата")]
        public async Task MergeBin_UsesFlashItemsAndOutputPath()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.MergeOutputPath = "out.bin";
            vm.FlashItems[0].Address = "0x0";
            vm.FlashItems[0].FilePath = "boot.bin";

            // Act
            await vm.MergeBinCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("merge-bin -o \"out.bin\"");
            _args.Should().Contain("0x0 \"boot.bin\"");
        }

        [Fact(DisplayName = "Склейка: выбранный формат и параметры флеша попадают в команду")]
        public async Task MergeBin_PassesFormatAndFlashOptions()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.SelectedChip = "esp32";
            vm.MergeOutputPath = "out.bin";
            vm.SelectedMergeFormat = "uf2";
            vm.FlashMode = "dio";
            vm.FlashItems[0].FilePath = "boot.bin";

            // Act
            await vm.MergeBinCommand.ExecuteAsync(null);

            // Assert
            _args.Should().Contain("--format uf2");
            _args.Should().Contain("--flash-mode dio");
        }

        [Fact(DisplayName = "Выполнение: во время работы интерфейс занят, после завершения свободен")]
        public async Task Execute_WhileServiceRuns_IsBusyUntilFinished()
        {
            // Arrange
            var gate = new TaskCompletionSource<EsptoolCommandResult>();
            _service
                .ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(_ => gate.Task);
            var vm = CreateViewModel();

            // Act
            var running = vm.ChipIdCommand.ExecuteAsync(null);
            var busyWhileRunning = vm.IsBusy;
            gate.SetResult(EsptoolCommandResult.Success());
            await running;

            // Assert
            busyWhileRunning.Should().BeTrue();
            vm.IsBusy.Should().BeFalse();
        }

        [Fact(DisplayName = "Выполнение: вторая команда во время работы первой игнорируется")]
        public async Task Execute_WhileBusy_IgnoresSecondCommand()
        {
            // Arrange
            var gate = new TaskCompletionSource<EsptoolCommandResult>();
            _service
                .ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(_ => gate.Task);
            var vm = CreateViewModel();
            var first = vm.ChipIdCommand.ExecuteAsync(null);

            // Act
            await vm.FlashIdCommand.ExecuteAsync(null);
            gate.SetResult(EsptoolCommandResult.Success());
            await first;

            // Assert
            _ = _service.Received(1).ExecuteCommandAsync(
                Arg.Any<string>(),
                Arg.Any<Action<string>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "Выполнение: если сервис упал с ошибкой, интерфейс снова разблокируется")]
        public async Task ChipId_WhenServiceThrows_ResetsIsBusy()
        {
            // Arrange
            _service
                .ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(
                    Task.FromException<EsptoolCommandResult>(new Exception("boom")));

            var vm = CreateViewModel();

            // Act
            await vm.ChipIdCommand.ExecuteAsync(null);

            // Assert
            vm.IsBusy.Should().BeFalse();
        }

        [Fact(DisplayName = "Отмена: во время работы прерывает операцию и разблокирует интерфейс")]
        public async Task Cancel_WhileRunning_CancelsOperationAndResetsIsBusy()
        {
            // Arrange
            var started = new TaskCompletionSource();

            _service
                .ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(async call =>
                {
                    started.SetResult();

                    await Task.Delay(
                        Timeout.Infinite,
                        call.Arg<CancellationToken>());
                    return EsptoolCommandResult.Cancelled();
                });

            var vm = CreateViewModel();

            var running = vm.ChipIdCommand.ExecuteAsync(null);

            await started.Task;

            // Act
            vm.CancelCommand.Execute(null);
            await running;

            // Assert
            vm.IsBusy.Should().BeFalse();
        }

        [Fact(DisplayName = "Отмена: когда ничего не выполняется, нажатие ничего не ломает")]
        public void Cancel_WhenNothingRuns_DoesNothing()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            var act = () => vm.CancelCommand.Execute(null);

            // Assert
            act.Should().NotThrow();
            _calls.Should().Be(0);
        }

        [Fact(DisplayName = "Отмена: запись прошивки без окна подтверждения всё равно прерывается")]
        public async Task Cancel_DuringFlashWithoutWindow_CancelsToken()
        {
            // Arrange
            var captured = CancellationToken.None;
            _service
                .ExecuteCommandAsync(default!, default!, default)
                .ReturnsForAnyArgs(async call =>
                {
                    captured = call.Arg<CancellationToken>();
                    await Task.Delay(Timeout.Infinite, captured);
                    return EsptoolCommandResult.Cancelled();
                });
            var vm = CreateViewModel();
            vm.FlashItems[0].FilePath = "boot.bin";
            var running = vm.FlashCommand.ExecuteAsync(null);

            // Act
            vm.CancelCommand.Execute(null);
            await running;

            // Assert
            captured.IsCancellationRequested.Should().BeTrue();
            vm.IsBusy.Should().BeFalse();
        }

        [Fact(DisplayName = "Лог: кнопка «Очистить» стирает весь текст")]
        public void ClearLog_RemovesAllText()
        {
            // Arrange
            var vm = CreateViewModel();

            // Act
            vm.ClearLogCommand.Execute(null);

            // Assert
            vm.ConsoleOutput.Should().BeEmpty();
        }
    }
}