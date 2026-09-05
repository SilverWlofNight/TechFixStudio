using System.IO;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class HistoryService
{
    private readonly JsonStore _store;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public HistoryService(JsonStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<HistoryRecord>>
        GetAsync()
    {
        var records =
            await _store.LoadAsync<
                List<HistoryRecord>>(
                    AppPaths.HistoryFile);

        return records ?? [];
    }

    public async Task AddAsync(
        string action,
        string device,
        string result,
        string details)
    {
        await _lock.WaitAsync();

        try
        {
            var records =
                (await _store.LoadAsync<
                    List<HistoryRecord>>(
                    AppPaths.HistoryFile))
                ?? [];

            records.Insert(
                0,
                new HistoryRecord
                {
                    TimestampUtc =
                        DateTime.UtcNow,
                    Action = action,
                    Device = device,
                    Result = result,
                    Details = details
                });

            if (records.Count > 500)
            {
                records =
                    records.Take(500).ToList();
            }

            await _store.SaveAsync(
                AppPaths.HistoryFile,
                records);
        }
        finally
        {
            _lock.Release();
        }
    }
}