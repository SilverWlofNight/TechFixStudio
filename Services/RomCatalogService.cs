using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RomCatalogService
{
    private readonly JsonStore _store;
    private readonly string _catalogPath;

    public RomCatalogService()
        : this(new JsonStore())
    {
    }

    public RomCatalogService(JsonStore store)
    {
        _store = store;
        _catalogPath = AppPaths.RomCatalogFile;

        AppPaths.Ensure();
    }

    public async Task<List<RomEntry>> LoadAsync(
        CancellationToken ct = default)
    {
        var entries =
            await _store.LoadAsync<List<RomEntry>>(
                _catalogPath,
                ct);

        return entries ?? new List<RomEntry>();
    }

    public async Task SaveAsync(
        IEnumerable<RomEntry> entries,
        CancellationToken ct = default)
    {
        var list =
            entries?.ToList()
            ?? new List<RomEntry>();

        await _store.SaveAsync(
            _catalogPath,
            list,
            ct);
    }

    public async Task AddAsync(
        RomEntry entry,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var entries =
            await LoadAsync(ct);

        if (entry.Id == Guid.Empty)
        {
            entry.Id = Guid.NewGuid();
        }

        if (entry.CreatedUtc == default)
        {
            entry.CreatedUtc = DateTime.UtcNow;
        }

        entries.RemoveAll(
            x => x.Id == entry.Id);

        entries.Add(entry);

        await SaveAsync(
            entries,
            ct);
    }

    public async Task<bool> RemoveAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var entries =
            await LoadAsync(ct);

        var removed =
            entries.RemoveAll(
                x => x.Id == id) > 0;

        if (removed)
        {
            await SaveAsync(
                entries,
                ct);
        }

        return removed;
    }

    public async Task<RomEntry?> GetAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var entries =
            await LoadAsync(ct);

        return entries.FirstOrDefault(
            x => x.Id == id);
    }

    public async Task<List<RomEntry>> SearchAsync(
        string? query,
        CancellationToken ct = default)
    {
        var entries =
            await LoadAsync(ct);

        if (string.IsNullOrWhiteSpace(query))
        {
            return entries;
        }

        query =
            query.Trim();

        return entries
            .Where(x =>
                Contains(x.Name, query) ||
                Contains(x.Product, query) ||
                Contains(x.Version, query) ||
                Contains(x.Region, query) ||
                Contains(x.Codename, query) ||
                Contains(x.Notes, query))
            .ToList();
    }

    public async Task<List<RomEntry>> FindByProductAsync(
        string product,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return new List<RomEntry>();
        }

        var entries =
            await LoadAsync(ct);

        return entries
            .Where(x =>
                string.Equals(
                    x.Product?.Trim(),
                    product.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task SeedDefaultsAsync(
        CancellationToken ct = default)
    {
        var entries =
            await LoadAsync(ct);

        if (entries.Count > 0)
        {
            return;
        }

        var defaults =
            new List<RomEntry>
            {
                new()
                {
                    Name = "Android Platform Tools",
                    Product = "generic",
                    Version = "latest",
                    Region = "global",
                    Codename = "",
                    Url =
                        "https://developer.android.com/tools/releases/platform-tools",
                    Sha256 = "",
                    Notes =
                        "Official Android SDK Platform-Tools. " +
                        "Use the official source for adb and fastboot.",
                    CreatedUtc = DateTime.UtcNow
                }
            };

        await SaveAsync(
            defaults,
            ct);
    }

    public async Task UpsertAsync(
        RomEntry entry,
        CancellationToken ct = default)
    {
        await AddAsync(
            entry,
            ct);
    }

    public async Task ClearAsync(
        CancellationToken ct = default)
    {
        await SaveAsync(
            Array.Empty<RomEntry>(),
            ct);
    }

    private static bool Contains(
        string? value,
        string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   query,
                   StringComparison.OrdinalIgnoreCase);
    }
}
