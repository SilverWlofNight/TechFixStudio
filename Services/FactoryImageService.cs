using System.IO;
using System.IO.Compression;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FactoryImageService
{
    private readonly Sha256Service _sha256;

    public FactoryImageService(
        Sha256Service sha256)
    {
        _sha256 = sha256;
    }

    public async Task<IReadOnlyList<string>>
        AnalyzeAsync(
            string filePath,
            CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "ROM 文件不存在。",
                filePath);
        }

        var extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        return extension switch
        {
            ".img" or ".bin" =>
                [
                    await BuildDescriptionAsync(
                        filePath,
                        cancellationToken)
                ],

            ".zip" =>
                await AnalyzeZipAsync(
                    filePath,
                    cancellationToken),

            _ =>
                []
        };
    }

    private async Task<List<string>> AnalyzeZipAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var result = new List<string>();

        using var archive =
            ZipFile.OpenRead(filePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(
                    entry.Name))
            {
                continue;
            }

            var extension =
                Path.GetExtension(entry.Name)
                    .ToLowerInvariant();

            if (extension is
                ".img" or
                ".bin")
            {
                result.Add(
                    $"{entry.FullName} | {entry.Length:N0} bytes");
            }
            else if (
                entry.Name.Equals(
                    "flash-all.bat",
                    StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Equals(
                    "flash-all.sh",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    $"SCRIPT | {entry.FullName}");
            }
        }

        return result;
    }

    private async Task<string>
        BuildDescriptionAsync(
            string path,
            CancellationToken cancellationToken)
    {
        var hash =
            await _sha256.CalculateAsync(
                path,
                cancellationToken);

        return
            $"{Path.GetFileName(path)} | " +
            $"{new FileInfo(path).Length:N0} bytes | " +
            $"SHA256={hash}";
    }
}