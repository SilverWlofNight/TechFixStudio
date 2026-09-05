using System.IO;
using System.Text.RegularExpressions;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class AndroidService
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator _tools;

    public AndroidService(
        ProcessRunner runner,
        ToolLocator tools)
    {
        _runner = runner;
        _tools = tools;
    }

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var adb = _tools.AdbPath;

        if (adb is null)
        {
            return [];
        }

        var result = await _runner.RunAsync(
            adb,
            ["devices", "-l"],
            TimeSpan.FromSeconds(15),
            cancellationToken);

        if (!result.Success)
        {
            return [];
        }

        var devices = new List<DeviceInfo>();

        using var reader =
            new StringReader(result.StdOut);

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith("List of devices"))
            {
                continue;
            }

            var parts =
                Regex.Split(
                    line,
                    @"\s+");

            if (parts.Length < 2)
            {
                continue;
            }

            var serial = parts[0];
            var state = parts[1];

            var device = new DeviceInfo
            {
                Transport = TransportType.Adb,
                Serial = serial,
                State = state
            };

            foreach (var part in parts.Skip(2))
            {
                if (part.StartsWith("model:"))
                {
                    device.Model =
                        part["model:".Length..];
                }
                else if (part.StartsWith("product:"))
                {
                    device.Product =
                        part["product:".Length..];
                }
            }

            devices.Add(device);
        }

        foreach (var device in devices)
        {
            await EnrichAsync(
                device,
                cancellationToken);
        }

        return devices;
    }

    private async Task EnrichAsync(
        DeviceInfo device,
        CancellationToken cancellationToken)
    {
        device.Manufacturer =
            await GetPropertyAsync(
                device.Serial,
                "ro.product.manufacturer",
                cancellationToken);

        device.Model =
            string.IsNullOrWhiteSpace(device.Model)
                ? await GetPropertyAsync(
                    device.Serial,
                    "ro.product.model",
                    cancellationToken)
                : device.Model;

        device.AndroidVersion =
            await GetPropertyAsync(
                device.Serial,
                "ro.build.version.release",
                cancellationToken);

        device.SdkVersion =
            await GetPropertyAsync(
                device.Serial,
                "ro.build.version.sdk",
                cancellationToken);

        device.CpuAbi =
            await GetPropertyAsync(
                device.Serial,
                "ro.product.cpu.abi",
                cancellationToken);

        var slot =
            await GetPropertyAsync(
                device.Serial,
                "ro.boot.slot_suffix",
                cancellationToken);

        device.CurrentSlot =
            slot.Trim().TrimStart('_');

        if (string.IsNullOrWhiteSpace(device.CurrentSlot))
        {
            device.CurrentSlot = "unknown";
        }

        var rootResult = await _runner.RunAsync(
            _tools.AdbPath!,
            ["-s", device.Serial, "shell", "su", "-c", "id"],
            TimeSpan.FromSeconds(10),
            cancellationToken);

        device.RootDetected =
            rootResult.Success &&
            rootResult.StdOut.Contains(
                "uid=0",
                StringComparison.OrdinalIgnoreCase);

        device.LastUpdatedUtc =
            DateTime.UtcNow;
    }

    private async Task<string> GetPropertyAsync(
        string serial,
        string property,
        CancellationToken cancellationToken)
    {
        var adb = _tools.AdbPath;

        if (adb is null)
        {
            return string.Empty;
        }

        var result = await _runner.RunAsync(
            adb,
            [
                "-s",
                serial,
                "shell",
                "getprop",
                property
            ],
            TimeSpan.FromSeconds(10),
            cancellationToken);

        return result.Success
            ? result.StdOut.Trim()
            : string.Empty;
    }
}