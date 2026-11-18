namespace ESPApp.Infrastructure.Interfaces;

public interface IEspToolService
{
    Task ExecuteCommandAsync(string arguments, Action<string> onOutputLine, CancellationToken cancellationToken = default);
}