using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace TPDownloader.UI;

internal class ProxySettingsForm : Form
{
    private static readonly Uri[] TradingPaintsTestUris =
    [
        new("https://dl.tradingpaints.com/"),
        new("https://dl1.tradingpaints.gg/"),
        new("https://dl2.tradingpaints.gg/"),
        new("https://fetch.tradingpaints.gg/"),
        new("https://showroom-assets.tradingpaints.gg/"),
        new("https://update.tradingpaints.gg/"),
    ];

    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory,
        "appsettings.json"
    );

    private readonly TextBox _urlTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _usernameTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _passwordTextBox = new()
    {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        UseSystemPasswordChar = true,
    };
    private readonly CheckBox _sshTunnelCheckBox = new()
    {
        Text = "Use SSH tunnel",
        AutoSize = true,
    };
    private readonly TextBox _sshHostTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly NumericUpDown _sshPortInput = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = 22,
        Anchor = AnchorStyles.Left,
        Width = 90,
    };
    private readonly TextBox _sshUsernameTextBox = new()
    {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
    };
    private readonly NumericUpDown _localSocksPortInput = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = 7890,
        Anchor = AnchorStyles.Left,
        Width = 90,
    };
    private readonly TextBox _identityFileTextBox = new()
    {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
    };
    private readonly CheckBox _bypassLocalCheckBox = new()
    {
        Text = "Bypass proxy for local addresses",
        Checked = true,
        AutoSize = true,
    };
    private readonly CheckBox _autostartCheckBox = new()
    {
        Text = "Run TPDownloader when Windows starts",
        AutoSize = true,
    };
    private readonly Button _testButton = new() { Text = "Test", Width = 90 };

    public ProxySettingsForm()
    {
        Text = "Proxy Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(660, 470);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 13,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 11; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        AddLabel(layout, "Proxy URL", 0);
        layout.Controls.Add(_urlTextBox, 1, 0);

        AddLabel(layout, "Proxy username", 1);
        layout.Controls.Add(_usernameTextBox, 1, 1);

        AddLabel(layout, "Proxy password", 2);
        layout.Controls.Add(_passwordTextBox, 1, 2);

        layout.Controls.Add(_bypassLocalCheckBox, 1, 3);
        layout.Controls.Add(_sshTunnelCheckBox, 1, 4);

        AddLabel(layout, "SSH host", 5);
        layout.Controls.Add(_sshHostTextBox, 1, 5);

        AddLabel(layout, "SSH port", 6);
        layout.Controls.Add(_sshPortInput, 1, 6);

        AddLabel(layout, "SSH username", 7);
        layout.Controls.Add(_sshUsernameTextBox, 1, 7);

        AddLabel(layout, "Local SOCKS port", 8);
        layout.Controls.Add(_localSocksPortInput, 1, 8);

        AddLabel(layout, "SSH key file", 9);
        var identityPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
        };
        identityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        identityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        var browseButton = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        browseButton.Click += (_, _) => BrowseIdentityFile();
        identityPanel.Controls.Add(_identityFileTextBox, 0, 0);
        identityPanel.Controls.Add(browseButton, 1, 0);
        layout.Controls.Add(identityPanel, 1, 9);
        layout.Controls.Add(_autostartCheckBox, 1, 10);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
        };
        var saveButton = new Button { Text = "Save", Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        _testButton.Click += async (_, _) => await TestSettingsAsync();
        saveButton.Click += (_, _) =>
        {
            if (!SaveSettings())
                return;

            DialogResult = DialogResult.OK;
            Close();
        };

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(_testButton);
        layout.Controls.Add(buttonPanel, 1, 12);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        _sshTunnelCheckBox.CheckedChanged += (_, _) => UpdateEnabledState();
        LoadSettings();
        UpdateEnabledState();
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int row)
    {
        layout.Controls.Add(
            new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
            },
            0,
            row
        );
    }

    private void LoadSettings()
    {
        try
        {
            var root = LoadRoot();
            root.TryGetPropertyValue("Proxy", out var proxyNode);
            var proxy = proxyNode as JsonObject;
            var sshTunnel = proxy?["SshTunnel"] as JsonObject;

            _urlTextBox.Text =
                proxy?["Url"]?.GetValue<string>()
                ?? Environment.GetEnvironmentVariable("TPDOWNLOADER_PROXY")
                ?? string.Empty;
            _usernameTextBox.Text = proxy?["Username"]?.GetValue<string>() ?? string.Empty;
            _passwordTextBox.Text = proxy?["Password"]?.GetValue<string>() ?? string.Empty;
            _bypassLocalCheckBox.Checked = proxy?["BypassOnLocal"]?.GetValue<bool>() ?? true;
            _sshTunnelCheckBox.Checked = sshTunnel?["Enabled"]?.GetValue<bool>() ?? false;
            _sshHostTextBox.Text = sshTunnel?["Host"]?.GetValue<string>() ?? string.Empty;
            _sshPortInput.Value = GetPortValue(sshTunnel?["Port"], 22);
            _sshUsernameTextBox.Text = sshTunnel?["Username"]?.GetValue<string>() ?? string.Empty;
            _localSocksPortInput.Value = GetPortValue(sshTunnel?["LocalSocksPort"], 7890);
            _identityFileTextBox.Text = sshTunnel?["IdentityFile"]?.GetValue<string>() ?? string.Empty;
            _autostartCheckBox.Checked = AutostartManager.IsEnabled();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load proxy settings: {ex.Message}",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    private bool SaveSettings()
    {
        try
        {
            var root = LoadRoot();
            var url = _urlTextBox.Text.Trim();
            var username = _usernameTextBox.Text.Trim();
            var password = _passwordTextBox.Text;
            var sshEnabled = _sshTunnelCheckBox.Checked;

            if (string.IsNullOrWhiteSpace(url) && !sshEnabled)
            {
                root.Remove("Proxy");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(url) && !Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    MessageBox.Show(
                        "Proxy URL must include a scheme, for example http://127.0.0.1:7890.",
                        "TP Downloader",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return false;
                }

                var proxy = new JsonObject
                {
                    ["BypassOnLocal"] = _bypassLocalCheckBox.Checked,
                };

                if (!string.IsNullOrWhiteSpace(url))
                    proxy["Url"] = url;
                if (!sshEnabled && !string.IsNullOrWhiteSpace(username))
                    proxy["Username"] = username;
                if (!sshEnabled && !string.IsNullOrEmpty(password))
                    proxy["Password"] = password;

                if (sshEnabled)
                {
                    var sshHost = _sshHostTextBox.Text.Trim();
                    var sshUsername = _sshUsernameTextBox.Text.Trim();
                    var identityFile = _identityFileTextBox.Text.Trim();

                    if (!ValidateSshSettings())
                        return false;

                    proxy["SshTunnel"] = new JsonObject
                    {
                        ["Enabled"] = true,
                        ["Host"] = sshHost,
                        ["Port"] = (int)_sshPortInput.Value,
                        ["Username"] = sshUsername,
                        ["LocalSocksPort"] = (int)_localSocksPortInput.Value,
                        ["IdentityFile"] = identityFile,
                    };
                }
                else
                {
                    proxy["SshTunnel"] = new JsonObject
                    {
                        ["Enabled"] = false,
                        ["Host"] = _sshHostTextBox.Text.Trim(),
                        ["Port"] = (int)_sshPortInput.Value,
                        ["Username"] = _sshUsernameTextBox.Text.Trim(),
                        ["LocalSocksPort"] = (int)_localSocksPortInput.Value,
                        ["IdentityFile"] = _identityFileTextBox.Text.Trim(),
                    };
                }

                root["Proxy"] = proxy;
            }

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            AutostartManager.SetEnabled(_autostartCheckBox.Checked);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save proxy settings: {ex.Message}",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return false;
        }
    }

    private async Task TestSettingsAsync()
    {
        _testButton.Enabled = false;

        try
        {
            var proxyUrl = GetTestProxyUrl();
            if (proxyUrl is null)
                return;

            Process? sshProcess = null;
            try
            {
                if (_sshTunnelCheckBox.Checked)
                {
                    var localPort = (int)_localSocksPortInput.Value;
                    if (!await IsLocalPortOpenAsync(localPort))
                    {
                        sshProcess = StartTestSshTunnel();
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        if (sshProcess?.HasExited == true)
                        {
                            var sshError = await sshProcess.StandardError.ReadToEndAsync();
                            throw new InvalidOperationException(
                                string.IsNullOrWhiteSpace(sshError)
                                    ? $"ssh.exe exited with code {sshProcess.ExitCode}."
                                    : sshError.Trim()
                            );
                        }
                    }
                }

                var proxy = new WebProxy(proxyUrl)
                {
                    BypassProxyOnLocal = _bypassLocalCheckBox.Checked,
                };

                if (!_sshTunnelCheckBox.Checked)
                {
                    var username = _usernameTextBox.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(username))
                        proxy.Credentials = new NetworkCredential(username, _passwordTextBox.Text);
                }

                using var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true,
                };
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(20),
                };

                var result = await TestTradingPaintsDomainsAsync(client);
                MessageBox.Show(
                    result,
                    "TP Downloader",
                    MessageBoxButtons.OK,
                    result.Contains("FAILED", StringComparison.Ordinal)
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information
                );
            }
            finally
            {
                if (sshProcess is { HasExited: false })
                    sshProcess.Kill(entireProcessTree: true);

                sshProcess?.Dispose();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Proxy test failed: {ex.Message}",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            _testButton.Enabled = true;
        }
    }

    private static async Task<string> TestTradingPaintsDomainsAsync(HttpClient client)
    {
        var failed = false;
        var result = new StringBuilder()
            .AppendLine("Trading Paints proxy test results:")
            .AppendLine();

        foreach (var uri in TradingPaintsTestUris)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead
                );

                if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                {
                    failed = true;
                    result.AppendLine($"{uri.Host}: FAILED ({(int)response.StatusCode} proxy auth required)");
                    continue;
                }

                result.AppendLine($"{uri.Host}: OK ({(int)response.StatusCode} {response.StatusCode})");
            }
            catch (Exception ex)
            {
                failed = true;
                result.AppendLine($"{uri.Host}: FAILED ({ex.Message})");
            }
        }

        return failed
            ? result.AppendLine().Append("One or more Trading Paints domains failed.").ToString()
            : result.AppendLine().Append("All Trading Paints domains are reachable.").ToString();
    }

    private string? GetTestProxyUrl()
    {
        if (_sshTunnelCheckBox.Checked)
        {
            if (!ValidateSshSettings())
                return null;

            return $"socks5://127.0.0.1:{(int)_localSocksPortInput.Value}";
        }

        var url = _urlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show(
                "Proxy URL is required for testing.",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MessageBox.Show(
                "Proxy URL must include a scheme, for example http://127.0.0.1:7890.",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return null;
        }

        return url;
    }

    private Process? StartTestSshTunnel()
    {
        var startInfo = SshTunnelService.CreateStartInfo(
            _sshHostTextBox.Text.Trim(),
            _sshUsernameTextBox.Text.Trim(),
            (int)_sshPortInput.Value,
            (int)_localSocksPortInput.Value,
            _identityFileTextBox.Text.Trim()
        );

        return Process.Start(startInfo);
    }

    private static async Task<bool> IsLocalPortOpenAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static JsonObject LoadRoot()
    {
        if (!File.Exists(SettingsPath))
            return new JsonObject();

        var content = File.ReadAllText(SettingsPath);
        if (string.IsNullOrWhiteSpace(content))
            return new JsonObject();

        return JsonNode.Parse(content) as JsonObject ?? new JsonObject();
    }

    private void BrowseIdentityFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select SSH key file",
            Filter = "All files (*.*)|*.*",
            FileName = _identityFileTextBox.Text,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _identityFileTextBox.Text = dialog.FileName;
    }

    private void UpdateEnabledState()
    {
        var sshEnabled = _sshTunnelCheckBox.Checked;
        _urlTextBox.Enabled = !sshEnabled;
        _usernameTextBox.Enabled = !sshEnabled;
        _passwordTextBox.Enabled = !sshEnabled;
        _sshHostTextBox.Enabled = sshEnabled;
        _sshPortInput.Enabled = sshEnabled;
        _sshUsernameTextBox.Enabled = sshEnabled;
        _localSocksPortInput.Enabled = sshEnabled;
        _identityFileTextBox.Enabled = sshEnabled;
    }

    private bool ValidateSshSettings()
    {
        if (
            string.IsNullOrWhiteSpace(_sshHostTextBox.Text)
            || string.IsNullOrWhiteSpace(_sshUsernameTextBox.Text)
        )
        {
            MessageBox.Show(
                "SSH host and SSH username are required.",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return false;
        }

        var identityFile = _identityFileTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(identityFile) && !File.Exists(identityFile))
        {
            MessageBox.Show(
                "SSH key file does not exist.",
                "TP Downloader",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return false;
        }

        return true;
    }

    private static decimal GetPortValue(JsonNode? node, int defaultValue)
    {
        var value = node?.GetValue<int>() ?? defaultValue;
        return Math.Clamp(value, 1, 65535);
    }
}
