using System.Windows;
using System.Windows.Controls;
using TechFixStudio.Models;
using TechFixStudio.ViewModels;

namespace TechFixStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext =
            new MainViewModel();
    }

    private MainViewModel VM =>
        (MainViewModel)DataContext;

    // ============================================================
    // Compatibility handlers for current MainWindow.xaml
    // ============================================================

    private async void Scan_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RefreshAsync();
    }

    private void Navigation_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        var tag =
            element.Tag?.ToString();

        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        ShowPage(tag);
    }

    private async void AddFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.AddTaskAsync();
    }

    private async void LoadFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RefreshAsync();
    }

    private async void ExecuteFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunQueueAsync();
    }

    private async void RunTerminal_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunCommandAsync();
    }

    // ============================================================
    // Current/new handlers
    // ============================================================

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RefreshAsync();
    }

    private async void Execute_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunCommandAsync();
    }

    private async void Sfc_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunSfcAsync();
    }

    private async void DismCheck_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunDismCheckAsync();
    }

    private async void Dism_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunDismAsync();
    }

    private async void BrowseFactory_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new Microsoft.Win32.OpenFileDialog
            {
                Filter =
                    "Factory / ROM|*.zip;*.img;*.bin|" +
                    "All files|*.*"
            };

        if (dialog.ShowDialog() == true)
        {
            await VM.InspectFactoryAsync(
                dialog.FileName);
        }
    }

    private void BrowseFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new Microsoft.Win32.OpenFileDialog
            {
                Filter =
                    "Android image|*.img;*.bin|" +
                    "All files|*.*"
            };

        if (dialog.ShowDialog() == true)
        {
            VM.FlashPath =
                dialog.FileName;
        }
    }

    private async void AddTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.AddTaskAsync();
    }

    private async void RunQueue_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunQueueAsync();
    }

    private async void CancelQueue_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.CancelQueueAsync();
    }

    private async void RemoveTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RemoveTaskAsync(
            (sender as FrameworkElement)
                ?.DataContext as FlashTask);
    }

    private async void Fastboot_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.FastbootInfoAsync();
    }

    private async void Ssh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RunSshAsync();
    }

    private async void DiagnoseAndroid_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.DiagnoseAndroidAsync();
    }

    private async void Logcat_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.LoadLogcatAsync();
    }

    private async void RootMagisk_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.InspectRootMagiskAsync();
    }

    private async void RebootBootloader_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RebootBootloaderAsync();
    }

    private async void RebootRecovery_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.RebootRecoveryAsync();
    }

    private async void BuildFlashPlan_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VM.BuildFlashPlanAsync();
    }

    private async void PrepareFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        // 如果当前按钮 DataContext 是 FlashPlanItem，
        // 后续可在这里接入专用镜像准备流程。
        //
        // 当前不执行危险操作，避免误刷。
        if ((sender as FrameworkElement)
            ?.DataContext is FlashPlanItem item)
        {
            MessageBox.Show(
                $"Prepared partition:\r\n\r\n" +
                $"Partition: {item.Partition}\r\n" +
                $"Image: {item.ImagePath}\r\n" +
                $"SHA-256: {item.Sha256}",
                "TechFix Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        await Task.CompletedTask;
    }

    // ============================================================
    // Navigation
    // ============================================================

    private void ShowPage(
        string page)
    {
        // 这里不直接依赖具体控件存在，
        // 避免 UI 名称改变导致启动崩溃。
        //
        // 当前页面切换由 XAML 的可见区域控制。
        //
        // 如果 XAML 中存在以下命名控件，则自动切换。
        SetPageVisibility(
            "Dashboard",
            page.Equals(
                "Dashboard",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Android",
            page.Equals(
                "Android",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Flash",
            page.Equals(
                "Flash",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Root",
            page.Equals(
                "Root",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Terminal",
            page.Equals(
                "Terminal",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Fastboot",
            page.Equals(
                "Fastboot",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Windows",
            page.Equals(
                "Windows",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Linux",
            page.Equals(
                "Linux",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Resources",
            page.Equals(
                "Resources",
                StringComparison.OrdinalIgnoreCase));

        SetPageVisibility(
            "Plugins",
            page.Equals(
                "Plugins",
                StringComparison.OrdinalIgnoreCase));
    }

    private void SetPageVisibility(
        string page,
        bool visible)
    {
        // 当前版本如果 XAML 没有对应命名控件，
        // 直接忽略，不会影响程序启动。
        var name =
            page + "Page";

        var element =
            FindName(name)
            as UIElement;

        if (element is not null)
        {
            element.Visibility =
                visible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}
