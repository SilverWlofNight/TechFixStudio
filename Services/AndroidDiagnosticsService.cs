using System.Globalization;
using System.Text.RegularExpressions;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class AndroidDiagnosticsService
{
    private readonly AndroidService _android = new();
    private readonly ToolLocator _tools = new();
    private readonly ProcessRunner _runner = new();

    public async Task<AndroidDiagnostics?> CollectAsync(
        DeviceInfo device,
        CancellationToken ct = default)
    {
        if (_tools.Adb is null ||
            device.Transport != Transport.Adb)
        {
            return null;
        }

        try
        {
            var brand =
                await Prop(device.Serial, "ro.product.brand", ct);

            var manufacturer =
                await Prop(device.Serial, "ro.product.manufacturer", ct);

            var model =
                await Prop(device.Serial, "ro.product.model", ct);

            var deviceName =
                await Prop(device.Serial, "ro.product.device", ct);

            var product =
                await Prop(device.Serial, "ro.product.name", ct);

            var android =
                await Prop(device.Serial, "ro.build.version.release", ct);

            var sdk =
                await Prop(device.Serial, "ro.build.version.sdk", ct);

            var securityPatch =
                await Prop(device.Serial, "ro.build.version.security_patch", ct);

            var buildId =
                await Prop(device.Serial, "ro.build.id", ct);

            var fingerprint =
                await Prop(device.Serial, "ro.build.fingerprint", ct);

            var abi =
                await Prop(device.Serial, "ro.product.cpu.abi", ct);

            var abi64 =
                await Prop(device.Serial, "ro.product.cpu.abilist64", ct);

            var kernel =
                await SimpleShell(
                    device.Serial,
                    "uname -r",
                    ct);

            var slot =
                await Prop(device.Serial, "ro.boot.slot_suffix", ct);

            var verifiedBoot =
                await Prop(
                    device.Serial,
                    "ro.boot.verifiedbootstate",
                    ct);

            var vbmetaState =
                await Prop(
                    device.Serial,
                    "ro.boot.vbmeta.device_state",
                    ct);

            var avb =
                await Prop(
                    device.Serial,
                    "ro.boot.avb_version",
                    ct);

            var bootReason =
                await Prop(
                    device.Serial,
                    "ro.boot.bootreason",
                    ct);

            var battery =
                await CollectBattery(
                    device.Serial,
                    ct);

            var memory =
                await CollectMemory(
                    device.Serial,
                    ct);

            var storage =
                await CollectStorage(
                    device.Serial,
                    ct);

            var cpu =
                await CollectCpu(
                    device.Serial,
                    ct);

            var uptime =
                await SimpleShell(
                    device.Serial,
                    "cat /proc/uptime",
                    ct);

            return new AndroidDiagnostics
            {
                Serial = device.Serial,
                Brand = brand,
                Manufacturer = manufacturer,
                Model = model,
                Device = deviceName,
                Product = product,
                AndroidVersion = android,
                Sdk = sdk,
                SecurityPatch = securityPatch,
                BuildId = buildId,
                Fingerprint = fingerprint,
                Abi = abi,
                Abi64 = abi64,
                Kernel = kernel,
                Slot = slot,
                VerifiedBootState = verifiedBoot,
                VbmetaDeviceState = vbmetaState,
                AvbVersion = avb,
                BootReason = bootReason,
                Battery = battery,
                Memory = memory,
                Storage = storage,
                Cpu = cpu,
                Uptime = uptime
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> Prop(
        string serial,
        string key,
        CancellationToken ct)
    {
        return await _android.PropAsync(
            serial,
            key,
            ct);
    }

    private async Task<string> SimpleShell(
        string serial,
        string command,
        CancellationToken ct)
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
                    "shell",
                    "sh",
                    "-c",
                    command
                },
                TimeSpan.FromSeconds(10),
                ct);

        return result.StdOut.Trim();
    }

    private async Task<AndroidBatteryInfo> CollectBattery(
        string serial,
        CancellationToken ct)
    {
        var raw =
            await SimpleShell(
                serial,
                "dumpsys battery",
                ct);

        var level =
            ParseDouble(raw, "level");

        var temperature =
            ParseDouble(raw, "temperature") / 10.0;

        var voltage =
            ParseDouble(raw, "voltage");

        var health =
            ParseString(raw, "health");

        var status =
            ParseString(raw, "status");

        return new AndroidBatteryInfo(
            level,
            temperature,
            voltage,
            health,
            status);
    }

    private async Task<AndroidMemoryInfo> CollectMemory(
        string serial,
        CancellationToken ct)
    {
        var raw =
            await SimpleShell(
                serial,
                "cat /proc/meminfo",
                ct);

        var total =
            ParseLong(raw, "MemTotal");

        var available =
            ParseLong(raw, "MemAvailable");

        if (available <= 0)
        {
            available =
                ParseLong(raw, "MemFree");
        }

        var used =
            Math.Max(
                0,
                total - available);

        var percent =
            total <= 0
                ? 0
                : used * 100.0 / total;

        return new AndroidMemoryInfo(
            total,
            available,
            used,
            percent);
    }

    private async Task<AndroidStorageInfo> CollectStorage(
        string serial,
        CancellationToken ct)
    {
        var raw =
            await SimpleShell(
                serial,
                "df -k /data",
                ct);

        var line =
            raw.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault(
                    x => x.Contains("/data"));

        if (line is null)
        {
            return new AndroidStorageInfo(
                0,
                0,
                0,
                0);
        }

        var parts =
            Regex.Split(
                line.Trim(),
                @"\s+");

        if (parts.Length < 5)
        {
            return new AndroidStorageInfo(
                0,
                0,
                0,
                0);
        }

        if (!long.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var totalKb))
        {
            return new AndroidStorageInfo(
                0,
                0,
                0,
                0);
        }

        long usedKb =
            long.TryParse(
                parts[2],
                out var u)
                ? u
                : 0;

        long availableKb =
            long.TryParse(
                parts[3],
                out var a)
                ? a
                : 0;

        var totalBytes =
            totalKb * 1024L;

        var usedBytes =
            usedKb * 1024L;

        var availableBytes =
            availableKb * 1024L;

        var percent =
            totalBytes <= 0
                ? 0
                : usedBytes * 100.0 / totalBytes;

        return new AndroidStorageInfo(
            totalBytes,
            usedBytes,
            availableBytes,
            percent);
    }

    private async Task<AndroidCpuInfo> CollectCpu(
        string serial,
        CancellationToken ct)
    {
        var raw =
            await SimpleShell(
                serial,
                "cat /proc/cpuinfo",
                ct);

        var coreCount =
            raw.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Count(
                    x => x.StartsWith(
                        "processor",
                        StringComparison.OrdinalIgnoreCase));

        var hardware =
            ParseString(
                raw,
                "Hardware");

        return new AndroidCpuInfo(
            coreCount,
            hardware);
    }

    private static string ParseString(
        string text,
        string key)
    {
        var line =
            text.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(
                    x => x.TrimStart()
                        .StartsWith(
                            key + ":",
                            StringComparison.OrdinalIgnoreCase));

        if (line is null)
        {
            return "";
        }

        return line
            .Split(':', 2)
            .Last()
            .Trim();
    }

    private static double ParseDouble(
        string text,
        string key)
    {
        var value =
            ParseString(text, key);

        return double.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }

    private static long ParseLong(
        string text,
        string key)
    {
        var value =
            ParseString(text, key)
                .Replace("kB", "")
                .Trim();

        return long.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }
}
