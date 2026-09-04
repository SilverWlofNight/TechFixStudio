using TechFixStudio.Infrastructure;

namespace TechFixStudio.Services;

public sealed class ToolLocator
{
    public string? AdbPath => FindTool("adb.exe", "adb");

    public string? FastbootPath => FindTool("fastboot.exe", "fastboot");

    private static string? FindTool(params string[] names)
    {
        foreach (var name in names)
        {
            var bundled = Path.Combine(
                AppPaths.PlatformToolsDirectory,
                name);

            if (File.Exists(bundled))
            {
                return bundled;
            }
        }

        var path = Environment.GetEnvironmentVariable("PATH");

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
                    var candidate = Path.Combine(
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