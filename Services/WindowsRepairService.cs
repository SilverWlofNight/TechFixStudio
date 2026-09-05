using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class WindowsRepairService
{
    private readonly ProcessRunner _runner;

    public WindowsRepairService()
        : this(new ProcessRunner())
    {
    }

    public WindowsRepairService(ProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<CommandResult> RunSfcAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "sfc.exe",
            new[]
            {
                "/scannow"
            },
            TimeSpan.FromMinutes(45),
            cancellationToken);
    }

    public Task<CommandResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "DISM.exe",
            new[]
            {
                "/Online",
                "/Cleanup-Image",
                "/CheckHealth"
            },
            TimeSpan.FromMinutes(15),
            cancellationToken);
    }

    public Task<CommandResult> RestoreHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "DISM.exe",
            new[]
            {
                "/Online",
                "/Cleanup-Image",
                "/RestoreHealth"
            },
            TimeSpan.FromMinutes(90),
            cancellationToken);
    }

    // 兼容旧版 MainViewModel
    public Task<CommandResult> Sfc(
        CancellationToken cancellationToken = default)
    {
        return RunSfcAsync(cancellationToken);
    }

    // 兼容旧版 MainViewModel
    public Task<CommandResult> DismCheck(
        CancellationToken cancellationToken = default)
    {
        return CheckHealthAsync(cancellationToken);
    }

    // 兼容旧版 MainViewModel
    public Task<CommandResult> DismRestore(
        CancellationToken cancellationToken = default)
    {
        return RestoreHealthAsync(cancellationToken);
    }
}
