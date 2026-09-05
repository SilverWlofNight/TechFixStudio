using System.Security.Cryptography;

namespace TechFixStudio.Services;

public sealed class Sha256Service
{
    public async Task<string> HashAsync(
        string path,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "File not found.",
                path);
        }

        var fileInfo =
            new FileInfo(path);

        var total =
            fileInfo.Length;

        if (total <= 0)
        {
            using var empty =
                SHA256.Create();

            return Convert.ToHexString(
                empty.ComputeHash(Array.Empty<byte>()))
                .ToLowerInvariant();
        }

        using var sha =
            SHA256.Create();

        await using var stream =
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

        var buffer =
            new byte[1024 * 1024];

        long readTotal = 0;

        while (true)
        {
            var read =
                await stream.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length),
                    ct);

            if (read == 0)
            {
                break;
            }

            sha.TransformBlock(
                buffer,
                0,
                read,
                null,
                0);

            readTotal += read;

            progress?.Report(
                Math.Min(
                    100,
                    readTotal * 100.0 / total));
        }

        sha.TransformFinalBlock(
            Array.Empty<byte>(),
            0,
            0);

        return Convert.ToHexString(
                sha.Hash!)
            .ToLowerInvariant();
    }

    public Task<string> HashAsync(
        string path,
        CancellationToken ct)
    {
        return HashAsync(
            path,
            null,
            ct);
    }
}
