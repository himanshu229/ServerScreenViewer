using System.Diagnostics;

namespace ServerScreenViewer;

internal sealed class HostForm : Form
{
    private readonly AppConfig _config;
    private readonly WebHostService _server;
    private readonly Label _statusLabel;
    private readonly NotifyIcon? _trayIcon;
    private bool _exitRequested;

    public HostForm(AppConfig config, WebHostService server)
    {
        _config = config;
        _server = server;

        Text = "Server Screen Viewer";
        Width = 670;
        Height = 360;
        MinimumSize = new Size(590, 320);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = !_config.HideFromTaskbar;
        Icon = SystemIcons.Application;

        var title = new Label
        {
            Text = "Server Screen Viewer",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
        _statusLabel = new Label
        {
            Text = "Starting…",
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Margin = new Padding(0, 0, 0, 18)
        };
        var urlLabel = MakeValueLabel("Viewer URL", _config.ViewerUrl);
        var keyLabel = MakeValueLabel("6-digit access code", _config.ApiKey);
        var modeLabel = MakeValueLabel("Taskbar mode", _config.HideFromTaskbar ? "Hidden (tray access remains available)" : "Visible");
        var note = new Label
        {
            Text = "View-only: this application does not accept keyboard, mouse, file-transfer, or command input.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 12, 0, 0)
        };

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 18, 0, 0)
        };
        buttons.Controls.Add(MakeButton("Open viewer", (_, _) => OpenUrl(_config.ViewerUrl)));
        buttons.Controls.Add(MakeButton("Copy URL", (_, _) => CopyText(_config.ViewerUrl)));
        buttons.Controls.Add(MakeButton("Copy access key", (_, _) => CopyText(_config.ApiKey)));
        buttons.Controls.Add(MakeButton("Open configuration", (_, _) => OpenConfig()));
        buttons.Controls.Add(MakeButton("Exit", (_, _) => ExitNow()));

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24)
        };
        panel.Controls.Add(title);
        panel.Controls.Add(_statusLabel);
        panel.Controls.Add(urlLabel);
        panel.Controls.Add(keyLabel);
        panel.Controls.Add(modeLabel);
        panel.Controls.Add(note);
        panel.Controls.Add(buttons);
        Controls.Add(panel);

        if (_config.EnableTrayIcon)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open host window", null, (_, _) => RestoreWindow());
            menu.Items.Add("Open viewer", null, (_, _) => OpenUrl(_config.ViewerUrl));
            menu.Items.Add("Copy viewer URL", null, (_, _) => CopyText(_config.ViewerUrl));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitNow());

            _trayIcon = new NotifyIcon
            {
                Text = "Server Screen Viewer",
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (_, _) => RestoreWindow();
        }

        Shown += OnShownAsync;
        FormClosing += OnFormClosing;
    }

    private async void OnShownAsync(object? sender, EventArgs e)
    {
        try
        {
            await _server.StartAsync();
            _statusLabel.Text = $"Running at {_config.ViewerUrl}";
            _statusLabel.ForeColor = Color.DarkGreen;

            if (_config.StartMinimized)
            {
                BeginInvoke((Action)(() =>
                {
                    if (_config.HideFromTaskbar || _config.EnableTrayIcon)
                        Hide();
                    else
                        WindowState = FormWindowState.Minimized;
                }));
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Could not start: {ex.Message}";
            _statusLabel.ForeColor = Color.DarkRed;
            MessageBox.Show(ex.Message, "Server Screen Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitRequested)
            return;

        if (_config.EnableTrayIcon)
        {
            e.Cancel = true;
            Hide();
            _trayIcon?.ShowBalloonTip(1500, "Server Screen Viewer", "The viewer is still running in the notification area.", ToolTipIcon.Info);
            return;
        }

        _exitRequested = true;
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitNow()
    {
        _exitRequested = true;
        _trayIcon?.Dispose();
        Close();
    }

    private static Label MakeValueLabel(string name, string value) => new()
    {
        Text = $"{name}:  {value}",
        AutoSize = true,
        MaximumSize = new Size(590, 0),
        Margin = new Padding(0, 5, 0, 5)
    };

    private static Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 8, 8) };
        button.Click += click;
        return button;
    }

    private static void CopyText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Clipboard error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static void OpenConfig()
    {
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{AppPaths.ConfigFile}\"") { UseShellExecute = true });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _trayIcon?.Dispose();
        base.Dispose(disposing);
    }
}
