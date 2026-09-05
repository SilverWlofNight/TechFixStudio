using System.Diagnostics;
using System.Text;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class ProcessRunner
{
    public async Task<CommandResult> RunAsync(
        string file,
        IEnumerable<string> args,
        TimeSpan timeout,
        CancellationToken ct = default,
        IProgress<string>? output = null)
    {
        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(file))
        {
            return new CommandResult(
                -10,
                "",
                "Executable path is empty.",
                sw.Elapsed,
                false);
        }

        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            if (!process.Start())
            {
                return new CommandResult(
                    -1,
                    "",
                    "Process failed to start.",
                    sw.Elapsed,
                    false);
            }

            var stdoutTask = ReadStreamAsync(
                process.StandardOutput,
                stdout,
                output,
                false,
                ct);

            var stderrTask = ReadStreamAsync(
                process.StandardError,
                stderr,
                output,
                true,
                ct);

            var waitTask = process.WaitForExitAsync(ct);

            var timeoutTask = Task.Delay(timeout, ct);

            var completed = await Task.WhenAny(
                waitTask,
                timeoutTask);

            if (completed == timeoutTask &&
                !waitTask.IsCompleted)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch
                {
                }

                await Task.WhenAll(
                    IgnoreCancellation(stdoutTask),
                    IgnoreCancellation(stderrTask));

                return new CommandResult(
                    -2,
                    stdout.ToString(),
                    stderr.ToString(),
                    sw.Elapsed,
                    true);
            }

            await waitTask;

            await Task.WhenAll(
                stdoutTask,
                stderrTask);

            return new CommandResult(
                process.ExitCode,
                stdout.ToString(),
                stderr.ToString(),
                sw.Elapsed,
                false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
            }

            return new CommandResult(
                -3,
                stdout.ToString(),
                stderr.ToString(),
                sw.Elapsed,
                false);
        }
        catch (Exception ex)
        {
            return new CommandResult(
                -4,
                stdout.ToString(),
                stderr + Environment.NewLine + ex.Message,
                sw.Elapsed,
                false);
        }
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder buffer,
        IProgress<string>? output,
        bool error,
        CancellationToken ct)
    {
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct);

            if (line is null)
            {
                break;
            }

            buffer.AppendLine(line);

            output?.Report(
                error
                    ? $"[ERR] {line}"
                    : line);
        }
    }

    private static async Task IgnoreCancellation(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
        }
    }
}
