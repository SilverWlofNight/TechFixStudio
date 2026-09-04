using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using TechFixStudio.Models;
using TechFixStudio.Services;

namespace TechFixStudio.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator _tools;
    private readonly AndroidService _android;
    private readonly FastbootService _fastboot;
    private readonly RiskEngine _risk;
    private readonly AuditService _audit;
    private readonly WindowsRepairService _windowsRepair;
    private readonly Sha256Service _sha256;
    private readonly HistoryService _history;
    private readonly FlashQueueService _flashQueue;

    private string _terminalInput = string.Empty;
    private string _terminalOutput = string.Empty;
    private string _statusText = "READY";
    private DeviceInfo? _selectedDevice;

    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public ObservableCollection<FlashTask> FlashTasks { get; } = [];

    public string TerminalInput
    {
        get => _terminalInput;
        set
        {
            _terminalInput = value;
            OnPropertyChanged();
        }
    }

    public string TerminalOutput
    {
        get => _terminalOutput;
        private set
        {
            _terminalOutput = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            _selectedDevice = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        _runner = new ProcessRunner();
        _tools = new ToolLocator();

        _risk = new RiskEngine();
        _audit = new AuditService();

        _android =
            new AndroidService(
                _runner,
                _tools);

        _fastboot =
            new FastbootService(
                _runner,
                _tools);

        _windowsRepair =
            new WindowsRepairService(
                _runner);

        _sha256 =
            new Sha256Service();

        var store =
            new JsonStore();

        _history =
            new HistoryService(store);

        _flashQueue =
            new FlashQueueService(
                _runner,
                _tools,
                _sha256,
                _audit,
                _history);
    }

    public async Task RefreshDevicesAsync()
    {
        StatusText =
            "SCANNING DEVICES...";

        Devices.Clear();

        var adbDevices =
            await _android.GetDevicesAsync();

        foreach (var device in adbDevices)
        {
            Devices.Add(device);
        }

        var fastbootDevices =
            await _fastboot.GetDevicesAsync();

        foreach (var device in fastbootDevices)
        {
            Devices.Add(device);
        }

        StatusText =
            $"READY • {Devices.Count} DEVICE(S)";
    }

    public async Task RunTerminalAsync()
    {
        var command =
            TerminalInput.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        var risk =
            _risk.Assess(command);

        if (risk is
            RiskLevel.High or
            RiskLevel.Critical)
        {
            MessageBox.Show(
                $"该命令风险等级：{risk}\n\n" +
                "为了避免误操作，普通终端不会直接执行高危命令。\n" +
                "请使用对应的专用维修工作流。",
                "TechFix Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            await _audit.WriteAsync(
                "TerminalBlocked",
                command,
                risk,
                false,
                "高风险命令被普通终端阻止。");

            return;
        }

        var args =
            SplitArguments(command);

        if (args.Count == 0)
        {
            return;
        }

        var executable =
            args[0].ToLowerInvariant();

        if (executable == "adb")
        {
            if (_tools.AdbPath is null)
            {
                AppendTerminal(
                    "ERROR: 未找到 adb.exe");
                return;
            }

            args.RemoveAt(0);

            var result =
                await _runner.RunAsync(
                    _tools.AdbPath,
                    args,
                    TimeSpan.FromMinutes(5));

            AppendTerminal(
                result.StdOut +
                Environment.NewLine +
                result.StdErr);

            await _audit.WriteAsync(
                "ADB",
                command,
                risk,
                result.Success,
                result.StdOut + result.StdErr);

            return;
        }

        if (executable == "fastboot")
        {
            AppendTerminal(
                "Fastboot 命令请通过 Fastboot 专用工作流执行。");

            return;
        }

        AppendTerminal(
            "仅允许注册的 ADB/Fastboot 执行器。\n" +
            "其他系统命令请使用对应的维修模块。");
    }

    public async Task RunSfcAsync()
    {
        var answer =
            MessageBox.Show(
                "SFC 将扫描并尝试修复受保护的 Windows 系统文件。\n\n是否继续？",
                "Windows Repair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        StatusText =
            "SFC RUNNING...";

        var result =
            await _windowsRepair.RunSfcAsync();

        AppendTerminal(
            result.StdOut +
            Environment.NewLine +
            result.StdErr);

        StatusText =
            result.Success
                ? "SFC COMPLETED"
                : "SFC FAILED";

        await _audit.WriteAsync(
            "SFC",
            "Windows",
            RiskLevel.Medium,
            result.Success,
            result.StdOut + result.StdErr);

        await _history.AddAsync(
            "SFC",
            "Windows",
            result.Success
                ? "Succeeded"
                : "Failed",
            result.StdOut + result.StdErr);
    }

    public async Task RunDismAsync()
    {
        var answer =
            MessageBox.Show(
                "DISM RestoreHealth 可能需要较长时间并访问 Windows Update。\n\n是否继续？",
                "Windows Repair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        StatusText =
            "DISM RUNNING...";

        var result =
            await _windowsRepair
                .RestoreHealthAsync();

        AppendTerminal(
            result.StdOut +
            Environment.NewLine +
            result.StdErr);

        StatusText =
            result.Success
                ? "DISM COMPLETED"
                : "DISM FAILED";

        await _audit.WriteAsync(
            "DISM RestoreHealth",
            "Windows",
            RiskLevel.Medium,
            result.Success,
            result.StdOut + result.StdErr);

        await _history.AddAsync(
            "DISM",
            "Windows",
            result.Success
                ? "Succeeded"
                : "Failed",
            result.StdOut + result.StdErr);
    }

    public async Task AddFlashTaskAsync()
    {
        if (SelectedDevice is null ||
            SelectedDevice.Transport !=
            TransportType.Fastboot)
        {
            MessageBox.Show(
                "请先选择一个 Fastboot 设备。",
                "Flash Center",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var dialog =
            new OpenFileDialog
            {
                Title = "选择 Android 镜像",
                Filter =
                    "Android Image (*.img;*.bin)|*.img;*.bin|" +
                    "All files (*.*)|*.*"
            };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var partition =
            PromptPartition();

        if (string.IsNullOrWhiteSpace(
                partition))
        {
            return;
        }

        var task =
            new FlashTask
            {
                Serial =
                    SelectedDevice.Serial,
                Partition =
                    partition,
                ImagePath =
                    dialog.FileName,
                State =
                    FlashTaskState.Queued,
                Message =
                    "等待执行"
            };

        await _flashQueue.AddAsync(task);

        await LoadFlashTasksAsync();

        StatusText =
            "FLASH TASK ADDED";
    }

    public async Task LoadFlashTasksAsync()
    {
        FlashTasks.Clear();

        var tasks =
            await _flashQueue.LoadAsync();

        foreach (var task in tasks)
        {
            FlashTasks.Add(task);
        }
    }

    public async Task ExecuteFlashTaskAsync(
        FlashTask task)
    {
        await _flashQueue.ExecuteAsync(
            task.Id,
            async selectedTask =>
            {
                var answer =
                    MessageBox.Show(
                        $"即将执行真实 Fastboot 刷写：\n\n" +
                        $"设备：{selectedTask.Serial}\n" +
                        $"分区：{selectedTask.Partition}\n" +
                        $"镜像：{selectedTask.ImagePath}\n" +
                        $"SHA-256：{selectedTask.ActualSha256}\n\n" +
                        "请确认设备与分区完全正确。\n" +
                        "继续操作可能导致设备无法启动。\n\n" +
                        "确定执行？",
                        "CRITICAL FLASH CONFIRMATION",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                return await Task.FromResult(
                    answer == MessageBoxResult.Yes);
            });

        await LoadFlashTasksAsync();
    }

    private static string PromptPartition()
    {
        var dialog =
            new Window
            {
                Title = "选择目标分区",
                Width = 420,
                Height = 220,
                WindowStartupLocation =
                    WindowStartupLocation.CenterScreen,
                ResizeMode =
                    ResizeMode.NoResize
            };

        var combo =
            new System.Windows.Controls.ComboBox
            {
                ItemsSource = new[]
                {
                    "boot",
                    "init_boot",
                    "vendor_boot",
                    "dtbo",
                    "vbmeta",
                    "recovery"
                },
                SelectedIndex = 0,
                Margin = new Thickness(20)
            };

        var button =
            new System.Windows.Controls.Button
            {
                Content = "确认",
                Width = 100,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                Margin =
                    new Thickness(20)
            };

        button.Click += (_, _) =>
        {
            dialog.DialogResult = true;
            dialog.Close();
        };

        var panel =
            new System.Windows.Controls.StackPanel();

        panel.Children.Add(combo);
        panel.Children.Add(button);

        dialog.Content = panel;

        return dialog.ShowDialog() == true
            ? combo.SelectedItem?.ToString()
                ?? string.Empty
            : string.Empty;
    }

    private void AppendTerminal(
        string text)
    {
        TerminalOutput +=
            $"[{DateTime.Now:HH:mm:ss}] {text}" +
            Environment.NewLine;
    }

    private static List<string> SplitArguments(
        string command)
    {
        var result = new List<string>();

        var current = new System.Text.StringBuilder();

        var quoted = false;

        foreach (var character in command)
        {
            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(character) &&
                !quoted)
            {
                if (current.Length > 0)
                {
                    result.Add(
                        current.ToString());

                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            result.Add(
                current.ToString());
        }

        return result;
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}





