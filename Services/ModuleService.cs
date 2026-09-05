using System.IO;
using System.Text.Json;
using TechFixStudio.Infrastructure;

namespace TechFixStudio.Services;

public sealed class ModuleManifest
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string EntryPoint { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}

public sealed class ModuleService
{
    public async Task<IReadOnlyList<ModuleManifest>>
        DiscoverAsync()
    {
        var result =
            new List<ModuleManifest>();

        if (!Directory.Exists(
                AppPaths.ModulesDirectory))
        {
            return result;
        }

        foreach (var directory in
                 Directory.GetDirectories(
                     AppPaths.ModulesDirectory))
        {
            var manifestPath =
                Path.Combine(
                    directory,
                    "module.json");

            if (!File.Exists(manifestPath))
            {
                continue;
            }

            try
            {
                var json =
                    await File.ReadAllTextAsync(
                        manifestPath);

                var manifest =
                    JsonSerializer.Deserialize<
                        ModuleManifest>(json);

                if (manifest is not null)
                {
                    result.Add(manifest);
                }
            }
            catch
            {
                // Invalid module manifests are ignored.
            }
        }

        return result;
    }
}