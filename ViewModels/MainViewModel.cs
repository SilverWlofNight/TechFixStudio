using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using TechFixStudio.Infrastructure;
using TechFixStudio.Models;
using TechFixStudio.Services;

namespace TechFixStudio.ViewModels;

public sealed class MainViewModel :
    INotifyPropertyChanged
{
    private readonly AndroidService _android = new();
    private readonly AndroidDiagnosticsService _diagnostics = new();
    private readonly MagiskService _magisk = new();
    private readonly FastbootService _fastboot = new();
    private readonly FlashQueueService _queue = new();
    private readonly FactoryImageService _factory = new();
    private readonly FlashPlanService _flashPlan = new();
    private readonly WindowsRepairService _repair = new();
    private readonly SshService _ssh = new();
    private readonly HistoryService _history = new();
    private readonly AuditService _audit = new();
    private readonly RiskEngine _risk = new();
    private readonly RomCatalogService _roms = new();
    private readonly ToolLocator _tools = new();

    private string _status =
        "Initializing...";

    private string _clock = "";

    private string _terminal =
        "TECHFIX STUDIO // REAL PROCESS TERMINAL\r\n";

    private string _command = "";

    private string _factoryText =
        "No package loaded.";

    private string _planText =
        "No flash plan.";

    private string _sshOutput =
        "SSH ready.";

    private string _sshHost = "";

    private string _sshUser = "";

    private string _sshCommand =
        "uname -a";

    private string _flashPath = "";

    private string _flashPartition =
        "boot";

    private string _flashHash = "";

    private string _flashStatus =
        "Idle";

    private string _logcat = "";

    private string _magiskText =
        "Magisk status not loaded.";

    private double _cpu;
    private double _ram;
    private double _battery;
    private double _batteryTemp;

    private DeviceInfo? _selectedDevice;

    private AndroidDiagnostics? _androidDiagnostics;

    private MagiskInfo? _magiskInfo;

    private RomPackage? _currentRom;

    private FlashPlan? _currentPlan;

    public event PropertyChangedEventHandler?
        PropertyChanged;

    public string Status
    {
        get => _status;
        set
        {
            if (Set(
                    ref _status,
                    value))
            {
                OnPropertyChanged(
                    nameof(StatusText));
            }
        }
    }

    public string Clock
    {
        get => _clock;
        set => Set(
            ref _clock,
            value);
    }

    public string Terminal
    {
        get => _terminal;
        set
        {
            if (Set(
                    ref _terminal,
                    value))
            {
                OnPropertyChanged(
                    nameof(TerminalOutput));
            }
        }
    }

    public string Command
    {
        get => _command;
        set
        {
            if (Set(
                    ref _command,
                    value))
            {
                OnPropertyChanged(
                    nameof(TerminalInput));
            }
        }
    }

    // 兼容现有 XAML
    public string StatusText =>
        Status;

    // 兼容现有 XAML
    public string TerminalInput
    {
        get => Command;
        set => Command = value;
    }

    // 兼容现有 XAML
    public string TerminalOutput =>
        Terminal;

    public string FactoryText
    {
        get => _factoryText;
        set => Set(
            ref _factoryText,
            value);
    }

    public string PlanText
    {
        get => _planText;
        set => Set(
            ref _planText,
            value);
    }

    public string SshOutput
    {
        get => _sshOutput;
        set => Set(
            ref _sshOutput,
            value);
    }

    public string SshHost
    {
        get => _sshHost;
        set => Set(
            ref _sshHost,
            value);
    }

    public string SshUser
    {
        get => _sshUser;
        set => Set(
            ref _sshUser,
            value);
    }

    public string SshCommand
    {
        get => _sshCommand;
        set => Set(
            ref _sshCommand,
            value);
    }

    public string FlashPath
    {
        get => _flashPath;
        set => Set(
            ref _flashPath,
            value);
    }

    public string FlashPartition
    {
        get => _flashPartition;
        set => Set(
            ref _flashPartition,
            value);
    }

    public string FlashHash
    {
        get => _flashHash;
        set => Set(
            ref _flashHash,
            value);
    }

    public string FlashStatus
    {
        get => _flashStatus;
        set => Set(
            ref _flashStatus,
            value);
    }

    public string Logcat
    {
        get => _logcat;
        set => Set(
            ref _logcat,
            value);
    }

    public string MagiskText
    {
        get => _magiskText;
        set => Set(
            ref _magiskText,
            value);
    }

    public double Cpu
    {
        get => _cpu;
        set => Set(
            ref _cpu,
            value);
    }

    public double Ram
    {
        get => _ram;
        set => Set(
            ref _ram,
            value);
    }

    public double Battery
    {
        get => _battery;
        set => Set(
            ref _battery,
            value);
    }

    public double BatteryTemp
    {
        get => _batteryTemp;
        set => Set(
            ref _batteryTemp,
            value);
    }

    public DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set => Set(
            ref _selectedDevice,
            value);
    }

    public AndroidDiagnostics?
        AndroidDiagnostics
    {
        get => _androidDiagnostics;
        private set => Set(
            ref _androidDiagnostics,
            value);
    }

    public MagiskInfo? MagiskInfo
    {
        get => _magiskInfo;
        private set => Set(
            ref _magiskInfo,
            value);
    }

    public RomPackage? CurrentRom
    {
        get => _currentRom;
        private set => Set(
            ref _currentRom,
            value);
    }

    public FlashPlan? CurrentPlan
    {
        get => _currentPlan;
        private set => Set(
            ref _currentPlan,
            value);
    }

    public ObservableCollection<DeviceInfo>
        Devices { get; } = new();

    public ObservableCollection<FlashTask>
        FlashQueue =>
        _queue.Items;

    // 兼容旧 XAML
    public ObservableCollection<FlashTask>
        FlashTasks =>
        _queue.Items;

    public IReadOnlyList<string>
        Partitions { get; } =
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
        AppPaths.Ensure();

        var timer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromSeconds(1)
            };

        timer.Tick +=
            async (_, _) =>
            {
                Clock =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss");

                try
                {
                    if (SelectedDevice?.Transport ==
                        Transport.Adb)
                    {
                        var telemetry =
                            await _android.TelemetryAsync(
                                SelectedDevice);

                        if (telemetry is not null)
                        {
                            Battery =
                                telemetry.Battery;

                            BatteryTemp =
                                telemetry.Temperature;

                            Cpu =
                                0;

                            Ram =
                                0;
                        }
                    }
                }
                catch
                {
                    // Telemetry failure should not kill UI timer.
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

            await _roms.SeedDefaultsAsync();

            Status =
                "TechFix Studio ready.";
        }
        catch (Exception ex)
        {
            Status =
                "Initialization error: " +
                ex.Message;
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            Devices.Clear();

            var adbDevices =
                await _android.ScanAsync();

            foreach (var device in adbDevices)
            {
                Devices.Add(device);
            }

            var fastbootDevices =
                await _fastboot.DevicesAsync();

            foreach (var serial in fastbootDevices)
            {
                if (Devices.Any(
                        x => x.Serial == serial &&
                             x.Transport ==
                             Transport.Fastboot))
                {
                    continue;
                }

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

            if (SelectedDevice is null ||
                !Devices.Contains(
                    SelectedDevice))
            {
                SelectedDevice =
                    Devices.FirstOrDefault();
            }

            Status =
                $"Detected {Devices.Count} device(s) | " +
                $"ADB: {(_tools.Adb is null ? "MISSING" : "OK")} | " +
                $"Fastboot: {(_tools.Fastboot is null ? "MISSING" : "OK")}";
        }
        catch (Exception ex)
        {
            Status =
                "Device scan failed: " +
                ex.Message;
        }
    }

    public async Task RunCommandAsync()
    {
        var command =
            Command.Trim();

        if (command.Length == 0)
        {
            return;
        }

        var risk =
            _risk.Assess(command);

        AppendTerminal(
            $"\r\n> {command}\r\n");

        if (risk >= RiskLevel.High)
        {
            AppendTerminal(
                $"BLOCKED [{risk}] " +
                "Use the dedicated safety workflow.\r\n");

            await _audit.WriteAsync(
                "blocked-command",
                command,
                risk,
                false,
                "blocked");

            return;
        }

        if (command.Equals(
                "adb",
                StringComparison.OrdinalIgnoreCase) ||
            command.StartsWith(
                "adb ",
                StringComparison.OrdinalIgnoreCase))
        {
            await RunAdbCommandAsync(
                command);

            return;
        }

        if (command.Equals(
                "fastboot",
                StringComparison.OrdinalIgnoreCase) ||
            command.StartsWith(
                "fastboot ",
                StringComparison.OrdinalIgnoreCase))
        {
            await RunFastbootCommandAsync(
                command);

            return;
        }

        AppendTerminal(
            "No registered executor for this command.\r\n");
    }

    private async Task RunAdbCommandAsync(
        string command)
    {
        if (_tools.Adb is null)
        {
            AppendTerminal(
                "adb executable not found.\r\n");

            return;
        }

        var raw =
            command.Length > 3
                ? command[3..].Trim()
                : "";

        var args =
            SplitArguments(raw);

        var progress =
            new Progress<string>(
                line =>
                {
                    AppendTerminal(
                        line +
                        Environment.NewLine);
                });

        var result =
            await new ProcessRunner().RunAsync(
                _tools.Adb,
                args,
                TimeSpan.FromMinutes(5),
                CancellationToken.None,
                progress);

        if (!string.IsNullOrWhiteSpace(
                result.StdOut))
        {
            AppendTerminal(
                result.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(
                result.StdErr))
        {
            AppendTerminal(
                result.StdErr);
        }

        await _audit.WriteAsync(
            "adb",
            command,
            RiskLevel.Low,
            result.ExitCode == 0,
            result.ExitCode.ToString());
    }

    private async Task RunFastbootCommandAsync(
        string command)
    {
        if (_tools.Fastboot is null)
        {
            AppendTerminal(
                "fastboot executable not found.\r\n");

            return;
        }

        var raw =
            command.Length > 8
                ? command[8..].Trim()
                : "";

        var args =
            SplitArguments(raw);

        var progress =
            new Progress<string>(
                line =>
                {
                    AppendTerminal(
                        line +
                        Environment.NewLine);
                });

        var result =
            await new ProcessRunner().RunAsync(
                _tools.Fastboot,
                args,
                TimeSpan.FromMinutes(10),
                CancellationToken.None,
                progress);

        if (!string.IsNullOrWhiteSpace(
                result.StdOut))
        {
            AppendTerminal(
                result.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(
                result.StdErr))
        {
            AppendTerminal(
                result.StdErr);
        }

        await _audit.WriteAsync(
            "fastboot",
            command,
            RiskLevel.Medium,
            result.ExitCode == 0,
            result.ExitCode.ToString());
    }

    public async Task DiagnoseAndroidAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Adb)
        {
            Status =
                "Select an ADB Android device.";

            return;
        }

        Status =
            "Collecting Android diagnostics...";

        try
        {
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

            Status =
                $"Diagnostics ready: " +
                $"{AndroidDiagnostics.Manufacturer} " +
                $"{AndroidDiagnostics.Model}";
        }
        catch (Exception ex)
        {
            Status =
                "Diagnostics error: " +
                ex.Message;
        }
    }

    public async Task LoadLogcatAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Adb)
        {
            Logcat =
                "Select an ADB device.";

            return;
        }

        try
        {
            Logcat =
                await _android.LogcatAsync(
                    SelectedDevice.Serial);
        }
        catch (Exception ex)
        {
            Logcat =
                "Logcat error:\r\n" +
                ex.Message;
        }
    }

    public async Task RebootBootloaderAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Adb)
        {
            return;
        }

        if (!Confirm(
                "Reboot the selected Android device into Bootloader?"))
        {
            return;
        }

        var result =
            await _android.RebootAsync(
                SelectedDevice.Serial,
                "bootloader");

        AppendTerminal(
            result.StdOut +
            result.StdErr);
    }

    public async Task RebootRecoveryAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Adb)
        {
            return;
        }

        if (!Confirm(
                "Reboot the selected Android device into Recovery?"))
        {
            return;
        }

        var result =
            await _android.RebootAsync(
                SelectedDevice.Serial,
                "recovery");

        AppendTerminal(
            result.StdOut +
            result.StdErr);
    }

    public async Task InspectMagiskAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Adb)
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

    // 兼容 MainWindow.xaml.cs
    public Task InspectRootMagiskAsync()
    {
        return InspectMagiskAsync();
    }

    public async Task RunSfcAsync()
    {
        if (!Confirm(
                "Run SFC /scannow?"))
        {
            return;
        }

        var result =
            await _repair.RunSfcAsync();

        AppendTerminal(
            result.StdOut +
            result.StdErr);

        await SaveHistory(
            "SFC",
            result);
    }

    public async Task RunDismCheckAsync()
    {
        if (!Confirm(
                "Run DISM CheckHealth?"))
        {
            return;
        }

        var result =
            await _repair.CheckHealthAsync();

        AppendTerminal(
            result.StdOut +
            result.StdErr);

        await SaveHistory(
            "DISM CheckHealth",
            result);
    }

    public async Task RunDismAsync()
    {
        if (!Confirm(
                "Run DISM RestoreHealth?"))
        {
            return;
        }

        var result =
            await _repair.RestoreHealthAsync();

        AppendTerminal(
            result.StdOut +
            result.StdErr);

        await SaveHistory(
            "DISM RestoreHealth",
            result);
    }

    public async Task InspectFactoryAsync(
        string path)
    {
        try
        {
            var package =
                await _factory.InspectAsync(
                    path);

            CurrentRom =
                package;

            FactoryText =
                $"{package.Name}\r\n" +
                $"Product: {package.Product}\r\n" +
                $"Build: {package.Build}\r\n" +
                $"Region: {package.Region}\r\n" +
                $"Images: {package.Images.Count}\r\n" +
                $"Scripts: {string.Join(", ", package.Scripts)}\r\n\r\n" +
                string.Join(
                    "\r\n",
                    package.Images.Select(
                        x =>
                            $"{x.Partition,-16} " +
                            $"{x.Size,14} bytes  " +
                            $"SHA {x.Sha256}" +
                            (x.Critical
                                ? "  [CRITICAL]"
                                : "")));

            FlashPath =
                path;

            FlashHash =
                package.Images
                    .FirstOrDefault()
                    ?.Sha256
                    ?? "";
        }
        catch (Exception ex)
        {
            FactoryText =
                "Inspection failed:\r\n" +
                ex.Message;
        }
    }

    public async Task BuildFlashPlanAsync()
    {
        if (CurrentRom is null)
        {
            PlanText =
                "Load a ROM/factory package first.";

            return;
        }

        if (SelectedDevice?.Transport !=
            Transport.Fastboot)
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
            $"Serial: {CurrentPlan.Serial}\r\n" +
            $"Product: {CurrentPlan.Product}\r\n" +
            $"Slot: {CurrentPlan.CurrentSlot}\r\n" +
            $"Unlocked: {CurrentPlan.Unlocked}\r\n" +
            $"Secure: {CurrentPlan.Secure}\r\n" +
            $"Anti-Rollback: {CurrentPlan.AntiRollback}\r\n" +
            $"Safe: {CurrentPlan.SafeToProceed}\r\n\r\n" +
            "WARNINGS:\r\n" +
            string.Join(
                "\r\n",
                CurrentPlan.Warnings.Select(
                    x => " - " + x)) +
            "\r\n\r\nITEMS:\r\n" +
            string.Join(
                "\r\n",
                CurrentPlan.Items.Select(
                    x =>
                        $"{x.Partition,-16} " +
                        $"{(x.Allowed ? "ALLOWED" : "BLOCKED"),-8} " +
                        $"{x.Reason}");
    }

    public async Task AddTaskAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Fastboot)
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

        if (!File.Exists(
                FlashPath))
        {
            FlashStatus =
                "Select an image.";

            return;
        }

        if (FlashHash.Length != 64 ||
            FlashHash.Any(
                c => !Uri.IsHexDigit(c)))
        {
            FlashStatus =
                "SHA-256 must be 64 hex characters.";

            return;
        }

        var file =
            new FileInfo(
                FlashPath);

        var task =
            new FlashTask
            {
                Id =
                    Guid.NewGuid().ToString("N"),

                Serial =
                    SelectedDevice.Serial,

                Partition =
                    FlashPartition.Trim(),

                ImagePath =
                    Path.GetFullPath(
                        FlashPath),

                Sha256 =
                    FlashHash.ToLowerInvariant(),

                Size =
                    file.Length,

                State =
                    TaskState.Queued,

                Message =
                    "Queued",

                Progress =
                    0,

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

        OnPropertyChanged(
            nameof(FlashQueue));

        OnPropertyChanged(
            nameof(FlashTasks));

        FlashStatus =
            "Task queued.";
    }

    public async Task RunQueueAsync()
    {
        if (!Confirm(
                "Start the REAL fastboot flash queue?\r\n\r\n" +
                "Every image will be checked and " +
                "each task requires individual confirmation."))
        {
            return;
        }

        await _queue.RunAsync(
            async item =>
            {
                return await Application.Current
                    .Dispatcher
                    .InvokeAsync(
                        () =>
                            Confirm(
                                $"FLASH {item.Partition}?\r\n\r\n" +
                                $"Device: {item.Serial}\r\n" +
                                $"Image: {Path.GetFileName(item.ImagePath)}\r\n" +
                                $"SHA-256: {item.Sha256}\r\n\r\n" +
                                (item.Critical
                                    ? "WARNING: CRITICAL PARTITION\r\n"
                                    : "")))
                    .Task;
            },
            item =>
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        FlashStatus =
                            $"{item.State}: {item.Message}";

                        OnPropertyChanged(
                            nameof(FlashQueue));

                        OnPropertyChanged(
                            nameof(FlashTasks));
                    });
            });
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
        {
            return;
        }

        _queue.Remove(
            task.Id);

        await _queue.SaveAsync();

        OnPropertyChanged(
            nameof(FlashQueue));

        OnPropertyChanged(
            nameof(FlashTasks));
    }

    public async Task FastbootInfoAsync()
    {
        if (SelectedDevice?.Transport !=
            Transport.Fastboot)
        {
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
            info.Raw +
            "\r\n");
    }

    public async Task RunSshAsync()
    {
        if (string.IsNullOrWhiteSpace(
                SshHost))
        {
            SshOutput =
                "SSH host is empty.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                SshUser))
        {
            SshOutput =
                "SSH user is empty.";

            return;
        }

        var risk =
            _risk.Assess(
                "ssh " +
                SshCommand);

        if (risk >= RiskLevel.High)
        {
            SshOutput =
                "Blocked by safety policy.";

            await _audit.WriteAsync(
                "blocked-ssh",
                SshHost,
                risk,
                false,
                "blocked");

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
                result.ExitCode.ToString());
        }
        catch (Exception ex)
        {
            SshOutput =
                "SSH error:\r\n" +
                ex.Message;
        }
    }

    private async Task SaveHistory(
        string action,
        CommandResult result)
    {
        await _history.AddAsync(
            new HistoryEntry(
                DateTime.Now,
                action,
                "local",
                result.ExitCode == 0
                    ? "Success"
                    : "Failed",
                RiskLevel.Medium.ToString(),
                result.StdOut +
                result.StdErr));
    }

    private void AppendTerminal(
        string text)
    {
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

