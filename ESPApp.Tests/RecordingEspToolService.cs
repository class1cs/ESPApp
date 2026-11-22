using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Tests;

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