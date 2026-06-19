using System.Reflection;

namespace TPDownloader.UI;

internal class TrayApplication(
    MainForm mainForm,
    PaintService paintService,
    SshTunnelService sshTunnelService
) : IDisposable
{
    private NotifyIcon? _trayIcon;
    private bool _isDisposed;
    private CancellationTokenSource? _paintServiceCts;
    private Task? _paintServiceTask;

    public void Run()
    {
        sshTunnelService.StartIfConfigured();
        _paintServiceCts = new CancellationTokenSource();

        // Start and *observe* faults
        _paintServiceTask = paintService.StartAsync(_paintServiceCts.Token);
        ObservePaintService(_paintServiceTask);

        var appIcon = GetIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = appIcon,
            Visible = true,
            Text = "TP Downloader",
        };
        mainForm.Icon = appIcon;
        mainForm.ProxySettingsRequested += (_, _) => ShowProxySettings();

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (_, __) => ShowWindow());
        contextMenu.Items.Add("Proxy Settings...", null, (_, __) => ShowProxySettings());
        contextMenu.Items.Add("Exit", null, (_, __) => ExitApp());
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, __) => ShowWindow();

        mainForm.Resize += (s, e) =>
        {
            if (mainForm.WindowState == FormWindowState.Minimized)
                mainForm.Hide();
        };
        mainForm.FormClosing += (s, e) =>
        {
            if (_trayIcon is not null) _trayIcon.Visible = false;
        };
        mainForm.FormClosed += (s, e) => ExitApp();

        mainForm.Load += (s, e) => mainForm.Hide();
        Application.Run();
    }

    private void ObservePaintService(Task task)
    {
        var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        _ = task.ContinueWith(t =>
        {
            // Observe the exception
            var ex = t.Exception?.GetBaseException() ?? t.Exception;

            if (_trayIcon is not null)
            {
                _trayIcon.BalloonTipTitle = "TP Downloader crashed";
                _trayIcon.BalloonTipText = ex?.Message ?? "Unknown error";
                _trayIcon.ShowBalloonTip(10_000);
            }

            Environment.FailFast("TP downloader crashed", ex);

        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, uiScheduler);
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

    private void ShowProxySettings()
    {
        using var settingsForm = new ProxySettingsForm();
        if (settingsForm.ShowDialog(mainForm) != DialogResult.OK)
            return;

        MessageBox.Show(
            "Proxy settings were saved. Restart TPDownloader to apply the new network settings.",
            "TP Downloader",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void ExitApp()
    {
        _paintServiceCts?.Cancel();
        paintService.Dispose();
        sshTunnelService.Dispose();

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
            sshTunnelService.Dispose();

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
