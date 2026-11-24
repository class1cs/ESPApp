using System.Collections.Generic;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Interfaces;

public interface IEspToolService
{
    Task<EsptoolCommandResult> ExecuteCommandAsync(
        IReadOnlyList<string> arguments,
        Action<string> onOutputLine,
        CancellationToken cancellationToken = default);
}
