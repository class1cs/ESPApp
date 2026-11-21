namespace ESPApp.Infrastructure.Models
{
    public enum EsptoolSourceKind
    {
        Python,
        Exe,
        SystemPath
    }

    public sealed record EsptoolSource(
        EsptoolSourceKind Kind,
        string? PythonPath = null,
        string? ExePath = null);

    public sealed record EsptoolProbe(bool Found, string Details);

    public sealed class AppSettings
    {
        public bool IsConfigured { get; set; }

        public EsptoolSourceKind SourceKind { get; set; } = EsptoolSourceKind.Python;

        public string PythonPath { get; set; } = "python";

        public string ExePath { get; set; } = string.Empty;

        public string DefaultChip { get; set; } = "auto";

        public string DefaultBaud { get; set; } = "460800";

        public bool ShowWelcomeOnStart { get; set; } = true;

        public EsptoolSource ToSource()
        {
            return new EsptoolSource(
                SourceKind,
                PythonPath,
                string.IsNullOrWhiteSpace(ExePath) ? null : ExePath);
        }
    }
}
