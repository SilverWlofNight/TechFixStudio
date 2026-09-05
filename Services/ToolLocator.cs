using TechFixStudio.Infrastructure;

namespace TechFixStudio.Services;

public sealed class ToolLocator
{
    public string? AdbPath =>
        FindTool(
            "adb.exe",
            "adb");

    public string? FastbootPath =>
        FindTool(
            "fastboot.exe",
            "fastboot");

    // 兼容现有服务
    public string? Adb =>
        AdbPath;

    // 兼容现有服务
    public string? Fastboot =>
        FastbootPath;

    private static string? FindTool(
        params string[] names)
    {
        // 1. 优先寻找程序自带 Platform-Tools
        foreach (var name in names)
        {
            try
            {
                var bundled =
                    Path.Combine(
                        AppPaths.PlatformToolsDirectory,
                        name);

                if (File.Exists(bundled))
                {
                    return bundled;
                }
            }
            catch
            {
                // Ignore invalid path.
            }
        }

        // 2. 从 PATH 查找
        var path =
            Environment.GetEnvironmentVariable(
                "PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in names)
            {
                try
                {
                    var candidate =
                        Path.Combine(
                            directory.Trim(),
                            name);

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Ignore invalid PATH entries.
                }
            }
        }

        return null;
    }
}
