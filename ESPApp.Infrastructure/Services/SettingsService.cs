using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;
using ESPApp.Infrastructure.Serialization;

namespace ESPApp.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _filePath;

        public SettingsService(string? filePath = null)
        {
            _filePath = filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ESPApp",
                "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new AppSettings();
                }

                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize(
                    json,
                    AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public bool Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                File.WriteAllText(_filePath, JsonSerializer.Serialize(
                    settings,
                    AppSettingsJsonContext.Default.AppSettings));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
