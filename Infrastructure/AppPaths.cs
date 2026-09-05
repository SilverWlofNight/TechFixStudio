using System;
using System.IO;

namespace TechFixStudio.Infrastructure;

public static class AppPaths
{
    public static string RootDirectory =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "TechFixStudio");

    public static string LogsDirectory =>
        Path.Combine(RootDirectory, "logs");

    public static string DataDirectory =>
        Path.Combine(RootDirectory, "data");

    public static string CacheDirectory =>
        Path.Combine(RootDirectory, "cache");

    public static string ModulesDirectory =>
        Path.Combine(RootDirectory, "modules");

    public static string ToolsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "tools");

    public static string PlatformToolsDirectory =>
        Path.Combine(ToolsDirectory, "platform-tools");

    public static string AuditLogFile =>
        Path.Combine(LogsDirectory, "audit.jsonl");

    public static string HistoryFile =>
        Path.Combine(DataDirectory, "history.json");

    public static string FlashQueueFile =>
        Path.Combine(DataDirectory, "flash-queue.json");

    public static string RomCatalogFile =>
        Path.Combine(DataDirectory, "rom-catalog.json");

    // Compatibility alias used by the 2.0 FlashQueueService.
    public static string Queue =>
        FlashQueueFile;

    // Compatibility entry point used by services during startup.
    public static void Ensure()
    {
        EnsureDirectories();
    }

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(ModulesDirectory);
        Directory.CreateDirectory(ToolsDirectory);
        Directory.CreateDirectory(PlatformToolsDirectory);
    }
}
