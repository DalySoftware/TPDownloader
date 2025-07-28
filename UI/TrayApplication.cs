namespace TPDownloader.UI;

internal class TrayApplication(MainForm mainForm, PaintService paintService) : IDisposable
{
    private NotifyIcon? _trayIcon;
    private bool _isDisposed;
    private CancellationTokenSource? _paintServiceCts;

    public void Run()
    {
        _paintServiceCts = new CancellationTokenSource();
        _ = paintService.StartAsync(_paintServiceCts.Token);

        var appIcon = new Icon(Path.Combine("UI", "icon.ico"), new Size(48, 48));
        _trayIcon = new NotifyIcon
        {
            Icon = appIcon,
            Visible = true,
            Text = "TP Downloader",
        };
        mainForm.Icon = appIcon;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, __) => ShowWindow());
        contextMenu.Items.Add("Exit", null, (_, __) => ExitApp());
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, __) => ShowWindow();

        mainForm.Resize += (s, e) =>
        {
            if (mainForm.WindowState == FormWindowState.Minimized)
            {
                mainForm.Hide();
            }
        };
        mainForm.FormClosing += (s, e) =>
        {
            _trayIcon.Visible = false;
        };

        ShowWindow();
        Application.Run(mainForm);
    }

    private void ShowWindow()
    {
        if (!mainForm.Visible)
        {
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
        }
        mainForm.Activate();
    }

    private void ExitApp()
    {
        if (_paintServiceCts is not null)
        {
            _paintServiceCts.Cancel();
            paintService.StopAsync(CancellationToken.None);
        }
        if (_trayIcon is not null)
            _trayIcon.Visible = false;
        Application.Exit();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _trayIcon?.Dispose();
            mainForm?.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
