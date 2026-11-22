using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Tests;

internal sealed class FakeSettingsService : ISettingsService
{
    public FakeSettingsService(AppSettings settings) => Settings = settings;

    public AppSettings Settings { get; }
    public bool SaveResult { get; set; } = true;
    public int SaveCallCount { get; private set; }
    public AppSettings? LastSavedSettings { get; private set; }

    public AppSettings Load() => Settings;

    public bool Save(AppSettings settings)
    {
        SaveCallCount++;
        LastSavedSettings = settings;
        return SaveResult;
    }
}