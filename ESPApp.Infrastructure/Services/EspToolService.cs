using System;
using System.ComponentModel;
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

        public EspToolService(string pythonPath = "python")
        {
            _pythonPath = pythonPath;
        }

        public async Task ExecuteCommandAsync(string arguments, Action<string> onOutputLine, CancellationToken cancellationToken = default)
        {
            var bundled = FindBundledEsptool();

            var startInfo = new ProcessStartInfo
            {
                FileName = bundled ?? _pythonPath,
                Arguments = bundled is null ? $"-u -m esptool {arguments}" : arguments,
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

                onOutputLine(process.ExitCode == 0
                    ? "Готово."
                    : $"Процесс завершился с ошибкой, код: {process.ExitCode}");
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                onOutputLine("Операция отменена.");
            }
            catch (Win32Exception)
            {
                onOutputLine(bundled is null
                    ? $"Ошибка: не найден «{_pythonPath}». Установите Python и выполните: pip install esptool"
                    : $"Ошибка: не удалось запустить {bundled}.");
            }
            catch (Exception ex)
            {
                onOutputLine($"[ERROR] Ошибка выполнения: {ex.Message}");
            }
        }

        private static string? FindBundledEsptool()
        {
            var name = OperatingSystem.IsWindows() ? "esptool.exe" : "esptool";
            var path = Path.Combine(AppContext.BaseDirectory, "esptool", name);
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