using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class MagiskService
{
    private readonly ToolLocator _tools = new();

    private readonly ProcessRunner _runner = new();

    public async Task<MagiskInfo?> InspectAsync(
        string serial,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null ||
            string.IsNullOrWhiteSpace(serial))
        {
            return null;
        }

        var root =
            await RunSu(
                serial,
                "id",
                ct);

        var rootDetected =
            root.ExitCode == 0 &&
            root.StdOut.Contains(
                "uid=0",
                StringComparison.OrdinalIgnoreCase);

        var version =
            await RunSu(
                serial,
                "magisk -V",
                ct);

        var magiskDetected =
            version.ExitCode == 0 &&
            !string.IsNullOrWhiteSpace(
                version.StdOut);

        var zygisk =
            await RunSu(
                serial,
                "getprop persist.sys.magisk.hide",
                ct);

        var denyList =
            await RunSu(
                serial,
                "ls -la /data/adb",
                ct);

        var installPath =
            await RunSu(
                serial,
                "test -d /data/adb/magisk && echo /data/adb/magisk || true",
                ct);

        var raw =
            string.Join(
                Environment.NewLine,
                version.StdOut,
                zygisk.StdOut,
                denyList.StdOut,
                installPath.StdOut);

        return new MagiskInfo
        {
            RootDetected = rootDetected,

            MagiskDetected = magiskDetected,

            Version = version.StdOut.Trim(),

            Zygisk = zygisk.StdOut.Trim(),

            DenyList = denyList.StdOut.Trim(),

            InstallPath = installPath.StdOut.Trim(),

            Raw = raw
        };
    }

    public async Task<List<string>> GetModulesAsync(
        string serial,
        CancellationToken ct = default)
    {
        var result =
            new List<string>();

        var command =
            await RunSu(
                serial,
                "if [ -d /data/adb/modules ]; then find /data/adb/modules -maxdepth 1 -mindepth 1 -type d -printf '%f\\n'; fi",
                ct);

        if (command.ExitCode != 0)
        {
            return result;
        }

        foreach (var line in command.StdOut.Split(
                     new[] { '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            result.Add(line.Trim());
        }

        return result;
    }

    private async Task<CommandResult> RunSu(
        string serial,
        string command,
        CancellationToken ct)
    {
        return await _runner.RunAsync(
            _tools.Adb!,
            new[]
            {
                "-s",
                serial,
                "shell",
                "su",
                "-c",
                command
            },
            TimeSpan.FromSeconds(15),
            ct);
    }
}
