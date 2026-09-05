using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class SshService
{
    private readonly ProcessRunner _runner;
    private readonly RiskEngine _risk;

    public SshService()
        : this(
            new ProcessRunner(),
            new RiskEngine())
    {
    }

    public SshService(
        ProcessRunner runner,
        RiskEngine risk)
    {
        _runner = runner;
        _risk = risk;
    }

    public async Task<CommandResult> ExecuteAsync(
        string host,
        string user,
        string command,
        int port = 22,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException(
                "SSH 主机不能为空。",
                nameof(host));
        }

        if (string.IsNullOrWhiteSpace(user))
        {
            throw new ArgumentException(
                "SSH 用户不能为空。",
                nameof(user));
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException(
                "SSH 命令不能为空。",
                nameof(command));
        }

        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port));
        }

        var risk =
            _risk.Assess(command);

        if (risk >= RiskLevel.High)
        {
            throw new InvalidOperationException(
                $"SSH 命令风险等级为 {risk}，已阻止。");
        }

        return await _runner.RunAsync(
            "ssh.exe",
            new[]
            {
                "-p",
                port.ToString(),
                $"{user}@{host}",
                command
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    // 兼容旧版 MainViewModel：
    // 原代码调用 RunAsync(host, user, port, command)
    public Task<CommandResult> RunAsync(
        string host,
        string user,
        int port,
        string command,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            host,
            user,
            command,
            port,
            cancellationToken);
    }
}
