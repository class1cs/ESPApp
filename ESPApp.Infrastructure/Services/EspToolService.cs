using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Services
{
    public class EspToolService : IEspToolService, IEspToolConfigurator
    {
        private readonly string _pythonPath;
        private readonly string _bundledDirectory;
        private (string FileName, IReadOnlyList<string> PrefixArguments)? _launcher;
        private EsptoolSource? _source;

        public EspToolService(string pythonPath = "python", string? bundledDirectory = null)
        {
            _pythonPath = pythonPath;
            _bundledDirectory = bundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "esptool");
        }

        public EsptoolSource? Current => _source;

        public void Use(EsptoolSource source)
        {
            _source = source;
            _launcher = null;
        }

        public async Task<EsptoolProbe> ProbeAsync(EsptoolSource source, CancellationToken cancellationToken = default)
        {
            var launcher = LauncherFor(source);
            var lines = new List<string>();
            var sync = new object();

            void Collect(string? data)
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    return;
                }

                lock (sync)
                {
                    lines.Add(data.Trim());
                }
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var process = new Process
            {
                StartInfo = CreateStartInfo(
                    launcher.FileName,
                    launcher.PrefixArguments.Append("version"))
            };
            process.OutputDataReceived += (_, args) => Collect(args.Data);
            process.ErrorDataReceived += (_, args) => Collect(args.Data);

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return new EsptoolProbe(
                    false,
                    cancellationToken.IsCancellationRequested ? "проверка отменена" : "не ответил за 30 секунд");
            }
            catch (Win32Exception)
            {
                return new EsptoolProbe(false, source.Kind switch
                {
                    EsptoolSourceKind.Python => "Python не найден",
                    EsptoolSourceKind.Exe => $"файл не найден: {launcher.FileName}",
                    _ => "команда esptool не найдена в PATH"
                });
            }
            catch (Exception ex)
            {
                return new EsptoolProbe(false, ex.Message);
            }

            string text;
            string? first;
            lock (sync)
            {
                text = string.Join(" ", lines);
                first = lines.FirstOrDefault(l => l.Contains("esptool", StringComparison.OrdinalIgnoreCase))
                        ?? lines.FirstOrDefault();
            }

            if (process.ExitCode == 0)
            {
                return new EsptoolProbe(true, first ?? "отвечает");
            }

            if (text.Contains("No module named", StringComparison.OrdinalIgnoreCase))
            {
                return new EsptoolProbe(false, "Python найден, но модуль esptool не установлен (pip install esptool)");
            }

            return new EsptoolProbe(
                false,
                string.IsNullOrWhiteSpace(text) ? $"процесс завершился с кодом {process.ExitCode}" : text);
        }

        public async Task<EsptoolCommandResult> ExecuteCommandAsync(
            IReadOnlyList<string> arguments,
            Action<string> onOutputLine,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            ArgumentNullException.ThrowIfNull(onOutputLine);

            if (arguments.Count == 0 || string.IsNullOrWhiteSpace(arguments[0]))
            {
                const string message = "Не указана команда esptool.";
                onOutputLine($"Ошибка: {message}");
                return EsptoolCommandResult.Failure(message);
            }

            var launcher = await ResolveLauncherAsync();
            if (launcher is null)
            {
                const string message = "esptool не найден. Установите Python и выполните: pip install esptool, либо положите esptool.exe в папку «esptool» рядом с программой.";
                onOutputLine($"Ошибка: {message}");
                return EsptoolCommandResult.Failure(message);
            }

            var processArguments = launcher.Value.PrefixArguments.Concat(arguments);
            using var process = new Process
            {
                StartInfo = CreateStartInfo(launcher.Value.FileName, processArguments)
            };

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    onOutputLine(args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    onOutputLine($"[ERR] {args.Data}");
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);


                if (process.ExitCode == 0)
                {
                    onOutputLine("Готово.");
                    return EsptoolCommandResult.Success();
                }

                var failure = $"Процесс завершился с ошибкой, код: {process.ExitCode}";
                onOutputLine(failure);
                return EsptoolCommandResult.Failure(failure, process.ExitCode);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                onOutputLine("Операция отменена.");
                return EsptoolCommandResult.Cancelled("Операция отменена.");
            }
            catch (Win32Exception ex)
            {
                _launcher = null;
                var message = $"Не удалось запустить «{launcher.Value.FileName}». Проверьте выбор esptool в настройках (кнопка ⚙). {ex.Message}";
                onOutputLine($"Ошибка: {message}");
                return EsptoolCommandResult.Failure(message);
            }
            catch (Exception ex)
            {
                _launcher = null;
                var message = $"Ошибка выполнения: {ex.Message}";
                onOutputLine($"[ERROR] {message}");
                return EsptoolCommandResult.Failure(message);
            }
        }

        private (string FileName, IReadOnlyList<string> PrefixArguments) LauncherFor(EsptoolSource source)
        {
            return source.Kind switch
            {
                EsptoolSourceKind.Python => (
                    string.IsNullOrWhiteSpace(source.PythonPath) ? _pythonPath : source.PythonPath.Trim(),
                    new[] { "-u", "-m", "esptool" }),
                EsptoolSourceKind.Exe => (
                    string.IsNullOrWhiteSpace(source.ExePath) ? DefaultBundledPath() : source.ExePath.Trim(),
                    Array.Empty<string>()),
                _ => ("esptool", Array.Empty<string>())
            };
        }

        private async Task<(string FileName, IReadOnlyList<string> PrefixArguments)?> ResolveLauncherAsync()
        {
            if (_launcher is not null)
            {
                return _launcher;
            }

            if (_source is not null)
            {
                _launcher = LauncherFor(_source);
                return _launcher;
            }

            if (await CanRunAsync(_pythonPath, new[] { "-c", "import esptool" }))
            {
                _launcher = (_pythonPath, new[] { "-u", "-m", "esptool" });
                return _launcher;
            }

            var bundled = FindBundledEsptool();
            if (bundled is not null)
            {
                _launcher = (bundled, Array.Empty<string>());
                return _launcher;
            }

            return null;
        }

        private static ProcessStartInfo CreateStartInfo(string fileName, IEnumerable<string> arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            startInfo.Environment["PYTHONUNBUFFERED"] = "1";
            startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            return startInfo;
        }

        private static async Task<bool> CanRunAsync(string fileName, IReadOnlyList<string> arguments)
        {
            try
            {
                using var process = Process.Start(CreateStartInfo(fileName, arguments));
                if (process is null)
                {
                    return false;
                }

                var standardOutput = process.StandardOutput.ReadToEndAsync();
                var standardError = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                await Task.WhenAll(standardOutput, standardError);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string DefaultBundledPath()
        {
            var name = OperatingSystem.IsWindows() ? "esptool.exe" : "esptool";
            return Path.Combine(_bundledDirectory, name);
        }

        private string? FindBundledEsptool()
        {
            var path = DefaultBundledPath();
            return File.Exists(path) ? path : null;
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        }
    }
}
