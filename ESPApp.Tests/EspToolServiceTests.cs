using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Services;
using ESPApp.UI.ViewModels;
using FluentAssertions;

namespace ESPApp.Tests
{
    public static class Runner
    {
        public static async Task<List<string>> RunAsync(
            IEspToolService service,
            string arguments,
            CancellationToken cancellationToken = default)
        {
            var lines = new List<string>();
            var sync = new object();

            await service.ExecuteCommandAsync(
                arguments,
                line =>
                {
                    lock (sync)
                    {
                        lines.Add(line);
                    }
                },
                cancellationToken);

            lock (sync)
            {
                return lines.ToList();
            }
        }
    }

    public static class RealEsptool
    {
        private static readonly Lazy<Task<bool>> Availability = new(CheckAsync);

        public static Task<bool> IsAvailableAsync() => Availability.Value;

        private static async Task<bool> CheckAsync()
        {
            var lines = await Runner.RunAsync(new EspToolService(), "version");
            return lines.Contains("Готово.");
        }
    }

    public sealed class FakePython : IDisposable
    {
        private readonly string _directory;

        public string FilePath { get; }

        public int ProbeCount
        {
            get
            {
                var probes = Path.Combine(_directory, "probes.txt");
                return File.Exists(probes) ? File.ReadAllLines(probes).Length : 0;
            }
        }

        public FakePython(int exitCode = 0, int probeExitCode = 0, int sleepSeconds = 0, int outputLines = 0)
        {
            _directory = Path.Combine(Path.GetTempPath(), "fake-python-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            if (OperatingSystem.IsWindows())
            {
                FilePath = Path.Combine(_directory, "fake-python.cmd");

                var linesCommand = outputLines > 0
                    ? $"for /L %%i in (1,1,{outputLines}) do echo LINE %%i"
                    : "rem";
                var sleepCommand = sleepSeconds > 0
                    ? $"ping -n {sleepSeconds + 1} 127.0.0.1 >nul"
                    : "rem";

                File.WriteAllText(FilePath,
                    "@echo off\r\n" +
                    "if \"%~1\"==\"-c\" (\r\n" +
                    "  echo x>>\"%~dp0probes.txt\"\r\n" +
                    $"  exit /b {probeExitCode}\r\n" +
                    ")\r\n" +
                    "echo ARGS: %*\r\n" +
                    "echo ENV: %PYTHONUNBUFFERED% %PYTHONIOENCODING%\r\n" +
                    "echo some warning 1>&2\r\n" +
                    $"{linesCommand}\r\n" +
                    $"{sleepCommand}\r\n" +
                    $"exit /b {exitCode}\r\n");
            }
            else
            {
                FilePath = Path.Combine(_directory, "fake-python.sh");

                var sleepCommand = sleepSeconds > 0 ? $"sleep {sleepSeconds}" : ":";

                File.WriteAllText(FilePath,
                    "#!/bin/sh\n" +
                    "if [ \"$1\" = \"-c\" ]; then\n" +
                    "  echo x >> \"$(dirname \"$0\")/probes.txt\"\n" +
                    $"  exit {probeExitCode}\n" +
                    "fi\n" +
                    "echo \"ARGS: $@\"\n" +
                    "echo \"ENV: $PYTHONUNBUFFERED $PYTHONIOENCODING\"\n" +
                    "echo \"some warning\" 1>&2\n" +
                    $"i=1; while [ $i -le {outputLines} ]; do echo \"LINE $i\"; i=$((i+1)); done\n" +
                    $"{sleepCommand}\n" +
                    $"exit {exitCode}\n");

                File.SetUnixFileMode(FilePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch
            {
            }
        }
    }

    public sealed class FakeBundled : IDisposable
    {
        public string Folder { get; }

        public string Arguments => OperatingSystem.IsWindows() ? "/c echo BUNDLED_OK" : "run";

        public FakeBundled()
        {
            Folder = Path.Combine(Path.GetTempPath(), "fake-bundled-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);

            if (OperatingSystem.IsWindows())
            {
                File.Copy(
                    Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                    Path.Combine(Folder, "esptool.exe"));
            }
            else
            {
                var script = Path.Combine(Folder, "esptool");
                File.WriteAllText(script, "#!/bin/sh\necho BUNDLED_OK\n");
                File.SetUnixFileMode(script,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch
            {
            }
        }
    }

    public sealed class RecordingService : IEspToolService
    {
        public string? LastArguments { get; private set; }

        public Task ExecuteCommandAsync(
            string arguments,
            Action<string> onOutputLine,
            CancellationToken cancellationToken = default)
        {
            LastArguments = arguments;
            return Task.CompletedTask;
        }
    }

    [Trait("Category", "Integration")]
    public class EspToolServiceTests
    {
        private static readonly string NoBundled =
            Path.Combine(Path.GetTempPath(), "esptool-tests-no-bundle");

        [Fact(DisplayName = "Запуск: аргументы доходят до esptool, в конце пишется «Готово.»")]
        public async Task Execute_WithSuccessfulProcess_PassesArgumentsAndReportsDone()
        {
            // Arrange
            using var fake = new FakePython(exitCode: 0);
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "--port COM3 chip-id");

            // Assert
            lines.Should().Contain("ARGS: -u -m esptool --port COM3 chip-id");
            lines.Should().Contain("Готово.");
        }

        [Fact(DisplayName = "Запуск: вывод ошибок esptool получает префикс [ERR]")]
        public async Task Execute_WithStderrOutput_AddsErrPrefix()
        {
            // Arrange
            using var fake = new FakePython();
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "version");

            // Assert
            lines.Should().Contain("[ERR] some warning");
        }

        [Fact(DisplayName = "Запуск: процессу передаются PYTHONUNBUFFERED=1 и PYTHONIOENCODING=utf-8")]
        public async Task Execute_SetsPythonEnvironment()
        {
            // Arrange
            using var fake = new FakePython();
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "version");

            // Assert
            lines.Should().Contain("ENV: 1 utf-8");
        }

        [Fact(DisplayName = "Запуск: ненулевой код выхода попадает в лог")]
        public async Task Execute_WithFailingProcess_ReportsExitCode()
        {
            // Arrange
            using var fake = new FakePython(exitCode: 3);
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "chip-id");

            // Assert
            lines.Should().Contain(l => l.Contains("код: 3"));
            lines.Should().NotContain("Готово.");
        }

        [Fact(DisplayName = "Запуск: большой вывод приходит целиком и по порядку")]
        public async Task Execute_WithLargeOutput_DoesNotLoseOrReorderLines()
        {
            // Arrange
            const int count = 2000;
            using var fake = new FakePython(outputLines: count);
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "version");

            // Assert
            var numbered = lines.Where(l => l.StartsWith("LINE ")).ToList();
            numbered.Should().Equal(Enumerable.Range(1, count).Select(i => $"LINE {i}"));
            lines.Should().Contain("Готово.");
        }

        [Fact(DisplayName = "Поиск: Python есть, но без esptool — в лог идёт сообщение «не найден»")]
        public async Task Execute_WhenPythonHasNoEsptool_ReportsNotFound()
        {
            // Arrange
            using var fake = new FakePython(probeExitCode: 1);
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "version");

            // Assert
            lines.Should().Contain(l => l.Contains("esptool не найден"));
        }

