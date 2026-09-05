using System.IO;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class WindowsRepairService
{
    private readonly ProcessRunner _runner;

    public WindowsRepairService(
        ProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<CommandResult> RunSfcAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "sfc.exe",
            ["/scannow"],
            TimeSpan.FromMinutes(45),
            cancellationToken);
    }

    public Task<CommandResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "DISM.exe",
            [
                "/Online",
                "/Cleanup-Image",
                "/CheckHealth"
            ],
            TimeSpan.FromMinutes(15),
            cancellationToken);
    }

    public Task<CommandResult> RestoreHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            "DISM.exe",
            [
                "/Online",
                "/Cleanup-Image",
                "/RestoreHealth"
            ],
            TimeSpan.FromMinutes(90),
            cancellationToken);
    }
}