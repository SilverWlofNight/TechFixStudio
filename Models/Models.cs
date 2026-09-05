using System.Text.Json.Serialization;

namespace TechFixStudio.Models;

public enum Transport
{
    Adb,
    Fastboot
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum TaskState
{
    Queued,
    Preflight,
    Hashing,
    AwaitingConfirmation,
    Running,
    Success,
    Failed,
    Cancelled
}

public sealed record DeviceInfo(
    string Serial,
    Transport Transport,
    string State,
    string Model,
    string Manufacturer,
    string Android,
    string Sdk,
    string Abi,
    string Slot,
    bool Root,
    bool Unlocked);

public sealed record Telemetry(
    double Battery,
    double Temperature,
    double StorageUsed,
    string Uptime,
    string Kernel,
    string Fingerprint);

public sealed record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    TimeSpan Duration,
    bool TimedOut);

public sealed record FastbootInfo(
    string Serial,
    string Product,
    string Variant,
    string Slot,
    string Unlocked,
    string Secure,
    string AntiRollback,
    string Raw);

public sealed record RomImage(
    string Partition,
    string Path,
    long Size,
    string Sha256,
    bool Critical);

public sealed record RomPackage(
    string Name,
    string Product,
    string Build,
    string Region,
    List<RomImage> Images,
    List<string> Scripts,
    string Source);

public sealed class FlashTask
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Serial { get; set; } = "";

    public string Partition { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public string Sha256 { get; set; } = "";

    public long Size { get; set; }

    public TaskState State { get; set; } =
        TaskState.Queued;

    public string Message { get; set; } =
        "Queued";

    public double Progress { get; set; }

    public DateTime Created { get; set; } =
        DateTime.Now;

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public string Product { get; set; } = "";

    public string Slot { get; set; } = "";

    public bool Critical { get; set; }

    public bool Confirmed { get; set; }
}

public sealed record HistoryEntry(
    DateTime Time,
    string Action,
    string Target,
    string Result,
    string Risk,
    string Details);

//
// Compatibility model used by the 2.0 history service.
//

public sealed class HistoryRecord
{
    public DateTime TimestampUtc { get; set; }

    public string Action { get; set; } = "";

    public string Device { get; set; } = "";

    public string Result { get; set; } = "";

    public string Details { get; set; } = "";
}

//
// Compatibility model used by the 2.0 ROM catalog.
//

public sealed class RomEntry
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string Name { get; set; } = "";

    public string Product { get; set; } = "";

    public string Version { get; set; } = "";

    public string Region { get; set; } = "";

    public string Codename { get; set; } = "";

    public string Url { get; set; } = "";

    public string Sha256 { get; set; } = "";

    public string Notes { get; set; } = "";

    public DateTime CreatedUtc { get; set; } =
        DateTime.UtcNow;
}

public sealed record RomResource(
    string Name,
    string Product,
    string Version,
    string Region,
    string Codename,
    string Url,
    string Sha256,
    string Notes);

//
// Audit record.
//

public sealed class AuditRecord
{
    public DateTime TimestampUtc { get; set; }

    public string User { get; set; } = "";

    public string Action { get; set; } = "";

    public string Target { get; set; } = "";

    public RiskLevel Risk { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; } = "";
}

//
// Android diagnostics.
//

public sealed class AndroidDiagnostics
{
    public string Serial { get; init; } = "";

    public string Brand { get; init; } = "";

    public string Manufacturer { get; init; } = "";

    public string Model { get; init; } = "";

    public string Device { get; init; } = "";

    public string Product { get; init; } = "";

    public string AndroidVersion { get; init; } = "";

    public string Sdk { get; init; } = "";

    public string SecurityPatch { get; init; } = "";

    public string BuildId { get; init; } = "";

    public string Fingerprint { get; init; } = "";

    public string Abi { get; init; } = "";

    public string Abi64 { get; init; } = "";

    public string Kernel { get; init; } = "";

    public string Slot { get; init; } = "";

    public string VerifiedBootState { get; init; } = "";

    public string VbmetaDeviceState { get; init; } = "";

    public string AvbVersion { get; init; } = "";

    public string BootReason { get; init; } = "";

    public AndroidBatteryInfo Battery { get; init; } =
        new(0, 0, 0, "", "");

    public AndroidMemoryInfo Memory { get; init; } =
        new(0, 0, 0, 0);

    public AndroidStorageInfo Storage { get; init; } =
        new(0, 0, 0, 0);

    public AndroidCpuInfo Cpu { get; init; } =
        new(0, "");

    public string Uptime { get; init; } = "";
}

public sealed record AndroidBatteryInfo(
    double Level,
    double Temperature,
    double VoltageMv,
    string Health,
    string Status);

public sealed record AndroidMemoryInfo(
    long TotalKb,
    long AvailableKb,
    long UsedKb,
    double UsedPercent);

public sealed record AndroidStorageInfo(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    double UsedPercent);

public sealed record AndroidCpuInfo(
    int CoreCount,
    string Hardware);

//
// Root / Magisk compatibility model.
//

public sealed class RootMagiskInfo
{
    public bool RootAvailable { get; init; }

    public bool MagiskDetected { get; init; }

    public string MagiskVersion { get; init; } = "";

    public string MagiskPath { get; init; } = "";

    public string SuPath { get; init; } = "";

    public string Zygisk { get; init; } = "";

    public string DenyList { get; init; } = "";

    public List<string> Modules { get; init; } = new();

    public string Raw { get; init; } = "";
}

//
// Flash plan.
//

public sealed class FlashPlan
{
    public string Serial { get; set; } = "";

    public string Product { get; set; } = "";

    public string CurrentSlot { get; set; } = "";

    public string Unlocked { get; set; } = "";

    public string Secure { get; set; } = "";

    public string AntiRollback { get; set; } = "";

    public bool ProductMatch { get; set; }

    public bool BootloaderUnlocked { get; set; }

    public bool SafeToProceed { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<FlashPlanItem> Items { get; set; } = new();
}

public sealed class FlashPlanItem
{
    public string Partition { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public string Sha256 { get; set; } = "";

    public long Size { get; set; }

    public bool Critical { get; set; }

    public bool Allowed { get; set; }

    public string Reason { get; set; } = "";
}

//
// Magisk.
//

public sealed class MagiskInfo
{
    public bool RootDetected { get; set; }

    public bool MagiskDetected { get; set; }

    public string Version { get; set; } = "";

    public string Zygisk { get; set; } = "";

    public string DenyList { get; set; } = "";

    public string InstallPath { get; set; } = "";

    public string Raw { get; set; } = "";
}
