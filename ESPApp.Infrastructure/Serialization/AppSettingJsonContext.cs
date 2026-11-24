using System.Text.Json.Serialization;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsJsonContext
    : JsonSerializerContext;