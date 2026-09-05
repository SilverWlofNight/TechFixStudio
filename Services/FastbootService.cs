using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FastbootService
{
    private readonly ToolLocator _tools = new();
    private readonly ProcessRunner _runner = new();

    public async Task<List<string>> DevicesAsync(
        CancellationToken ct = default)
    {
        var result = new List<string>();

        if (_tools.Fastboot is null)
        {
            return result;
        }

        var r = await _runner.RunAsync(
            _tools.Fastboot,
            new[] { "devices" },
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

            if (parts.Length > 0)
            {
                result.Add(parts[0]);
            }
        }

        return result;
    }

    public async Task<FastbootInfo?> InspectAsync(
        string serial,
        CancellationToken ct = default)
    {
        if (_tools.Fastboot is null ||
            string.IsNullOrWhiteSpace(serial))
        {
            return null;
        }

        var r = await _runner.RunAsync(
            _tools.Fastboot,
            new[]
            {
                "-s",
                serial,
                "getvar",
                "all"
            },
            TimeSpan.FromSeconds(20),
            ct);

        var raw = (r.StdOut +
                   Environment.NewLine +
                   r.StdErr).Trim();

        var values = FastbootParser.Parse(raw);

        var product = FastbootParser.Get(
            values,
            "product",
            "product-name");

        var variant = FastbootParser.Get(
            values,
            "variant");

        var slot = FastbootParser.Get(
            values,
            "current-slot",
            "slot");

        var unlocked = FastbootParser.Get(
            values,
            "unlocked",
            "secure-state");

        var secure = FastbootParser.Get(
            values,
            "secure");

        var antiRollback = FastbootParser.Get(
            values,
            "anti",
            "anti-rollback",
            "anti-rollback-version");

        return new FastbootInfo(
            serial,
            product,
            variant,
            slot,
            unlocked,
            secure,
            antiRollback,
            raw);
    }

    public async Task<CommandResult> FlashAsync(
        string serial,
        string partition,
        string imagePath,
        CancellationToken ct = default,
        IProgress<string>? output = null)
    {
        if (_tools.Fastboot is null)
        {
            return new CommandResult(
                -10,
                "",
                "fastboot executable not found.",
                TimeSpan.Zero,
                false);
        }

        return await _runner.RunAsync(
            _tools.Fastboot,
            new[]
            {
                "-s",
                serial,
                "flash",
                partition,
                imagePath
            },
            TimeSpan.FromMinutes(10),
            ct,
            output);
    }

    public async Task<CommandResult> RebootAsync(
        string serial,
        string target,
        CancellationToken ct = default)
    {
        if (_tools.Fastboot is null)
        {
            return new CommandResult(
                -10,
                "",
                "fastboot executable not found.",
                TimeSpan.Zero,
                false);
        }

        return await _runner.RunAsync(
            _tools.Fastboot,
            new[]
            {
                "-s",
                serial,
                "reboot",
                target
            },
            TimeSpan.FromSeconds(30),
            ct);
    }

    public async Task<CommandResult> GetVarAsync(
        string serial,
        string variable,
        CancellationToken ct = default)
    {
        if (_tools.Fastboot is null)
        {
            return new CommandResult(
                -10,
                "",
                "fastboot executable not found.",
                TimeSpan.Zero,
                false);
        }

        return await _runner.RunAsync(
            _tools.Fastboot,
            new[]
            {
                "-s",
                serial,
                "getvar",
                variable
            },
            TimeSpan.FromSeconds(20),
            ct);
    }
}
