using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Tests;

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