        [Fact(DisplayName = "Поиск: нет ни Python, ни esptool.exe — в лог идёт сообщение «не найден»")]
        public async Task Execute_WhenNothingIsInstalled_ReportsNotFound()
        {
            // Arrange
            var service = new EspToolService("no-such-python-xyz", NoBundled);

            // Act
            var lines = await Runner.RunAsync(service, "version");

            // Assert
            lines.Should().Contain(l => l.Contains("esptool не найден"));
        }

        [Fact(DisplayName = "Поиск: если есть и Python с esptool, и бинарник, используется Python")]
        public async Task Execute_WhenPythonAndBundledExist_PrefersPython()
        {
            // Arrange
            using var fake = new FakePython();
            using var bundled = new FakeBundled();
            var service = new EspToolService(fake.FilePath, bundled.Folder);

            // Act
            var lines = await Runner.RunAsync(service, "--port COM3 chip-id");

            // Assert
            lines.Should().Contain("ARGS: -u -m esptool --port COM3 chip-id");
            lines.Should().NotContain("BUNDLED_OK");
        }

        [Fact(DisplayName = "Поиск: если Python не найден, используется бинарник esptool")]
        public async Task Execute_WhenPythonMissing_FallsBackToBundled()
        {
            // Arrange
            using var bundled = new FakeBundled();
            var service = new EspToolService("no-such-python-xyz", bundled.Folder);

            // Act
            var lines = await Runner.RunAsync(service, bundled.Arguments);

            // Assert
            lines.Should().Contain("BUNDLED_OK");
            lines.Should().Contain("Готово.");
        }

