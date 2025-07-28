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
        _logListBox.Items.Add(message);
        _logListBox.TopIndex = _logListBox.Items.Count - 1;
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
