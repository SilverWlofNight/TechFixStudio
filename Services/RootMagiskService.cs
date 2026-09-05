using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RootMagiskService
{
    private readonly MagiskService _magisk;

    public RootMagiskService()
        : this(new MagiskService())
    {
    }

    public RootMagiskService(
        MagiskService magisk)
    {
        _magisk = magisk;
    }

    public async Task<RootMagiskInfo> InspectAsync(
        string serial,
        CancellationToken ct = default)
    {
        var result =
            new RootMagiskInfo
            {
                DeviceId =
                    serial ?? string.Empty
            };

        if (string.IsNullOrWhiteSpace(serial))
        {
            result.Message =
                "No Android device selected.";

            return result;
        }

        try
        {
            var info =
                await _magisk.InspectAsync(
                    serial,
                    ct);

            if (info is null)
            {
                result.Message =
                    "Magisk inspection failed.";

                return result;
            }

            result.RootDetected =
                info.RootDetected;

            result.MagiskDetected =
                info.MagiskDetected;

            result.Version =
                info.Version;

            result.Zygisk =
                info.Zygisk;

            result.DenyList =
                info.DenyList;

            result.InstallPath =
                info.InstallPath;

            result.Raw =
                info.Raw;

            result.Message =
                "Magisk inspection complete.";

            return result;
        }
        catch (Exception ex)
        {
            result.Message =
                ex.Message;

            return result;
        }
    }

    public Task<RootMagiskInfo> InspectRootMagiskAsync(
        string serial,
        CancellationToken ct = default)
    {
        return InspectAsync(
            serial,
            ct);
    }
}
