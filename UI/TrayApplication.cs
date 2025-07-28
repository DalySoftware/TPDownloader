using System.Reflection;

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

        var appIcon = GetIcon();
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
        mainForm.FormClosed += (s, e) => ExitApp();

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
        _paintServiceCts?.Cancel();
        paintService.Dispose();

        if (_trayIcon is not null)
            _trayIcon.Visible = false;
        Application.Exit();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _paintServiceCts?.Cancel();
            paintService.Dispose();

            _trayIcon?.Dispose();
            mainForm?.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private static Icon GetIcon()
    {
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("TPDownloader.UI.icon.ico");
        return stream is not null
            ? new Icon(stream, new Size(48, 48))
            : new Icon(SystemIcons.Application, new Size(48, 48));
    }
}
