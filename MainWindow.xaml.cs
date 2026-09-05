using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TechFixStudio.Models;
using TechFixStudio.ViewModels;

namespace TechFixStudio;

public partial class MainWindow : Window
{
    private MainViewModel VM => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();

        Loaded += MainWindow_Loaded;
    }


    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            WorkspaceTabs.SelectedIndex = 0;

            ModulePathText.Text =
                System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "TechFixStudio",
                    "modules");
        }
        catch
        {
            // UI 初始化失败不应导致程序退出
        }
    }


    // ============================================================
    // NAVIGATION
    // ============================================================

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(0);
    }


    private async void Android_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(1);

        try
        {
            await VM.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError("Android 设备扫描失败", ex);
        }
    }


    private void Flash_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(2);
    }


    private async void Root_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(3);

        try
        {
            await VM.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError("Root / Magisk 页面刷新失败", ex);
        }
    }


    private void Terminal_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(4);
    }


    private async void Fastboot_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(5);

        try
        {
            await VM.FastbootInfoAsync();
        }
        catch (Exception ex)
        {
            ShowError("Fastboot 操作失败", ex);
        }
    }


    private void Windows_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(6);
    }


    private void Ssh_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(7);
    }


    private void Rom_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(8);
    }


    private void Plugin_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(9);
    }


    private void ShowPage(int index)
    {
        if (index < 0)
            index = 0;

        if (index >= WorkspaceTabs.Items.Count)
            index = WorkspaceTabs.Items.Count - 1;

        WorkspaceTabs.SelectedIndex = index;
    }


    // ============================================================
    // DEVICE
    // ============================================================

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError("设备扫描失败", ex);
        }
    }


    // ============================================================
    // ADB TERMINAL
    // ============================================================

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.RunCommandAsync();
        }
        catch (Exception ex)
        {
            ShowError("ADB 命令执行失败", ex);
        }
    }


    // ============================================================
    // WINDOWS REPAIR
    // ============================================================

    private async void Sfc_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.RunSfcAsync();
        }
        catch (Exception ex)
        {
            ShowError("SFC 执行失败", ex);
        }
    }


    private async void DismCheck_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.RunDismCheckAsync();
        }
        catch (Exception ex)
        {
            ShowError("DISM CheckHealth 执行失败", ex);
        }
    }


    private async void Dism_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.RunDismAsync();
        }
        catch (Exception ex)
        {
            ShowError("DISM RestoreHealth 执行失败", ex);
        }
    }


    // ============================================================
    // FACTORY / ROM
    // ============================================================

    private async void BrowseFactory_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 ROM / Factory Image",
                Filter =
                    "ROM / Factory|*.zip;*.img;*.bin|" +
                    "ZIP|*.zip|" +
                    "Android Image|*.img|" +
                    "Binary|*.bin|" +
                    "All Files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                await VM.InspectFactoryAsync(dialog.FileName);

                ShowPage(8);
            }
        }
        catch (Exception ex)
        {
            ShowError("ROM / Factory 镜像分析失败", ex);
        }
    }


    // ============================================================
    // FLASH
    // ============================================================

    private void BrowseFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 Android Image",
                Filter =
                    "Android Image|*.img;*.bin|" +
                    "IMG|*.img|" +
                    "BIN|*.bin|" +
                    "All Files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                VM.FlashPath = dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            ShowError("选择镜像失败", ex);
        }
    }


    private async void AddTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await VM.AddTaskAsync();
        }
        catch (Exception ex)
        {
            ShowError("加入刷机队列失败", ex);
        }
    }


    private async void RunQueue_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await VM.RunQueueAsync();
        }
        catch (Exception ex)
        {
            ShowError("刷机队列执行失败", ex);
        }
    }


    private async void CancelQueue_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await VM.CancelQueueAsync();
        }
        catch (Exception ex)
        {
            ShowError("取消刷机队列失败", ex);
        }
    }


    private async void RemoveTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element &&
                element.DataContext is FlashTask task)
            {
                await VM.RemoveTaskAsync(task);
            }
        }
        catch (Exception ex)
        {
            ShowError("删除刷机任务失败", ex);
        }
    }


    // ============================================================
    // PLUGIN DIRECTORY
    // ============================================================

    private void OpenModules_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            string modulePath =
                System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "TechFixStudio",
                    "modules");

            System.IO.Directory.CreateDirectory(modulePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{modulePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError("无法打开插件目录", ex);
        }
    }


    // ============================================================
    // ERROR HANDLING
    // ============================================================

    private static void ShowError(
        string title,
        Exception ex)
    {
        MessageBox.Show(
            $"{title}\n\n{ex.Message}",
            "TechFix Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
