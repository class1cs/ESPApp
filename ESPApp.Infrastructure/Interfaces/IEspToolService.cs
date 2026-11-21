using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Interfaces;

public interface IEspToolService
{
    Task<EsptoolCommandResult> ExecuteCommandAsync(
        string arguments,
        Action<string> onOutputLine,
        CancellationToken cancellationToken = default);
}
