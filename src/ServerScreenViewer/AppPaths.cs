namespace ServerScreenViewer;

internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ServerScreenViewer");

    public static string ConfigFile { get; } = Path.Combine(DataDirectory, "appsettings.json");
}
