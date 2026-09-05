using System.Collections.ObjectModel;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FlashQueueService
{
    private readonly JsonStore<FlashTask> _store =
        new(AppPaths.Queue);

    private readonly FastbootService _fastboot =
        new();

    private readonly Sha256Service _sha =
        new();

    private readonly FlashPlanService _plan =
        new();

    private CancellationTokenSource? _cts;

    public ObservableCollection<FlashTask> Items { get; } =
        new();

    public bool Running { get; private set; }

    public async Task LoadAsync(
        CancellationToken ct = default)
    {
        Items.Clear();

        var loaded =
            await _store.LoadAsync(ct);

        foreach (var item in loaded)
        {
            Items.Add(item);
        }
    }

    public Task SaveAsync(
        CancellationToken ct = default)
    {
        return _store.SaveAsync(
            Items,
            ct);
    }

    public void Add(
        FlashTask task)
    {
        Items.Add(task);
    }

    public void Remove(
        string id)
    {
        var item =
            Items.FirstOrDefault(
                x => x.Id == id);

        if (item is not null &&
            item.State != TaskState.Running)
        {
            Items.Remove(item);
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task RunAsync(
        Func<FlashTask, Task<bool>> confirm,
        Action<FlashTask>? changed = null,
        CancellationToken externalToken = default)
    {
        if (Running)
        {
            return;
        }

        Running = true;

        using var linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                externalToken);

        _cts = linked;

        try
        {
            var queue =
                Items
                    .Where(
                        x => x.State == TaskState.Queued)
                    .ToList();

            foreach (var item in queue)
            {
                linked.Token.ThrowIfCancellationRequested();

                await RunOneAsync(
                    item,
                    confirm,
                    changed,
                    linked.Token);
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var item in Items)
            {
                if (item.State ==
                        TaskState.Running ||
                    item.State ==
                        TaskState.Hashing ||
                    item.State ==
                        TaskState.Preflight)
                {
                    SetState(
                        item,
                        TaskState.Cancelled,
                        "Cancelled.",
                        0,
                        changed);
                }
            }
        }
        finally
        {
            Running = false;

            _cts = null;

            await SaveAsync();
        }
    }

    private async Task RunOneAsync(
        FlashTask item,
        Func<FlashTask, Task<bool>> confirm,
        Action<FlashTask>? changed,
        CancellationToken ct)
    {
        item.Started = DateTime.Now;

        if (!_plan.IsAllowedPartition(
                item.Partition))
        {
            SetState(
                item,
                TaskState.Failed,
                $"Partition '{item.Partition}' is not allowed in generic queue.",
                0,
                changed);

            return;
        }

        SetState(
            item,
            TaskState.Preflight,
            "Checking fastboot device...",
            2,
            changed);

        var info =
            await _fastboot.InspectAsync(
                item.Serial,
                ct);

        if (info is null)
        {
            SetState(
                item,
                TaskState.Failed,
                "Fastboot device not available.",
                0,
                changed);

            return;
        }

        if (!File.Exists(item.ImagePath))
        {
            SetState(
                item,
                TaskState.Failed,
                "Image file does not exist.",
                0,
                changed);

            return;
        }

        if (!FastbootParser.IsTrue(
                info.Unlocked))
        {
            SetState(
                item,
                TaskState.Failed,
                "Bootloader is not reported as unlocked.",
                0,
                changed);

            return;
        }

        var file =
            new FileInfo(item.ImagePath);

        if (item.Size > 0 &&
            file.Length != item.Size)
        {
            SetState(
                item,
                TaskState.Failed,
                $"File size changed. Expected {item.Size}, actual {file.Length}.",
                0,
                changed);

            return;
        }

        SetState(
            item,
            TaskState.Hashing,
            "Calculating SHA-256...",
            5,
            changed);

        var hashProgress =
            new Progress<double>(
                value =>
                {
                    SetState(
                        item,
                        TaskState.Hashing,
                        $"SHA-256 {value:F0}%",
                        5 + value * 0.15,
                        changed);
                });

        var hash =
            await _sha.HashAsync(
                item.ImagePath,
                hashProgress,
                ct);

        if (!hash.Equals(
                item.Sha256,
                StringComparison.OrdinalIgnoreCase))
        {
            SetState(
                item,
                TaskState.Failed,
                "SHA-256 mismatch.",
                0,
                changed);

            return;
        }

        SetState(
            item,
            TaskState.AwaitingConfirmation,
            item.Critical
                ? "Critical partition requires confirmation."
                : "Waiting for confirmation.",
            20,
            changed);

        var approved =
            await confirm(item);

        if (!approved)
        {
            SetState(
                item,
                TaskState.Cancelled,
                "User cancelled.",
                0,
                changed);

            return;
        }

        item.Confirmed = true;

        SetState(
            item,
            TaskState.Running,
            "Flashing...",
            25,
            changed);

        var output =
            new Progress<string>(
                line =>
                {
                    item.Message = line;

                    changed?.Invoke(item);
                });

        var result =
            await _fastboot.FlashAsync(
                item.Serial,
                item.Partition,
                item.ImagePath,
                ct,
                output);

        if (result.ExitCode == 0)
        {
            SetState(
                item,
                TaskState.Success,
                "Flash complete.",
                100,
                changed);
        }
        else
        {
            var message =
                string.IsNullOrWhiteSpace(
                    result.StdErr)
                    ? result.StdOut
                    : result.StdErr;

            SetState(
                item,
                TaskState.Failed,
                message.Trim(),
                0,
                changed);
        }

        item.Finished = DateTime.Now;

        await SaveAsync(ct);
    }

    private static void SetState(
        FlashTask item,
        TaskState state,
        string message,
        double progress,
        Action<FlashTask>? changed)
    {
        item.State = state;

        item.Message = message;

        item.Progress =
            Math.Clamp(
                progress,
                0,
                100);

        changed?.Invoke(item);
    }
}
