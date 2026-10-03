namespace ServerScreenViewer;

internal sealed class HostApplicationContext : ApplicationContext
{
    private readonly WebHostService _server;
    private bool _disposed;

    public HostApplicationContext(AppConfig config)
    {
        _server = new WebHostService(config);
        var form = new HostForm(config, _server);
        MainForm = form;
        form.Show();
    }

    protected override void ExitThreadCore()
    {
        if (!_disposed)
        {
            _server.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _disposed = true;
        }
        base.ExitThreadCore();
    }
}
