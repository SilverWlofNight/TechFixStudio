using TechFixStudio.Infrastructure;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class FlashQueueService
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator _tools;
    private readonly Sha256Service _sha256;
    private readonly AuditService _audit;
    private readonly HistoryService _history;

    private static readonly string[] AllowedPartitions =
    [
        "boot",
        "init_boot",
        "vendor_boot",
        "dtbo",
        "vbmeta",
        "recovery"
    ];

    public FlashQueueService(
        ProcessRunner runner,
        ToolLocator tools,
        Sha256Service sha256,
        AuditService audit,
        HistoryService history)
    {
        _runner = runner;
        _tools = tools;
        _sha256 = sha256;
        _audit = audit;
        _history = history;
    }

    public async Task<List<FlashTask>> LoadAsync()
    {
        return await LoadInternalAsync();
    }

    public async Task AddAsync(
        FlashTask task)
    {
        ValidateTask(task);

        var tasks =
            await LoadInternalAsync();

        tasks.Add(task);

        await SaveInternalAsync(tasks);
    }

    public async Task RemoveAsync(
        Guid id)
    {
        var tasks =
            await LoadInternalAsync();

        tasks.RemoveAll(
            x => x.Id == id &&
                 x.State == FlashTaskState.Queued);

        await SaveInternalAsync(tasks);
    }

    public async Task CancelAsync(
        Guid id)
    {
        var tasks =
            await LoadInternalAsync();

        var task =
            tasks.FirstOrDefault(
                x => x.Id == id);

        if (task is null)
        {
            return;
        }

        if (task.State is
            FlashTaskState.Queued or
            FlashTaskState.Preflight or
            FlashTaskState.Hashing or
            FlashTaskState.WaitingForConfirmation)
        {
            task.State =
                FlashTaskState.Cancelled;

            task.Message =
                "任务已取消。";
        }

        await SaveInternalAsync(tasks);
    }

    public async Task ExecuteAsync(
        Guid taskId,
        Func<FlashTask, Task<bool>> confirmation,
        CancellationToken cancellationToken = default)
    {
        var tasks =
            await LoadInternalAsync();

        var task =
            tasks.FirstOrDefault(
                x => x.Id == taskId);

        if (task is null)
        {
            throw new InvalidOperationException(
                "找不到刷机任务。");
        }

        try
        {
            task.State =
                FlashTaskState.Preflight;

            task.Message =
                "正在执行设备预检。";

            await SaveInternalAsync(tasks);

            ValidateTask(task);

            var fastboot =
                _tools.FastbootPath
                ?? throw new InvalidOperationException(
                    "未找到 fastboot.exe。");

            task.State =
                FlashTaskState.Hashing;

            task.Message =
                "正在计算镜像 SHA-256。";

            await SaveInternalAsync(tasks);

            task.ActualSha256 =
                await _sha256.CalculateAsync(
                    task.ImagePath,
                    cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                    task.ExpectedSha256) &&
                !string.Equals(
                    task.ActualSha256,
                    NormalizeHash(
                        task.ExpectedSha256),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SHA-256 校验失败，已阻止刷写。");
            }

            task.State =
                FlashTaskState.WaitingForConfirmation;

            task.Message =
                "等待用户确认。";

            await SaveInternalAsync(tasks);

            var approved =
                await confirmation(task);

            if (!approved)
            {
                task.State =
                    FlashTaskState.Cancelled;

                task.Message =
                    "用户取消刷写。";

                await SaveInternalAsync(tasks);

                await _audit.WriteAsync(
                    "Flash",
                    $"{task.Serial}:{task.Partition}",
                    RiskLevel.Critical,
                    false,
                    "用户取消");

                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            task.State =
                FlashTaskState.Flashing;

            task.StartedUtc =
                DateTime.UtcNow;

            task.Progress = 0;

            task.Message =
                $"正在刷写 {task.Partition}";

            await SaveInternalAsync(tasks);

            await _audit.WriteAsync(
                "FlashStart",
                $"{task.Serial}:{task.Partition}",
                RiskLevel.Critical,
                true,
                task.ImagePath);

            var result =
                await _runner.RunAsync(
                    fastboot,
                    [
                        "-s",
                        task.Serial,
                        "flash",
                        task.Partition,
                        task.ImagePath
                    ],
                    TimeSpan.FromMinutes(15),
                    cancellationToken);

            task.Progress = 100;

            if (result.Success)
            {
                task.State =
                    FlashTaskState.Succeeded;

                task.Message =
                    result.StdOut +
                    Environment.NewLine +
                    result.StdErr;

                await _audit.WriteAsync(
                    "Flash",
                    $"{task.Serial}:{task.Partition}",
                    RiskLevel.Critical,
                    true,
                    task.Message);

                await _history.AddAsync(
                    "Flash",
                    task.Serial,
                    "Succeeded",
                    task.Message);
            }
            else
            {
                task.State =
                    FlashTaskState.Failed;

                task.Message =
                    result.StdErr;

                await _audit.WriteAsync(
                    "Flash",
                    $"{task.Serial}:{task.Partition}",
                    RiskLevel.Critical,
                    false,
                    task.Message);

                await _history.AddAsync(
                    "Flash",
                    task.Serial,
                    "Failed",
                    task.Message);
            }

            task.FinishedUtc =
                DateTime.UtcNow;

            await SaveInternalAsync(tasks);
        }
        catch (OperationCanceledException)
        {
            task.State =
                FlashTaskState.Cancelled;

            task.Message =
                "任务被取消。";

            task.FinishedUtc =
                DateTime.UtcNow;

            await SaveInternalAsync(tasks);
        }
        catch (Exception ex)
        {
            task.State =
                FlashTaskState.Failed;

            task.Message =
                ex.Message;

            task.FinishedUtc =
                DateTime.UtcNow;

            await SaveInternalAsync(tasks);

            await _audit.WriteAsync(
                "Flash",
                $"{task.Serial}:{task.Partition}",
                RiskLevel.Critical,
                false,
                ex.ToString());

            await _history.AddAsync(
                "Flash",
                task.Serial,
                "Failed",
                ex.ToString());
        }
    }

    private static void ValidateTask(
        FlashTask task)
    {
        if (string.IsNullOrWhiteSpace(
                task.Serial))
        {
            throw new ArgumentException(
                "设备 Serial 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(
                task.Partition))
        {
            throw new ArgumentException(
                "分区不能为空。");
        }

        if (!AllowedPartitions.Contains(
                task.Partition,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"分区 {task.Partition} 不在受控刷写白名单中。");
        }

        if (!File.Exists(
                task.ImagePath))
        {
            throw new FileNotFoundException(
                "镜像文件不存在。",
                task.ImagePath);
        }
    }

    private async Task<List<FlashTask>>
        LoadInternalAsync()
    {
        var store = new JsonStore();

        return await store.LoadAsync<
                   List<FlashTask>>(
                   AppPaths.FlashQueueFile)
               ?? [];
    }

    private async Task SaveInternalAsync(
        List<FlashTask> tasks)
    {
        var store = new JsonStore();

        await store.SaveAsync(
            AppPaths.FlashQueueFile,
            tasks);
    }

    private static string NormalizeHash(
        string value)
    {
        return value
            .Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }
}


