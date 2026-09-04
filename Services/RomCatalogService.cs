using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class RomCatalogService
{
    private readonly JsonStore _store;

    public RomCatalogService(JsonStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<RomEntry>>
        GetAllAsync()
    {
        var entries =
            await _store.LoadAsync<
                List<RomEntry>>(
                    AppPaths.RomCatalogFile);

        return entries ?? [];
    }

    public async Task AddAsync(
        RomEntry entry)
    {
        var entries =
            (await _store.LoadAsync<
                List<RomEntry>>(
                AppPaths.RomCatalogFile))
            ?? [];

        entries.RemoveAll(
            x => x.Id == entry.Id);

        entries.Add(entry);

        await _store.SaveAsync(
            AppPaths.RomCatalogFile,
            entries);
    }

    public async Task DeleteAsync(
        Guid id)
    {
        var entries =
            (await _store.LoadAsync<
                List<RomEntry>>(
                AppPaths.RomCatalogFile))
            ?? [];

        entries.RemoveAll(
            x => x.Id == id);

        await _store.SaveAsync(
            AppPaths.RomCatalogFile,
            entries);
    }
}