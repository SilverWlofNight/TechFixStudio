using System.Windows;
using TechFixStudio.Models;
using TechFixStudio.ViewModels;

namespace TechFixStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private MainViewModel VM => (MainViewModel)DataContext;

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        var page = (sender as FrameworkElement)?.Tag?.ToString() ?? "Dashboard";

        DashboardPage.Visibility = page == "Dashboard"
            ? Visibility.Visible
            : Visibility.Collapsed;

        AndroidPage.Visibility = page == "Android"
            ? Visibility.Visible
            : Visibility.Collapsed;

        FlashPage.Visibility = page == "Flash"
            ? Visibility.Visible
            : Visibility.Collapsed;

        RootPage.Visibility = page == "Root"
            ? Visibility.Visible
            : Visibility.Collapsed;

        TerminalPage.Visibility = page == "Terminal"
            ? Visibility.Visible
            : Visibility.Collapsed;

        FastbootPage.Visibility = page == "Fastboot"
            ? Visibility.Visible
            : Visibility.Collapsed;

        WindowsPage.Visibility = page == "Windows"
            ? Visibility.Visible
            : Visibility.Collapsed;

        LinuxPage.Visibility = page == "Linux"
            ? Visibility.Visible
            : Visibility.Collapsed;

        ResourcesPage.Visibility = page == "Resources"
            ? Visibility.Visible
            : Visibility.Collapsed;

        PluginsPage.Visibility = page == "Plugins"
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

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
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter =
                "Factory / ROM|*.zip;*.img;*.bin|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await VM.InspectFactoryAsync(dialog.FileName);
        }
    }

    private void BrowseFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter =
                "Android image|*.img;*.bin|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            VM.FlashPath = dialog.FileName;
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
            (sender as FrameworkElement)?.DataContext as FlashTask);
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
        await VM.InspectMagiskAsync();
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
}
