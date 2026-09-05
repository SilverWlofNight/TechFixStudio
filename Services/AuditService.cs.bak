using System.Text.Json;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

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

public sealed class AuditService
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public async Task WriteAsync(
        string action,
        string target,
        RiskLevel risk,
        bool success,
        string message,
        CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureDirectories();

        var record = new AuditRecord
        {
            TimestampUtc = DateTime.UtcNow,
            User = Environment.UserName,
            Action = action ?? "",
            Target = target ?? "",
            Risk = risk,
            Success = success,
            Message = message ?? ""
        };

        var line = JsonSerializer.Serialize(
            record,
            JsonOptions);

        await _lock.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(
                AppPaths.AuditLogFile,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
}
