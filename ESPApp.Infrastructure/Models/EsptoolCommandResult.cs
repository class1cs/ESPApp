namespace ESPApp.Infrastructure.Models;

public enum EsptoolCommandStatus
{
    Succeeded,
    Failed,
    Cancelled,
    Rejected
}

/// <summary>
/// Structured result of a single esptool process execution.
/// </summary>
public sealed record EsptoolCommandResult(
    EsptoolCommandStatus Status,
    int? ExitCode = null,
    string? Message = null)
{
    public bool IsSuccess => Status == EsptoolCommandStatus.Succeeded;

    public static EsptoolCommandResult Success() => new(EsptoolCommandStatus.Succeeded);
    public static EsptoolCommandResult Failure(string? message = null, int? exitCode = null) =>
        new(EsptoolCommandStatus.Failed, exitCode, message);
    public static EsptoolCommandResult Cancelled(string? message = null) =>
        new(EsptoolCommandStatus.Cancelled, null, message);
    public static EsptoolCommandResult Rejected(string? message = null) =>
        new(EsptoolCommandStatus.Rejected, null, message);
}
