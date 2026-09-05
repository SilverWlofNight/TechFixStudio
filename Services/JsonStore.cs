using System.Text.Json;

namespace TechFixStudio.Services;

/// <summary>
/// Generic JSON store used by services that bind the store
/// to a single model type.
/// </summary>
public sealed class JsonStore<T>
{
    private readonly string _path;

    private readonly JsonSerializerOptions _options =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public JsonStore(string path)
    {
        _path = path;
    }

    public async Task<List<T>> LoadAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new List<T>();
            }

            var json =
                await File.ReadAllTextAsync(
                    _path,
                    ct);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(
                       json,
                       _options)
                   ?? new List<T>();
        }
        catch (JsonException)
        {
            BackupCorruptFile();

            return new List<T>();
        }
        catch (IOException)
        {
            return new List<T>();
        }
    }

    public async Task SaveAsync(
        IEnumerable<T> items,
        CancellationToken ct = default)
    {
        await SaveInternalAsync(
            _path,
            items,
            ct);
    }

    private async Task SaveInternalAsync(
        string path,
        IEnumerable<T> items,
        CancellationToken ct)
    {
        var directory =
            Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json =
            JsonSerializer.Serialize(
                items,
                _options);

        var temp =
            path + ".tmp";

        await File.WriteAllTextAsync(
            temp,
            json,
            ct);

        ReplaceFile(
            temp,
            path);
    }

    private void BackupCorruptFile()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var backup =
                _path +
                ".corrupt-" +
                DateTime.Now.ToString(
                    "yyyyMMdd-HHmmss");

            File.Copy(
                _path,
                backup,
                true);
        }
        catch
        {
            // Corrupt-file backup must never
            // prevent the application from starting.
        }
    }

    private static void ReplaceFile(
        string temp,
        string destination)
    {
        if (File.Exists(destination))
        {
            try
            {
                File.Replace(
                    temp,
                    destination,
                    null);

                return;
            }
            catch
            {
                // Some Windows file systems can reject
                // File.Replace in certain situations.
                // Fall back to delete + move.
            }

            File.Delete(destination);
        }

        File.Move(
            temp,
            destination);
    }
}


/// <summary>
/// Non-generic compatibility JSON store.
///
/// TechFix Studio 2.0 contains services that use:
///
///     JsonStore
///
/// and call:
///
///     LoadAsync<T>(path)
///     SaveAsync<T>(path, items)
///
/// This class preserves that API while JsonStore<T>
/// remains available for newer services.
/// </summary>
public sealed class JsonStore
{
    private readonly JsonSerializerOptions _options =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public JsonStore()
    {
    }

    public async Task<T?> LoadAsync<T>(
        string path,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(path))
            {
                return default;
            }

            var json =
                await File.ReadAllTextAsync(
                    path,
                    ct);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(
                json,
                _options);
        }
        catch (JsonException)
        {
            BackupCorruptFile(path);

            return default;
        }
        catch (IOException)
        {
            return default;
        }
    }

    public async Task SaveAsync<T>(
        string path,
        T value,
        CancellationToken ct = default)
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
                _options);

        var temp =
            path + ".tmp";

        await File.WriteAllTextAsync(
            temp,
            json,
            ct);

        ReplaceFile(
            temp,
            path);
    }

    private static void BackupCorruptFile(
        string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            var backup =
                path +
                ".corrupt-" +
                DateTime.Now.ToString(
                    "yyyyMMdd-HHmmss");

            File.Copy(
                path,
                backup,
                true);
        }
        catch
        {
            // Ignore backup errors.
        }
    }

    private static void ReplaceFile(
        string temp,
        string destination)
    {
        if (File.Exists(destination))
        {
            try
            {
                File.Replace(
                    temp,
                    destination,
                    null);

                return;
            }
            catch
            {
                // Fall back below.
            }

            File.Delete(destination);
        }

        File.Move(
            temp,
            destination);
    }
}
