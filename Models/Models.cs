namespace TechFixStudio.Models;

public enum TransportType
{
    Unknown,
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

public enum FlashTaskState
{
    Queued,
    Preflight,
    Hashing,
    WaitingForConfirmation,
    Flashing,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class DeviceInfo
{
    public TransportType Transport { get; set; }

    public string Serial { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string AndroidVersion { get; set; } = string.Empty;

    public string SdkVersion { get; set; } = string.Empty;

    public string CpuAbi { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public string CurrentSlot { get; set; } = string.Empty;

    public bool BootloaderUnlocked { get; set; }

    public bool RootDetected { get; set; }

    public bool AvbEnabled { get; set; }

    public string AntiRollback { get; set; } = string.Empty;

    public string RawFastbootInfo { get; set; } = string.Empty;

    public DateTime LastUpdatedUtc { get; set; } =
        DateTime.UtcNow;
}

public sealed class CommandResult
{
    public int ExitCode { get; init; }

    public string StdOut { get; init; } = string.Empty;

    public string StdErr { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }

    public bool TimedOut { get; init; }

    public bool Success =>
        !TimedOut && ExitCode == 0;
}

public sealed class FlashTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Serial { get; set; } = string.Empty;

    public string Partition { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string ExpectedSha256 { get; set; } = string.Empty;

    public string ActualSha256 { get; set; } = string.Empty;

    public FlashTaskState State { get; set; } =
        FlashTaskState.Queued;

    public double Progress { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime? StartedUtc { get; set; }

    public DateTime? FinishedUtc { get; set; }
}

public sealed class RomEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Manufacturer { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public string Codename { get; set; } = string.Empty;

    public string AndroidVersion { get; set; } = string.Empty;

    public string BuildId { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public bool Official { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public sealed class AuditRecord
{
    public DateTime TimestampUtc { get; set; }

    public string User { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public RiskLevel Risk { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class HistoryRecord
{
    public DateTime TimestampUtc { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Device { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}