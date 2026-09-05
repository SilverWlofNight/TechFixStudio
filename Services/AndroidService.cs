using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class AndroidService
{
    private readonly ToolLocator _tools = new();
    private readonly ProcessRunner _runner = new();

    public async Task<List<DeviceInfo>> ScanAsync(
        CancellationToken ct = default)
    {
        var result = new List<DeviceInfo>();

        if (_tools.Adb is null)
        {
            return result;
        }

        var r = await _runner.RunAsync(
            _tools.Adb,
            new[] { "devices", "-l" },
            TimeSpan.FromSeconds(15),
            ct);

        foreach (var line in r.StdOut.Split(
                     new[] { '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                continue;
            }

            if (parts[0].Equals(
                    "List",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!parts[1].Equals(
                    "device",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var serial = parts[0];

            var model = parts
                .FirstOrDefault(
                    x => x.StartsWith(
                        "model:",
                        StringComparison.OrdinalIgnoreCase))
                ?.Split(':', 2)
                .LastOrDefault()
                ?.Replace('_', ' ')
                ?? "Android Device";

            var device = new DeviceInfo(
                serial,
                Transport.Adb,
                parts[1],
                model,
                "—",
                "—",
                "—",
                "—",
                "—",
                false,
                false);

            result.Add(
                await EnrichAsync(device, ct));
        }

        return result;
    }

    public async Task<DeviceInfo> EnrichAsync(
        DeviceInfo device,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null)
        {
            return device;
        }

        try
        {
            var model = await PropAsync(
                device.Serial,
                "ro.product.model",
                ct);

            var manufacturer = await PropAsync(
                device.Serial,
                "ro.product.manufacturer",
                ct);

            var android = await PropAsync(
                device.Serial,
                "ro.build.version.release",
                ct);

            var sdk = await PropAsync(
                device.Serial,
                "ro.build.version.sdk",
                ct);

            var abi = await PropAsync(
                device.Serial,
                "ro.product.cpu.abi",
                ct);

            var slot = await PropAsync(
                device.Serial,
                "ro.boot.slot_suffix",
                ct);

            if (string.IsNullOrWhiteSpace(slot))
            {
                slot = await PropAsync(
                    device.Serial,
                    "ro.boot.slot",
                    ct);
            }

            var unlocked =
                await PropAsync(
                    device.Serial,
                    "ro.boot.verifiedbootstate",
                    ct);

            var rootResult =
                await _runner.RunAsync(
                    _tools.Adb,
                    new[]
                    {
                        "-s",
                        device.Serial,
                        "shell",
                        "su",
                        "-c",
                        "id"
                    },
                    TimeSpan.FromSeconds(5),
                    ct);

            var root =
                rootResult.ExitCode == 0 &&
                rootResult.StdOut.Contains(
                    "uid=0",
                    StringComparison.OrdinalIgnoreCase);

            return device with
            {
                Model = string.IsNullOrWhiteSpace(model)
                    ? device.Model
                    : model,

                Manufacturer = manufacturer,

                Android = android,

                Sdk = sdk,

                Abi = abi,

                Slot = slot,

                Root = root,

                Unlocked =
                    unlocked.Equals(
                        "orange",
                        StringComparison.OrdinalIgnoreCase)
                    || unlocked.Equals(
                        "yellow",
                        StringComparison.OrdinalIgnoreCase)
            };
        }
        catch
        {
            return device;
        }
    }

    public async Task<string> PropAsync(
        string serial,
        string key,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null)
        {
            return "";
        }

        var r = await _runner.RunAsync(
            _tools.Adb,
            new[]
            {
                "-s",
                serial,
                "shell",
                "getprop",
                key
            },
            TimeSpan.FromSeconds(8),
            ct);

        return r.StdOut.Trim();
    }

    public async Task<Telemetry?> TelemetryAsync(
        DeviceInfo device,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null)
        {
            return null;
        }

        try
        {
            var battery =
                await _runner.RunAsync(
                    _tools.Adb,
                    new[]
                    {
                        "-s",
                        device.Serial,
                        "shell",
                        "dumpsys",
                        "battery"
                    },
                    TimeSpan.FromSeconds(8),
                    ct);

            var level =
                ParseDouble(
                    battery.StdOut,
                    "level");

            var temp =
                ParseDouble(
                    battery.StdOut,
                    "temperature") / 10.0;

            var uptime =
                await _runner.RunAsync(
                    _tools.Adb,
                    new[]
                    {
                        "-s",
                        device.Serial,
                        "shell",
                        "cat",
                        "/proc/uptime"
                    },
                    TimeSpan.FromSeconds(5),
                    ct);

            var kernel =
                await _runner.RunAsync(
                    _tools.Adb,
                    new[]
                    {
                        "-s",
                        device.Serial,
                        "shell",
                        "uname",
                        "-r"
                    },
                    TimeSpan.FromSeconds(5),
                    ct);

            var fingerprint =
                await PropAsync(
                    device.Serial,
                    "ro.build.fingerprint",
                    ct);

            return new Telemetry(
                level,
                temp,
                0,
                uptime.StdOut.Trim(),
                kernel.StdOut.Trim(),
                fingerprint);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> LogcatAsync(
        string serial,
        int lines = 300,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null)
        {
            return "";
        }

        var result =
            await _runner.RunAsync(
                _tools.Adb,
                new[]
                {
                    "-s",
                    serial,
                    "logcat",
                    "-d",
                    "-t",
                    lines.ToString()
                },
                TimeSpan.FromSeconds(30),
                ct);

        return result.StdOut +
               result.StdErr;
    }

    public async Task<CommandResult> RebootAsync(
        string serial,
        string target,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null)
        {
            return new CommandResult(
                -10,
                "",
                "adb executable not found.",
                TimeSpan.Zero,
                false);
        }

        var args = new List<string>
        {
            "-s",
            serial,
            "reboot"
        };

        if (!string.IsNullOrWhiteSpace(target))
        {
            args.Add(target);
        }

        return await _runner.RunAsync(
            _tools.Adb,
            args,
            TimeSpan.FromSeconds(30),
            ct);
    }

    private static double ParseDouble(
        string text,
        string key)
    {
        var line = text
            .Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(
                x => x.TrimStart()
                    .StartsWith(
                        key + ":",
                        StringComparison.OrdinalIgnoreCase));

        if (line is null)
        {
            return 0;
        }

        var value = line
            .Split(':', 2)
            .Last()
            .Trim();

        return double.TryParse(
            value,
            out var result)
            ? result
            : 0;
    }
}

