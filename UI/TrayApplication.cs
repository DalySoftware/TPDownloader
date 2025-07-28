namespace TPDownloader.UI;

internal class TrayApplication(MainForm mainForm) : IDisposable
{
    private NotifyIcon? _trayIcon;
    private bool _isDisposed;
    private MainForm _mainForm = mainForm;

    public void Run()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "TP Downloader",
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, __) => ShowWindow());
        contextMenu.Items.Add("Exit", null, (_, __) => ExitApp());
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, __) => ShowWindow();

        _mainForm.Resize += (s, e) =>
        {
            if (_mainForm.WindowState == FormWindowState.Minimized)
            {
                _mainForm.Hide();
            }
        };
        _mainForm.FormClosing += (s, e) =>
        {
            _trayIcon.Visible = false;
        };

        ShowWindow();
        Application.Run(_mainForm);
    }

    private void ShowWindow()
    {
        if (_mainForm.Visible == false)
        {
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
        }
        _mainForm.Activate();
    }

    private void ExitApp()
    {
        if (_trayIcon is not null)
            _trayIcon.Visible = false;
        Application.Exit();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _trayIcon?.Dispose();
            _mainForm?.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
