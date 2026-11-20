using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPApp.Infrastructure.Interfaces;

namespace ESPApp.Infrastructure.Services
{
    public class EspToolService : IEspToolService
    {
        private readonly string _pythonPath;
        private readonly string _bundledDirectory;
        private (string FileName, string Prefix)? _launcher;

        public EspToolService(string pythonPath = "python", string? bundledDirectory = null)
        {
            _pythonPath = pythonPath;
            _bundledDirectory = bundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "esptool");
        }

        public async Task ExecuteCommandAsync(string arguments, Action<string> onOutputLine, CancellationToken cancellationToken = default)
        {
            var launcher = await ResolveLauncherAsync();
            if (launcher is null)
            {
                onOutputLine("Ошибка: esptool не найден. Установите Python и выполните: pip install esptool, либо положите esptool.exe в папку «esptool» рядом с программой.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = launcher.Value.FileName,
                Arguments = launcher.Value.Prefix + arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.Environment["PYTHONUNBUFFERED"] = "1";
            startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    var line = args.Data.TrimEnd();
                    if (line.Length > 0)
                    {
                        onOutputLine(line);
                    }
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    var line = args.Data.TrimEnd();
                    if (line.Length > 0)
                    {
                        onOutputLine($"[ERR] {line}");
                    }
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                onOutputLine(process.ExitCode == 0
                    ? "Готово."
                    : $"Процесс завершился с ошибкой, код: {process.ExitCode}");
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                onOutputLine("Операция отменена.");
            }
            catch (Exception ex)
            {
                _launcher = null;
                onOutputLine($"[ERROR] Ошибка выполнения: {ex.Message}");
            }
        }

        private async Task<(string FileName, string Prefix)?> ResolveLauncherAsync()
        {
            if (_launcher is not null)
            {
                return _launcher;
            }

            if (await CanRunAsync(_pythonPath, "-c \"import esptool\""))
            {
                _launcher = (_pythonPath, "-u -m esptool ");
                return _launcher;
            }

            var bundled = FindBundledEsptool();
            if (bundled is not null)
            {
                _launcher = (bundled, string.Empty);
                return _launcher;
            }

            return null;
        }

        private static async Task<bool> CanRunAsync(string fileName, string arguments)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process is null)
                {
                    return false;
                }

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string? FindBundledEsptool()
        {
            var name = OperatingSystem.IsWindows() ? "esptool.exe" : "esptool";
            var path = Path.Combine(_bundledDirectory, name);
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