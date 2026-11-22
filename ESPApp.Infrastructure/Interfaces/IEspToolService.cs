using System.Collections.Generic;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Interfaces;

public interface IEspToolService
{
    /// <summary>
    /// Executes esptool with already separated command-line arguments.
    /// Each collection item is passed to the process as one argument.
    /// </summary>
    Task<EsptoolCommandResult> ExecuteCommandAsync(
        IReadOnlyList<string> arguments,
        Action<string> onOutputLine,
        CancellationToken cancellationToken = default);
}
