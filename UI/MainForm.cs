namespace TPDownloader.UI;

using System.Drawing;
using System.Windows.Forms;

internal class MainForm : Form
{
    private readonly List<(string Message, Color Color)> _logEntries = [];
    private readonly ListBox _logListBox = new() { Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = "TPDownloader Log";
        Controls.Add(_logListBox);
        Width = 600;
        Height = 400;
        _logListBox.DrawMode = DrawMode.OwnerDrawFixed;
        _logListBox.DrawItem += LogListBox_DrawItem;
    }

    public void AppendLog(string message)
    {
        AppendLog(message, Color.Black);
    }

    public void AppendLog(string message, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message, color));
            return;
        }
        _logEntries.Add((message, color));
        if (_logEntries.Count > 1024)
        {
            _logEntries.RemoveAt(0);
            _logListBox.Items.RemoveAt(0);
        }
        _logListBox.Items.Add(message);
        _logListBox.TopIndex = _logListBox.Items.Count - 1;
    }

    public string GetAllLogs()
    {
        return string.Join("\r\n", _logEntries.Select(e => e.Message));
    }

    public void CopyAllLogsToClipboard()
    {
        Clipboard.SetText(GetAllLogs());
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var menu = new ContextMenuStrip();
        var copyItem = new ToolStripMenuItem("Copy All Logs", null, (_, __) => CopyAllLogsToClipboard());
        var saveItem = new ToolStripMenuItem("Save Logs As...", null, (_, __) => SaveLogsAs());
        menu.Items.Add(copyItem);
        menu.Items.Add(saveItem);
        _logListBox.ContextMenuStrip = menu;
    }

    private void SaveLogsAs()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = "TPDownloaderLog.txt"
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, GetAllLogs());
        }
    }

    private void LogListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _logEntries.Count)
            return;
        var (itemText, itemColor) = _logEntries[e.Index];
        e.DrawBackground();
        using var brush = new SolidBrush(itemColor);
        e.Graphics.DrawString(itemText, e.Font ?? this.Font, brush, e.Bounds);
        e.DrawFocusRectangle();
    }
}
