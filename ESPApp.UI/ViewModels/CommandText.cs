namespace ESPApp.UI.ViewModels;

internal static class CommandText
{
    public static string Quote(string value) => "\"" + value.Trim() + "\"";
}
