using System.Threading;

namespace ServerScreenViewer;

internal static class Program
{
    private const string MutexName = @"Local\ServerScreenViewer.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Server Screen Viewer is already running in this Windows session.",
                "Server Screen Viewer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var config = AppConfig.LoadOrCreate();
            Application.Run(new HostApplicationContext(config));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The application could not start.\r\n\r\n{ex.Message}\r\n\r\nConfiguration: {AppPaths.ConfigFile}",
                "Server Screen Viewer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
