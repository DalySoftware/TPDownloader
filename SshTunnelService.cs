using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TPDownloader;

internal class SshTunnelService(IConfiguration configuration, ILogger<SshTunnelService> logger) : IDisposable
{
    private Process? _process;
    private bool _disposed;

    public void StartIfConfigured()
    {
        if (!Program.GetBool(configuration, "Proxy:SshTunnel:Enabled", false))
            return;

        var host = configuration["Proxy:SshTunnel:Host"]?.Trim();
        var username = configuration["Proxy:SshTunnel:Username"]?.Trim();
        var identityFile = configuration["Proxy:SshTunnel:IdentityFile"]?.Trim();
        var sshPort = Program.GetInt(configuration, "Proxy:SshTunnel:Port", 22);
        var localPort = Program.GetInt(configuration, "Proxy:SshTunnel:LocalSocksPort", 7890);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            logger.LogWarning("SSH tunnel is enabled, but host or username is missing.");
            return;
        }

        if (!IsPortValid(sshPort) || !IsPortValid(localPort))
        {
            logger.LogWarning("SSH tunnel is enabled, but one of the configured ports is invalid.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(identityFile) && !File.Exists(identityFile))
        {
            logger.LogWarning("SSH identity file does not exist: {IdentityFile}", identityFile);
            return;
        }

        var startInfo = CreateStartInfo(host, username, sshPort, localPort, identityFile);

        try
        {
            _process = Process.Start(startInfo);
            if (_process is null)
            {
                logger.LogError("Failed to start ssh.exe for SSH tunnel.");
                return;
            }

            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) =>
            {
                var exitCode = _process?.ExitCode;
                logger.LogWarning("SSH tunnel process exited with code {ExitCode}.", exitCode);
            };

            _ = Task.Run(() => LogProcessOutput(_process));
            logger.LogInformation(
                "SSH tunnel started. Trading Paints traffic will use socks5://127.0.0.1:{LocalPort}.",
                localPort
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start ssh.exe for SSH tunnel.");
        }
    }

    private async Task LogProcessOutput(Process process)
    {
        try
        {
            while (!process.HasExited)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line is null)
                    break;

                logger.LogWarning("ssh.exe: {Line}", line);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Stopped reading ssh.exe output.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to stop SSH tunnel process.");
            }
        }

        _process?.Dispose();
        _disposed = true;
    }

    internal static ProcessStartInfo CreateStartInfo(
        string host,
        string username,
        int sshPort,
        int localPort,
        string? identityFile
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ssh.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        startInfo.ArgumentList.Add("-N");
        startInfo.ArgumentList.Add("-D");
        startInfo.ArgumentList.Add($"127.0.0.1:{localPort}");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(sshPort.ToString());
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("ExitOnForwardFailure=yes");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("ServerAliveInterval=30");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("ServerAliveCountMax=3");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("BatchMode=yes");

        if (!string.IsNullOrWhiteSpace(identityFile))
        {
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(identityFile);
        }

        startInfo.ArgumentList.Add($"{username}@{host}");
        return startInfo;
    }

    internal static bool IsPortValid(int port) => port is > 0 and <= 65535;
}
