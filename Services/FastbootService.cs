using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FastbootService
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator _tools;

    public FastbootService(
        ProcessRunner runner,
        ToolLocator tools)
    {
        _runner = runner;
        _tools = tools;
    }

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var fastboot = _tools.FastbootPath;

        if (fastboot is null)
        {
            return [];
        }

        var result = await _runner.RunAsync(
            fastboot,
            ["devices"],
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
            var parts =
                line.Split(
                    '\t',
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                continue;
            }

            devices.Add(
                new DeviceInfo
                {
                    Transport = TransportType.Fastboot,
                    Serial = parts[0].Trim(),
                    State = "fastboot",
                    LastUpdatedUtc = DateTime.UtcNow
                });
        }

        foreach (var device in devices)
        {
            await EnrichAsync(
                device,
                cancellationToken);
        }

        return devices;
    }

    public async Task<string> GetVarAllAsync(
        string serial,
        CancellationToken cancellationToken = default)
    {
        var fastboot = _tools.FastbootPath;

        if (fastboot is null)
        {
            throw new InvalidOperationException(
                "未找到 fastboot.exe。");
        }

        var result = await _runner.RunAsync(
            fastboot,
            ["-s", serial, "getvar", "all"],
            TimeSpan.FromSeconds(30),
            cancellationToken);

        return result.StdOut +
               Environment.NewLine +
               result.StdErr;
    }

    public async Task<CommandResult> RebootAsync(
        string serial,
        CancellationToken cancellationToken = default)
    {
        var fastboot = _tools.FastbootPath;

        if (fastboot is null)
        {
            throw new InvalidOperationException(
                "未找到 fastboot.exe。");
        }

        return await _runner.RunAsync(
            fastboot,
            ["-s", serial, "reboot"],
            TimeSpan.FromSeconds(30),
            cancellationToken);
    }

    private async Task EnrichAsync(
        DeviceInfo device,
        CancellationToken cancellationToken)
    {
        device.RawFastbootInfo =
            await GetVarAllAsync(
                device.Serial,
                cancellationToken);

        var data = device.RawFastbootInfo;

        device.Product =
            ReadVar(data, "product");

        device.CurrentSlot =
            ReadVar(data, "current-slot");

        if (string.IsNullOrWhiteSpace(
                device.CurrentSlot))
        {
            device.CurrentSlot =
                ReadVar(data, "slot-current");
        }

        var unlocked =
            ReadVar(data, "unlocked");

        device.BootloaderUnlocked =
            unlocked.Equals(
                "yes",
                StringComparison.OrdinalIgnoreCase);

        var secure =
            ReadVar(data, "secure");

        device.AvbEnabled =
            secure.Equals(
                "yes",
                StringComparison.OrdinalIgnoreCase);

        device.AntiRollback =
            ReadVar(data, "anti");

        device.LastUpdatedUtc =
            DateTime.UtcNow;
    }

    private static string ReadVar(
        string text,
        string key)
    {
        foreach (var line in text.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            var marker =
                $"{key}:";

            var index =
                trimmed.IndexOf(
                    marker,
                    StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                continue;
            }

            return trimmed[
                (index + marker.Length)..]
                .Trim();
        }

        return string.Empty;
    }
}