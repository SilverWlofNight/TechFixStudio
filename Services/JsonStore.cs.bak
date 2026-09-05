using System.Text.Json;

namespace TechFixStudio.Services;

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
        var directory =
            Path.GetDirectoryName(_path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        var json =
            JsonSerializer.Serialize(
                items,
                _options);

        var temp =
            _path + ".tmp";

        await File.WriteAllTextAsync(
            temp,
            json,
            ct);

        if (File.Exists(_path))
        {
            File.Replace(
                temp,
                _path,
                null);
        }
        else
        {
            File.Move(
                temp,
                _path);
        }
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
        }
    }
}
