using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class SshService
{
    private readonly ProcessRunner _runner;
    private readonly RiskEngine _risk;

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

        var risk =
            _risk.Assess(command);

        if (risk is
            RiskLevel.High or
            RiskLevel.Critical)
        {
            throw new InvalidOperationException(
                $"SSH 命令风险等级为 {risk}，已阻止。");
        }

        return await _runner.RunAsync(
            "ssh.exe",
            [
                "-p",
                port.ToString(),
                $"{user}@{host}",
                command
            ],
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }
}