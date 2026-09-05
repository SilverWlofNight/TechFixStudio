using System.IO;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RiskEngine
{
    private static readonly string[] CriticalPatterns =
    [
        "fastboot flash",
        "fastboot erase",
        "fastboot format",
        "fastboot -w",
        "diskpart",
        "format ",
        "dd if=",
        "rm -rf",
        "del /s"
    ];

    private static readonly string[] HighPatterns =
    [
        "adb reboot",
        "adb shell",
        "fastboot reboot",
        "bootrec",
        "bcdedit"
    ];

    private static readonly string[] MediumPatterns =
    [
        "adb install",
        "adb push",
        "adb pull",
        "sfc",
        "dism"
    ];

    public RiskLevel Assess(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return RiskLevel.Low;
        }

        var normalized =
            command.Trim().ToLowerInvariant();

        if (CriticalPatterns.Any(
                normalized.Contains))
        {
            return RiskLevel.Critical;
        }

        if (HighPatterns.Any(
                normalized.Contains))
        {
            return RiskLevel.High;
        }

        if (MediumPatterns.Any(
                normalized.Contains))
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    public bool IsDestructive(string command)
    {
        var risk = Assess(command);

        return risk is
            RiskLevel.High or
            RiskLevel.Critical;
    }
}