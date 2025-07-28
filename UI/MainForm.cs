namespace TPDownloader.UI;

using System.Windows.Forms;

internal class MainForm : Form
{
    private readonly ListBox _logListBox = new() { Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = "TPDownloader Log";
        Controls.Add(_logListBox);
        Width = 600;
        Height = 400;
    }

    public void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message));
            return;
        }
        _logListBox.Items.Add(message);
        _logListBox.TopIndex = _logListBox.Items.Count - 1;
    }
}
