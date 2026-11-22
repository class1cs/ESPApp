using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Tests;

/// <summary>
/// Deterministic replacement for the external esptool process.
/// Captures the individual argument tokens and cancellation token supplied by the ViewModels.
/// </summary>
internal sealed class RecordingEspToolService : IEspToolService
{
    public Func<IReadOnlyList<string>, Action<string>, CancellationToken, Task<EsptoolCommandResult>> Handler { get; set; }
        = (_, _, _) => Task.FromResult(EsptoolCommandResult.Success());

    public int CallCount { get; private set; }
    public IReadOnlyList<string>? LastArguments { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }
    public Action<string>? LastOutputCallback { get; private set; }

    public Task<EsptoolCommandResult> ExecuteCommandAsync(
        IReadOnlyList<string> arguments,
        Action<string> onOutputLine,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastArguments = arguments.ToArray();
        LastCancellationToken = cancellationToken;
        LastOutputCallback = onOutputLine;

        return Handler(arguments, onOutputLine, cancellationToken);
    }
}

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

internal sealed class FakeEspToolConfigurator : IEspToolConfigurator
{
    public EsptoolSource? Current { get; private set; }
    public List<EsptoolSource> UsedSources { get; } = new();
    public List<EsptoolSource> ProbeRequests { get; } = new();

    public Func<EsptoolSource, CancellationToken, Task<EsptoolProbe>> ProbeHandler { get; set; }
        = (_, _) => Task.FromResult(new EsptoolProbe(false, "не найдено"));

    public void Use(EsptoolSource source)
    {
        Current = source;
        UsedSources.Add(source);
    }

    public Task<EsptoolProbe> ProbeAsync(
        EsptoolSource source,
        CancellationToken cancellationToken = default)
    {
        ProbeRequests.Add(source);
        return ProbeHandler(source, cancellationToken);
    }
}
