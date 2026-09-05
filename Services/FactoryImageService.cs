using System.IO.Compression;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FactoryImageService
{
    private readonly Sha256Service _sha = new();

    private static readonly string[] KnownPartitions =
    {
        "vbmeta_system",
        "vbmeta_vendor",
        "vendor_boot",
        "init_boot",
        "vbmeta",
        "system_ext",
        "userdata",
        "recovery",
        "product",
        "vendor",
        "odm",
        "system",
        "super",
        "boot",
        "dtbo"
    };

    private static readonly HashSet<string> CriticalPartitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "boot",
            "init_boot",
            "vbmeta",
            "vbmeta_system",
            "vbmeta_vendor",
            "super"
        };

    public async Task<RomPackage> InspectAsync(
        string path,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Package path is empty.",
                nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Package does not exist.",
                path);
        }

        var images = new List<RomImage>();

        var scripts = new List<string>();

        if (path.EndsWith(
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            using var archive =
                ZipFile.OpenRead(path);

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(
                        entry.Name))
                {
                    continue;
                }

                var name =
                    entry.Name;

                if (IsFlashScript(name))
                {
                    scripts.Add(name);
                }

                var extension =
                    Path.GetExtension(name);

                if (!extension.Equals(
                        ".img",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !extension.Equals(
                        ".bin",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var partition =
                    GuessPartition(
                        entry.Name);

                var temp =
                    Path.Combine(
                        Path.GetTempPath(),
                        "TechFixStudio",
                        Guid.NewGuid().ToString("N") +
                        extension);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(temp)!);

                try
                {
                    await using (
                        var input =
                            entry.Open())
                    await using (
                        var output =
                            File.Create(temp))
                    {
                        await input.CopyToAsync(
                            output,
                            ct);
                    }

                    var info =
                        new FileInfo(temp);

                    var hash =
                        await _sha.HashAsync(
                            temp,
                            null,
                            ct);

                    images.Add(
                        new RomImage(
                            partition,
                            entry.FullName,
                            info.Length,
                            hash,
                            CriticalPartitions.Contains(
                                partition)));
                }
                finally
                {
                    try
                    {
                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        else
        {
            var partition =
                GuessPartition(
                    Path.GetFileName(path));

            var info =
                new FileInfo(path);

            var hash =
                await _sha.HashAsync(
                    path,
                    null,
                    ct);

            images.Add(
                new RomImage(
                    partition,
                    path,
                    info.Length,
                    hash,
                    CriticalPartitions.Contains(
                        partition)));
        }

        var product =
            GuessProduct(path);

        var build =
            GuessBuild(path);

        var region =
            GuessRegion(path);

        return new RomPackage(
            Path.GetFileName(path),
            product,
            build,
            region,
            images,
            scripts,
            path);
    }

    private static bool IsFlashScript(
        string path)
    {
        var file =
            Path.GetFileName(path);

        return file.StartsWith(
                   "flash-all",
                   StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(
                   ".bat",
                   StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(
                   ".cmd",
                   StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(
                   ".sh",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static string GuessPartition(
        string fileName)
    {
        var baseName =
            Path.GetFileNameWithoutExtension(
                fileName)
            .ToLowerInvariant();

        foreach (var partition in
                 KnownPartitions.OrderByDescending(
                     x => x.Length))
        {
            if (baseName.Contains(
                    partition,
                    StringComparison.OrdinalIgnoreCase))
            {
                return partition;
            }
        }

        return "unknown";
    }

    private static string GuessProduct(
        string path)
    {
        var name =
            Path.GetFileNameWithoutExtension(path);

        var tokens =
            name.Split(
                new[] { '-', '_', ' ' },
                StringSplitOptions.RemoveEmptyEntries);

        return tokens.Length > 0
            ? tokens[0]
            : "—";
    }

    private static string GuessBuild(
        string path)
    {
        var name =
            Path.GetFileNameWithoutExtension(path);

        var parts =
            name.Split(
                new[] { '-', '_' },
                StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 2
            ? parts[^1]
            : "—";
    }

    private static string GuessRegion(
        string path)
    {
        var upper =
            Path.GetFileNameWithoutExtension(path)
                .ToUpperInvariant();

        var known =
            new[]
            {
                "CN",
                "EU",
                "US",
                "IN",
                "JP",
                "KR",
                "GLOBAL"
            };

        return known.FirstOrDefault(
                   x => upper.Contains(
                       x,
                       StringComparison.Ordinal))
               ?? "—";
    }
}
