using System.Security.Cryptography;

namespace TechFixStudio.Services;

public sealed class Sha256Service
{
    public async Task<string> CalculateAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "找不到需要校验的文件。",
                filePath);
        }

        await using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                useAsync: true);

        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<bool> VerifyAsync(
        string filePath,
        string expectedHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var actual =
            await CalculateAsync(
                filePath,
                cancellationToken);

        return string.Equals(
            actual,
            Normalize(expectedHash),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
}