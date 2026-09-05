using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RiskEngine
{
    private static readonly string[] CriticalKeywords =
    {
        "erase",
        "format",
        "userdata",
        "factory-reset",
        "partition",
        "gpt",
        "raw",
        "dd if=",
        "mkfs",
        "wipe"
    };

    private static readonly string[] HighKeywords =
    {
        "flash",
        "fastboot",
        "reboot bootloader",
        "reboot recovery",
        "bootloader",
        "unlock",
        "lock",
        "vbmeta",
        "super",
        "system",
        "vendor"
    };

    private static readonly string[] MediumKeywords =
    {
        "push",
        "pull",
        "install",
        "uninstall",
        "shell",
        "dism",
        "sfc",
        "ssh"
    };

    public RiskLevel Assess(
        string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return RiskLevel.Low;
        }

        var text =
            command.ToLowerInvariant();

        if (CriticalKeywords.Any(
                text.Contains))
        {
            return RiskLevel.Critical;
        }

        if (HighKeywords.Any(
                text.Contains))
        {
            return RiskLevel.High;
        }

        if (MediumKeywords.Any(
                text.Contains))
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    public bool IsBlocked(
        string command)
    {
        return Assess(command) >=
               RiskLevel.High;
    }
}
