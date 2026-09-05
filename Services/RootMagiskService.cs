using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RootMagiskService
{
    readonly ToolLocator _t = new();
    readonly ProcessRunner _r = new();

    public async Task<RootMagiskInfo?> InspectAsync(
        DeviceInfo device)
    {
        if (_t.Adb is null)
            return null;

        if (device.Transport != Transport.Adb)
            return null;

        var serial = device.Serial;

        var rootResult = await RunSuAsync(
            serial,
            "id");

        var rootAvailable =
            rootResult.ExitCode == 0 &&
            rootResult.StdOut.Contains(
                "uid=0",
                StringComparison.OrdinalIgnoreCase);

        var magiskVersion =
            await RunShellAsync(
                serial,
                "magisk -v");

        var magiskPath =
            await RunShellAsync(
                serial,
                "command -v magisk");

        var suPath =
            await RunShellAsync(
                serial,
                "command -v su");

        var zygisk =
            await RunShellAsync(
                serial,
                "getprop persist.sys.zgisk");

        if (string.IsNullOrWhiteSpace(zygisk))
        {
            zygisk =
                await RunShellAsync(
                    serial,
                    "getprop ro.dalvik.vm.native.bridge");
        }

        var denyList =
            await RunShellAsync(
                serial,
                "getprop persist.magisk.denylist");

        var modules =
            await GetModulesAsync(serial);

        var magiskDetected =
            !string.IsNullOrWhiteSpace(
                magiskVersion) ||
            !string.IsNullOrWhiteSpace(
                magiskPath) ||
            modules.Count > 0;

        var raw =
            rootResult.StdOut +
            rootResult.StdErr +
            Environment.NewLine +
            magiskVersion;

        return new RootMagiskInfo
        {
            RootAvailable = rootAvailable,

            MagiskDetected = magiskDetected,

            MagiskVersion =
                magiskVersion.Trim(),

            MagiskPath =
                magiskPath.Trim(),

            SuPath =
                suPath.Trim(),

            Zygisk =
                zygisk.Trim(),

            DenyList =
                denyList.Trim(),

            Modules = modules,

            Raw = raw.Trim()
        };
    }

    public async Task<List<string>> GetModulesAsync(
        string serial)
    {
        var result = await RunSuAsync(
            serial,
            "ls -1 /data/adb/modules 2>/dev/null");

        if (result.ExitCode != 0)
            return new List<string>();

        return result.StdOut
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    async Task<CommandResult> RunSuAsync(
        string serial,
        string command)
    {
        return await _r.RunAsync(
            _t.Adb!,
            [
                "-s",
                serial,
                "shell",
                "su",
                "-c",
                command
            ],
            TimeSpan.FromSeconds(15));
    }

    async Task<string> RunShellAsync(
        string serial,
        string command)
    {
        var result = await _r.RunAsync(
            _t.Adb!,
            [
                "-s",
                serial,
                "shell",
                command
            ],
            TimeSpan.FromSeconds(10));

        return (
            result.StdOut +
            result.StdErr
        ).Trim();
    }
}
