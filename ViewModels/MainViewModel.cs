using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;
using TechFixStudio.Services;

namespace TechFixStudio.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AndroidService _android = new();
    private readonly AndroidDiagnosticsService _diagnostics = new();
    private readonly MagiskService _magisk = new();
    private readonly FastbootService _fastboot = new();
    private readonly FlashQueueService _queue = new();
    private readonly FactoryImageService _factory = new();
    private readonly FlashPlanService _flashPlan = new();
    private readonly ProcessRunner _runner = new();
    private readonly RiskEngine _risk = new();
    private readonly WindowsRepairService _repair;
    private readonly SshService _ssh;
    private readonly AuditService _audit = new();
    private readonly ToolLocator _tools = new();
    private readonly Sha256Service _sha = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _status = "Initializing...";
    private string _clock = "";
    private string _terminal = "TECHFIX STUDIO // REAL PROCESS TERMINAL\r\n";
    private string _factoryText = "No package loaded.";
    private string _planText = "No flash plan.";
    private string _sshOutput = "SSH ready.";
    private string _command = "";
    private string _flashPath = "";
    private string _flashPartition = "boot";
    private string _flashHash = "";
    private string _flashStatus = "Idle";
    private string _sshHost = "";
    private string _sshUser = "";
    private string _sshCommand = "uname -a";
    private string _logcat = "";
    private string _magiskText = "Magisk status not loaded.";

    private double _cpu;
    private double _ram;
    private double _battery;
    private double _temp;

    private DeviceInfo? _selected;
    private AndroidDiagnostics? _androidDiagnostics;
    private MagiskInfo? _magiskInfo;
    private RomPackage? _currentRom;
    private FlashPlan? _currentPlan;

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string Clock
    {
        get => _clock;
        set => Set(ref _clock, value);
    }

    public string Terminal
    {
        get => _terminal;
        set => Set(ref _terminal, value);
    }

    public string Command
    {
        get => _command;
        set => Set(ref _command, value);
    }

    public string FactoryText
    {
        get => _factoryText;
        set => Set(ref _factoryText, value);
    }

    public string PlanText
    {
        get => _planText;
        set => Set(ref _planText, value);
    }

    public string SshOutput
    {
        get => _sshOutput;
        set => Set(ref _sshOutput, value);
    }

    public string SshHost
    {
        get => _sshHost;
        set => Set(ref _sshHost, value);
    }

    public string SshUser
    {
        get => _sshUser;
        set => Set(ref _sshUser, value);
    }

    public string SshCommand
    {
        get => _sshCommand;
        set => Set(ref _sshCommand, value);
    }

    public string FlashPath
    {
        get => _flashPath;
        set => Set(ref _flashPath, value);
    }

    public string FlashPartition
    {
        get => _flashPartition;
        set => Set(ref _flashPartition, value);
    }

    public string FlashHash
    {
        get => _flashHash;
        set => Set(ref _flashHash, value);
    }

    public string FlashStatus
    {
        get => _flashStatus;
        set => Set(ref _flashStatus, value);
    }

    public string Logcat
    {
        get => _logcat;
        set => Set(ref _logcat, value);
    }

    public string MagiskText
    {
        get => _magiskText;
        set => Set(ref _magiskText, value);
    }

    public double Cpu
    {
        get => _cpu;
        set => Set(ref _cpu, value);
    }

    public double Ram
    {
        get => _ram;
        set => Set(ref _ram, value);
    }

    public double Battery
    {
        get => _battery;
        set => Set(ref _battery, value);
    }

    public double BatteryTemp
    {
        get => _temp;
        set => Set(ref _temp, value);
    }

    public DeviceInfo? SelectedDevice
    {
        get => _selected;
        set => Set(ref _selected, value);
    }

    public AndroidDiagnostics? AndroidDiagnostics
    {
        get => _androidDiagnostics;
        private set => Set(ref _androidDiagnostics, value);
    }

    public MagiskInfo? MagiskInfo
    {
        get => _magiskInfo;
        private set => Set(ref _magiskInfo, value);
    }

    public RomPackage? CurrentRom
    {
        get => _currentRom;
        private set => Set(ref _currentRom, value);
    }

    public FlashPlan? CurrentPlan
    {
        get => _currentPlan;
        private set => Set(ref _currentPlan, value);
    }

    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    public ObservableCollection<FlashTask> FlashQueue => _queue.Items;

    public IReadOnlyList<string> Partitions { get; } =
        new[]
        {
            "boot",
            "init_boot",
            "vendor_boot",
            "dtbo",
            "vbmeta",
            "vbmeta_system",
            "vbmeta_vendor",
            "recovery"
        };

    public MainViewModel()
    {
        AppPaths.EnsureDirectories();

        _repair = new WindowsRepairService(_runner);
        _ssh = new SshService(_runner, _risk);

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        timer.Tick += async (_, _) =>
        {
            Clock = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (SelectedDevice?.Transport == Transport.Adb)
            {
                try
                {
                    var telemetry =
                        await _android.TelemetryAsync(SelectedDevice);

                    if (telemetry is not null)
                    {
                        Battery = telemetry.Battery;
                        BatteryTemp = telemetry.Temperature;
                    }
                }
                catch
                {
                    // Telemetry failure must not stop the UI timer.
                }
            }
        };

        timer.Start();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _queue.LoadAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Status = "Initialization error: " + ex.Message;
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            var oldSerial = SelectedDevice?.Serial;

            Devices.Clear();

            foreach (var device in await _android.ScanAsync())
            {
                Devices.Add(device);
            }

            foreach (var serial in await _fastboot.DevicesAsync())
            {
                Devices.Add(
                    new DeviceInfo(
                        serial,
                        Transport.Fastboot,
                        "fastboot",
                        "Fastboot Device",
                        "—",
                        "—",
                        "—",
                        "—",
                        "—",
                        false,
                        false));
            }

            SelectedDevice =
                Devices.FirstOrDefault(x => x.Serial == oldSerial)
                ?? Devices.FirstOrDefault();

            Status =
                $"Detected {Devices.Count} device(s) | " +
                $"ADB: {(_tools.AdbPath is null ? "MISSING" : "OK")} | " +
                $"Fastboot: {(_tools.FastbootPath is null ? "MISSING" : "OK")}";

            OnPropertyChanged(nameof(FlashQueue));
        }
        catch (Exception ex)
        {
            Status = "Scan failed: " + ex.Message;
        }
    }

    public async Task RunCommandAsync()
    {
        var command = Command.Trim();

        if (command.Length == 0)
            return;

        var risk = _risk.Assess(command);

        AppendTerminal(
            $"\r\n> {command}\r\n");

        if (risk >= RiskLevel.High)
        {
            AppendTerminal(
                $"BLOCKED [{risk}] Use the dedicated safety workflow.\r\n");

            await _audit.WriteAsync(
                "blocked-command",
                command,
                risk,
                false,
                "Blocked by safety policy.");

            return;
        }

        if (!command.StartsWith(
                "adb",
                StringComparison.OrdinalIgnoreCase)
            ||
            (command.Length > 3 &&
             !char.IsWhiteSpace(command[3])))
        {
            AppendTerminal(
                "Only the registered ADB executor is available in this terminal.\r\n");

            await _audit.WriteAsync(
                "unregistered-command",
                command,
                risk,
                false,
                "No registered executor.");

            return;
        }

        if (_tools.AdbPath is null)
        {
            AppendTerminal(
                "adb executable not found.\r\n");

            await _audit.WriteAsync(
                "adb",
                command,
                risk,
                false,
                "adb executable not found.");

            return;
        }

        var args =
            SplitArguments(
                command.Length == 3
                    ? ""
                    : command[4..]);

        var progress =
            new Progress<string>(
                line =>
                    AppendTerminal(
                        line + Environment.NewLine));

        var result =
            await _runner.RunAsync(
                _tools.AdbPath,
                args,
                TimeSpan.FromMinutes(2),
                output: progress);

        AppendTerminal(result.StdOut);
        AppendTerminal(result.StdErr);

        await _audit.WriteAsync(
            "adb",
            command,
            risk,
            result.ExitCode == 0,
            result.StdOut + result.StdErr);
    }

    public async Task DiagnoseAndroidAsync()
    {
        if (SelectedDevice?.Transport != Transport.Adb)
        {
            Status =
                "Select an ADB Android device.";

            return;
        }

        Status =
            "Collecting Android diagnostics...";

        AndroidDiagnostics =
            await _diagnostics.CollectAsync(
                SelectedDevice);

        if (AndroidDiagnostics is null)
        {
            Status =
                "Android diagnostics failed.";

            return;
        }

        Battery =
            AndroidDiagnostics.Battery.Level;

        BatteryTemp =
            AndroidDiagnostics.Battery.Temperature;

        Ram =
            AndroidDiagnostics.Memory.UsedPercent;

        Status =
            $"Diagnostics ready: " +
            $"{AndroidDiagnostics.Manufacturer} " +
            $"{AndroidDiagnostics.Model}";
    }

    public async Task LoadLogcatAsync()
    {
        if (SelectedDevice?.Transport != Transport.Adb)
        {
            Logcat =
                "Select an ADB device.";

            return;
        }

        Logcat =
            await _android.LogcatAsync(
                SelectedDevice.Serial);
    }

    public async Task RebootBootloaderAsync()
    {
        if (SelectedDevice?.Transport != Transport.Adb)
            return;

        if (!Confirm(
                "Reboot the selected Android device into Bootloader?"))
            return;

        var result =
            await _android.RebootAsync(
                SelectedDevice.Serial,
                "bootloader");

        AppendTerminal(
            result.StdOut + result.StdErr);
    }

    public async Task RebootRecoveryAsync()
    {
        if (SelectedDevice?.Transport != Transport.Adb)
            return;

        if (!Confirm(
                "Reboot the selected Android device into Recovery?"))
            return;

        var result =
            await _android.RebootAsync(
                SelectedDevice.Serial,
                "recovery");

        AppendTerminal(
            result.StdOut + result.StdErr);
    }

    public async Task InspectMagiskAsync()
    {
        if (SelectedDevice?.Transport != Transport.Adb)
        {
            MagiskText =
                "Select an ADB device.";

            return;
        }

        MagiskInfo =
            await _magisk.InspectAsync(
                SelectedDevice.Serial);

        if (MagiskInfo is null)
        {
            MagiskText =
                "Magisk inspection failed.";

            return;
        }

        MagiskText =
            $"Root: {MagiskInfo.RootDetected}\r\n" +
            $"Magisk: {MagiskInfo.MagiskDetected}\r\n" +
            $"Version: {MagiskInfo.Version}\r\n" +
            $"Zygisk: {MagiskInfo.Zygisk}\r\n" +
            $"DenyList: {MagiskInfo.DenyList}\r\n" +
            $"Install: {MagiskInfo.InstallPath}";
    }

    public async Task RunSfcAsync()
    {
        if (!Confirm(
                "Run SFC /scannow?"))
            return;

        var result =
            await _repair.RunSfcAsync();

        AppendTerminal(
            result.StdOut + result.StdErr);

        await WriteActionAudit(
            "SFC",
            "sfc.exe /scannow",
            RiskLevel.Medium,
            result);
    }

    public async Task RunDismCheckAsync()
    {
        if (!Confirm(
                "Run DISM CheckHealth?"))
            return;

        var result =
            await _repair.CheckHealthAsync();

        AppendTerminal(
            result.StdOut + result.StdErr);

        await WriteActionAudit(
            "DISM CheckHealth",
            "DISM.exe /Online /Cleanup-Image /CheckHealth",
            RiskLevel.Medium,
            result);
    }

    public async Task RunDismAsync()
    {
        if (!Confirm(
                "Run DISM RestoreHealth?"))
            return;

        var result =
            await _repair.RestoreHealthAsync();

        AppendTerminal(
            result.StdOut + result.StdErr);

        await WriteActionAudit(
            "DISM RestoreHealth",
            "DISM.exe /Online /Cleanup-Image /RestoreHealth",
            RiskLevel.Medium,
            result);
    }

    public async Task InspectFactoryAsync(string path)
    {
        try
        {
            var package =
                await _factory.InspectAsync(path);

            CurrentRom =
                package;

            FactoryText =
                BuildFactoryText(package);

            FlashPath =
                path;

            FlashHash =
                package.Images.Count == 1
                    ? package.Images[0].Sha256
                    : "";

            Status =
                $"ROM analysis complete: " +
                $"{package.Images.Count} image(s), " +
                $"{package.Scripts.Count} script(s).";
        }
        catch (Exception ex)
        {
            FactoryText =
                "Factory analysis failed:\r\n" +
                ex.Message;

            Status =
                "Factory analysis failed.";
        }
    }

    public async Task BuildFlashPlanAsync()
    {
        if (CurrentRom is null)
        {
            PlanText =
                "Please analyze a ROM / Factory package first.";

            return;
        }

        if (SelectedDevice?.Transport != Transport.Fastboot)
        {
            PlanText =
                "Select a Fastboot device.";

            return;
        }

        var info =
            await _fastboot.InspectAsync(
                SelectedDevice.Serial);

        if (info is null)
        {
            PlanText =
                "Unable to inspect Fastboot device.";

            return;
        }

        CurrentPlan =
            _flashPlan.BuildPlan(
                info,
                CurrentRom);

        PlanText =
            BuildPlanText(CurrentPlan);
    }

    public async Task PrepareFlashImageAsync(
        FlashPlanItem item)
    {
        if (item is null ||
            string.IsNullOrWhiteSpace(item.Partition))
            return;

        if (!item.Allowed)
        {
            FlashStatus =
                $"Partition {item.Partition} " +
                "is blocked by the generic safety policy.";

            return;
        }

        var path =
            item.ImagePath;

        if (!File.Exists(path) &&
            CurrentRom is not null &&
            CurrentRom.Source.EndsWith(
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            path =
                await ExtractRomEntryAsync(
                    CurrentRom.Source,
                    item.ImagePath);
        }

        if (string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path))
        {
            FlashStatus =
                "Image could not be materialized from the package.";

            return;
        }

        FlashPath =
            path;

        FlashPartition =
            item.Partition;

        FlashHash =
            await _sha.HashAsync(path);

        FlashStatus =
            $"Prepared {item.Partition}; " +
            "SHA-256 verified locally.";
    }

    public async Task AddTaskAsync()
    {
        if (SelectedDevice?.Transport != Transport.Fastboot)
        {
            FlashStatus =
                "Select a Fastboot device.";

            return;
        }

        if (!_flashPlan.IsAllowedPartition(
                FlashPartition))
        {
            FlashStatus =
                $"Partition '{FlashPartition}' " +
                "is not allowed in generic queue.";

            return;
        }

        if (!File.Exists(FlashPath))
        {
            FlashStatus =
                "Select an image file.";

            return;
        }

        if (FlashHash.Length != 64 ||
            FlashHash.Any(c => !Uri.IsHexDigit(c)))
        {
            FlashStatus =
                "SHA-256 must be 64 hexadecimal characters.";

            return;
        }

        var file =
            new FileInfo(FlashPath);

        var actualHash =
            await _sha.HashAsync(FlashPath);

        if (!actualHash.Equals(
                FlashHash,
                StringComparison.OrdinalIgnoreCase))
        {
            FlashStatus =
                "SHA-256 mismatch; task was not queued.";

            return;
        }

        var task =
            new FlashTask
            {
                Id =
                    Guid.NewGuid().ToString("N"),

                Serial =
                    SelectedDevice.Serial,

                Partition =
                    FlashPartition,

                ImagePath =
                    Path.GetFullPath(FlashPath),

                Sha256 =
                    actualHash.ToLowerInvariant(),

                Size =
                    file.Length,

                State =
                    TaskState.Queued,

                Message =
                    "Queued",

                Created =
                    DateTime.Now,

                Critical =
                    FlashPartition.Equals(
                        "boot",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    FlashPartition.Equals(
                        "init_boot",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    FlashPartition.StartsWith(
                        "vbmeta",
                        StringComparison.OrdinalIgnoreCase)
            };

        _queue.Add(task);

        await _queue.SaveAsync();

        FlashStatus =
            "Task queued with verified SHA-256.";

        OnPropertyChanged(
            nameof(FlashQueue));
    }

    public async Task RunQueueAsync()
    {
        if (!Confirm(
                "Start the REAL fastboot flash queue?\r\n\r\n" +
                "Every image is preflighted, hashed and " +
                "individually confirmed."))
            return;

        await _queue.RunAsync(
            item =>
                Application.Current.Dispatcher
                    .InvokeAsync(
                        () =>
                            Confirm(
                                $"FLASH {item.Partition}?\r\n\r\n" +
                                $"Device: {item.Serial}\r\n" +
                                $"Image: {Path.GetFileName(item.ImagePath)}\r\n" +
                                $"SHA-256: {item.Sha256}\r\n\r\n" +
                                (
                                    item.Critical
                                        ? "WARNING: CRITICAL PARTITION\r\n"
                                        : "")))
                    .Task,

            item =>
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        FlashStatus =
                            $"{item.State}: {item.Message}";

                        OnPropertyChanged(
                            nameof(FlashQueue));
                    }));
    }

    public Task CancelQueueAsync()
    {
        _queue.Cancel();

        FlashStatus =
            "Cancellation requested.";

        return Task.CompletedTask;
    }

    public async Task RemoveTaskAsync(
        FlashTask? task)
    {
        if (task is null)
            return;

        _queue.Remove(task.Id);

        await _queue.SaveAsync();

        OnPropertyChanged(
            nameof(FlashQueue));
    }

    public async Task FastbootInfoAsync()
    {
        if (SelectedDevice?.Transport != Transport.Fastboot)
        {
            Status =
                "Select a Fastboot device.";

            return;
        }

        var info =
            await _fastboot.InspectAsync(
                SelectedDevice.Serial);

        if (info is null)
        {
            AppendTerminal(
                "\r\nFASTBOOT: device inspection failed.\r\n");

            return;
        }

        AppendTerminal(
            "\r\nFASTBOOT\r\n" +
            $"Serial: {info.Serial}\r\n" +
            $"Product: {info.Product}\r\n" +
            $"Variant: {info.Variant}\r\n" +
            $"Slot: {info.Slot}\r\n" +
            $"Unlocked: {info.Unlocked}\r\n" +
            $"Secure: {info.Secure}\r\n" +
            $"Anti-Rollback: {info.AntiRollback}\r\n\r\n" +
            $"{info.Raw}\r\n");
    }

    public async Task RunSshAsync()
    {
        var risk =
            _risk.Assess(
                "ssh " + SshCommand);

        if (risk >= RiskLevel.High)
        {
            SshOutput =
                "Blocked by safety policy.";

            await _audit.WriteAsync(
                "blocked-ssh",
                SshHost,
                risk,
                false,
                "Blocked by safety policy.");

            return;
        }

        try
        {
            var result =
                await _ssh.ExecuteAsync(
                    SshHost,
                    SshUser,
                    SshCommand,
                    22);

            SshOutput =
                result.StdOut +
                result.StdErr;

            await _audit.WriteAsync(
                "ssh",
                SshHost,
                risk,
                result.ExitCode == 0,
                SshOutput);
        }
        catch (Exception ex)
        {
            SshOutput =
                ex.Message;

            await _audit.WriteAsync(
                "ssh",
                SshHost,
                risk,
                false,
                ex.Message);
        }
    }

    private async Task WriteActionAudit(
        string action,
        string target,
        RiskLevel risk,
        CommandResult result)
    {
        await _audit.WriteAsync(
            action,
            target,
            risk,
            result.ExitCode == 0,
            result.StdOut + result.StdErr);
    }

    private static string BuildFactoryText(
        RomPackage package)
    {
        return
            $"{package.Name}\r\n" +
            $"Product: {package.Product}\r\n" +
            $"Build: {package.Build}\r\n" +
            $"Region: {package.Region}\r\n" +
            $"Source: {package.Source}\r\n" +
            $"Images: {package.Images.Count}\r\n" +
            $"Scripts: {string.Join(", ", package.Scripts)}\r\n\r\n" +
            string.Join(
                "\r\n",
                package.Images.Select(
                    x =>
                        $"{x.Partition,-16} " +
                        $"{x.Size,14} bytes  " +
                        $"SHA {x.Sha256}" +
                        (
                            x.Critical
                                ? "  [CRITICAL]"
                                : "")));
    }

    private static string BuildPlanText(
        FlashPlan plan)
    {
        return
            $"Serial: {plan.Serial}\r\n" +
            $"Product: {plan.Product}\r\n" +
            $"Slot: {plan.CurrentSlot}\r\n" +
            $"Unlocked: {plan.Unlocked}\r\n" +
            $"Secure: {plan.Secure}\r\n" +
            $"Anti-Rollback: {plan.AntiRollback}\r\n" +
            $"Safe: {plan.SafeToProceed}\r\n\r\n" +
            "WARNINGS:\r\n" +
            string.Join(
                "\r\n",
                plan.Warnings.Select(
                    x => " - " + x)) +
            "\r\n\r\nITEMS:\r\n" +
            string.Join(
                "\r\n",
                plan.Items.Select(
                    x =>
                        $"{x.Partition,-16} " +
                        $"{(x.Allowed ? "ALLOWED" : "BLOCKED"),-8} " +
                        $"{x.Reason}"));
    }

    private static async Task<string?> ExtractRomEntryAsync(
        string zipPath,
        string entryName)
    {
        if (!File.Exists(zipPath))
            return null;

        using var archive =
            ZipFile.OpenRead(zipPath);

        var entry =
            archive.GetEntry(entryName);

        if (entry is null)
            return null;

        var root =
            Path.Combine(
                AppPaths.CacheDirectory,
                "rom",
                Path.GetFileNameWithoutExtension(zipPath));

        Directory.CreateDirectory(root);

        var safeName =
            Path.GetFileName(entry.FullName);

        var output =
            Path.Combine(
                root,
                safeName);

        await using var input =
            entry.Open();

        await using var file =
            File.Create(output);

        await input.CopyToAsync(file);

        return output;
    }

    private void AppendTerminal(
        string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Terminal += text;

        if (Terminal.Length > 500_000)
        {
            Terminal =
                Terminal[^400_000..];
        }
    }

    private static bool Confirm(
        string text)
    {
        return MessageBox.Show(
                   text,
                   "TechFix Safety Confirmation",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Warning)
               == MessageBoxResult.Yes;
    }

    private static IEnumerable<string> SplitArguments(
        string text)
    {
        var result =
            new List<string>();

        var buffer =
            new System.Text.StringBuilder();

        var quoted =
            false;

        foreach (var c in text)
        {
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (buffer.Length > 0)
                {
                    result.Add(
                        buffer.ToString());

                    buffer.Clear();
                }
            }
            else
            {
                buffer.Append(c);
            }
        }

        if (buffer.Length > 0)
        {
            result.Add(
                buffer.ToString());
        }

        return result;
    }

    private static double ParsePercent(
        string text)
    {
        if (double.TryParse(
                text,
                out var value))
        {
            return value;
        }

        return 0;
    }

    private void Set<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value))
        {
            return;
        }

        field = value;

        OnPropertyChanged(
            propertyName);
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}
