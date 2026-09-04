using System.Text.Json;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

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
        string message)
    {
        var record = new AuditRecord
        {
            TimestampUtc = DateTime.UtcNow,
            User = Environment.UserName,
            Action = action,
            Target = target,
            Risk = risk,
            Success = success,
            Message = message
        };

        var line =
            JsonSerializer.Serialize(record, JsonOptions);

        await _lock.WaitAsync();

        try
        {
            await File.AppendAllTextAsync(
                AppPaths.AuditLogFile,
                line + Environment.NewLine);
        }
        finally
        {
            _lock.Release();
        }
    }
}