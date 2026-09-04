using System.Text.Json;

namespace TechFixStudio.Services;

public sealed class JsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task SaveAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken = default)
    {
        var directory =
            Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json =
            JsonSerializer.Serialize(
                value,
                Options);

        await File.WriteAllTextAsync(
            path,
            json,
            cancellationToken);
    }

    public async Task<T?> LoadAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json =
            await File.ReadAllTextAsync(
                path,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            json,
            Options);
    }
}