using System.Diagnostics;
using System.Text;
using TechFixStudio.Models;

namespace TechFixStudio.Services;

public sealed class ProcessRunner
{
    public async Task<CommandResult> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stdout)
                {
                    stdout.AppendLine(e.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stderr)
                {
                    stderr.AppendLine(e.Data);
                }
            }
        };

        try
        {
            if (!process.Start())
            {
                return new CommandResult
                {
                    ExitCode = -1,
                    StdErr = "无法启动进程。",
                    Duration = stopwatch.Elapsed
                };
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(
                    timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Ignore cleanup failure.
                    }
                }

                return new CommandResult
                {
                    ExitCode = -1,
                    StdOut = stdout.ToString(),
                    StdErr = stderr.ToString(),
                    Duration = stopwatch.Elapsed,
                    TimedOut = !cancellationToken.IsCancellationRequested
                };
            }

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                StdOut = stdout.ToString(),
                StdErr = stderr.ToString(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                ExitCode = -1,
                StdOut = stdout.ToString(),
                StdErr = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }
}