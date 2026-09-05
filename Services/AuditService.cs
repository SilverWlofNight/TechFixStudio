using System.Text.Json;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class AuditService
{
    private readonly SemaphoreSlim _lock =
        new(1, 1);

    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
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
        var record =
            new AuditRecord
            {
                TimestampUtc =
                    DateTime.UtcNow,

                User =
                    Environment.UserName,

                Action =
                    action ?? string.Empty,

                Target =
                    target ?? string.Empty,

                Risk =
                    risk,

                Success =
                    success,

                Message =
                    message ?? string.Empty
            };

        var line =
            JsonSerializer.Serialize(
                record,
                JsonOptions);

        await _lock.WaitAsync();

        try
        {
            AppPaths.Ensure();

            await File.AppendAllTextAsync(
                AppPaths.AuditLogFile,
                line +
                Environment.NewLine);
        }
        finally
        {
            _lock.Release();
        }
    }

    // 兼容旧版 4 参数调用：
    // action, target, message, risk
    public Task WriteAsync(
        string action,
        string target,
        string message,
        RiskLevel risk)
    {
        var success =
            !string.Equals(
                message,
                "blocked",
                StringComparison.OrdinalIgnoreCase);

        return WriteAsync(
            action,
            target,
            risk,
            success,
            message);
    }

    // 兼容 action,target,risk,message
    public Task WriteAsync(
        string action,
        string target,
        RiskLevel risk,
        string message)
    {
        var success =
            !string.Equals(
                message,
                "blocked",
                StringComparison.OrdinalIgnoreCase);

        return WriteAsync(
            action,
            target,
            risk,
            success,
            message);
    }
}