        [Fact(DisplayName = "Поиск: если у Python нет модуля esptool, используется бинарник")]
        public async Task Execute_WhenPythonHasNoEsptoolModule_FallsBackToBundled()
        {
            // Arrange
            using var fake = new FakePython(probeExitCode: 1);
            using var bundled = new FakeBundled();
            var service = new EspToolService(fake.FilePath, bundled.Folder);

            // Act
            var lines = await Runner.RunAsync(service, bundled.Arguments);

            // Assert
            lines.Should().Contain("BUNDLED_OK");
            lines.Should().NotContain(l => l.StartsWith("ARGS:"));
        }

        [Fact(DisplayName = "Поиск: Python проверяется один раз, повторные запуски идут без проверки")]
        public async Task Execute_CalledTwice_ProbesPythonOnce()
        {
            // Arrange
            using var fake = new FakePython();
            var service = new EspToolService(fake.FilePath, NoBundled);

            // Act
            await Runner.RunAsync(service, "version");
            await Runner.RunAsync(service, "version");

            // Assert
            fake.ProbeCount.Should().Be(1);
        }

        [Fact(DisplayName = "Отмена: долгий процесс прерывается быстро, в лог пишется «Операция отменена.»")]
        public async Task Execute_WhenCancelled_KillsProcessQuickly()
        {
            // Arrange
            using var fake = new FakePython(sleepSeconds: 30);
            var service = new EspToolService(fake.FilePath, NoBundled);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var stopwatch = Stopwatch.StartNew();

            // Act
            var lines = await Runner.RunAsync(service, "chip-id", cts.Token);
            stopwatch.Stop();

            // Assert
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
            lines.Should().Contain("Операция отменена.");
        }
    }

    [Trait("Category", "Integration")]
    [Trait("Requires", "esptool")]
    public class EsptoolContractTests
    {
        private static async Task<string> HelpAsync(string arguments)
        {
            // Arrange
            Skip.IfNot(
                await RealEsptool.IsAvailableAsync(),
                "esptool не найден: нужен Python с esptool или esptool.exe в папке «esptool»");

            // Act
            var lines = await Runner.RunAsync(new EspToolService(), arguments);

            // Assert
            lines.Should().Contain("Готово.");
            return string.Concat(lines);
        }

        [SkippableTheory(DisplayName = "Контракт: esptool принимает команду, которую собирает ViewModel")]
        [InlineData("FlashCommand")]
        [InlineData("VerifyFlashCommand")]
        [InlineData("EraseFlashCommand")]
        [InlineData("EraseRegionCommand")]
        [InlineData("ReadFlashCommand")]
        [InlineData("ReadFlashStatusCommand")]
        [InlineData("WriteFlashStatusCommand")]
        [InlineData("ChipIdCommand")]
        [InlineData("FlashIdCommand")]
        [InlineData("ReadMacCommand")]
        [InlineData("SecurityInfoCommand")]
        [InlineData("VersionCommand")]
        [InlineData("ReadSfdpCommand")]
        [InlineData("ReadMemCommand")]
        [InlineData("WriteMemCommand")]
        [InlineData("DumpMemCommand")]
        [InlineData("LoadRamCommand")]
        [InlineData("RunCodeCommand")]
        [InlineData("ImageInfoCommand")]
        [InlineData("Elf2ImageCommand")]
        [InlineData("MergeBinCommand")]
        public async Task ViewModelCommand_IsAcceptedByRealEsptool(string commandName)
        {
            // Arrange
            var recorder = new RecordingService();
            var vm = new MainViewModel(recorder)
            {
                SelectedPort = "COM3",
                SelectedChip = "esp32",
                NoStub = true,
                Trace = true,
                EraseAll = true,
                NoCompress = true,
                Force = true,
                VerifyDiff = true,
                StatusNonVolatile = true,
                EraseRegionAddress = "0x10000",
                EraseRegionSize = "0x1000",
                ReadPath = "backup.bin",
                StatusValue = "0x40",
                MemAddress = "0x3ff00000",
                MemValue = "0x1",
                MemMask = "0xff",
                DumpAddress = "0x40000000",
                DumpSize = "0x1000",
                DumpPath = "dump.bin",
                LoadRamPath = "app.bin",
                ImageInfoPath = "fw.bin",
                ElfPath = "app.elf",
                ElfOutputPath = "app.bin",
                MergeOutputPath = "merged.bin"
            };
            vm.FlashItems[0].FilePath = "boot.bin";
            var command = (IAsyncRelayCommand)typeof(MainViewModel).GetProperty(commandName)!.GetValue(vm)!;
            await command.ExecuteAsync(null);

            // Act
            var help = await HelpAsync($"{recorder.LastArguments} --help");

            // Assert
            help.Should().NotBeEmpty();
        }
    }
}