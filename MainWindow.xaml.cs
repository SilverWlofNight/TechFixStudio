using System;
using System.Windows;
using System.Windows.Controls;
using TechFixStudio.Models;
using TechFixStudio.ViewModels;

namespace TechFixStudio;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadFlashTasksAsync();
            await _vm.RefreshDevicesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "TechFix Studio 初始化失败：\n\n" +
                ex.Message,
                "TechFix Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Navigation_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var page =
            button.Tag?.ToString()
            ?? "Dashboard";

        ShowPage(page);
    }

    private void ShowPage(string page)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        AndroidPage.Visibility = Visibility.Collapsed;
        FlashPage.Visibility = Visibility.Collapsed;
        RootPage.Visibility = Visibility.Collapsed;
        TerminalPage.Visibility = Visibility.Collapsed;
        FastbootPage.Visibility = Visibility.Collapsed;
        WindowsPage.Visibility = Visibility.Collapsed;
        LinuxPage.Visibility = Visibility.Collapsed;
        ResourcesPage.Visibility = Visibility.Collapsed;
        PluginsPage.Visibility = Visibility.Collapsed;

        switch (page)
        {
            case "Android":
                AndroidPage.Visibility = Visibility.Visible;
                break;

            case "Flash":
                FlashPage.Visibility = Visibility.Visible;
                break;

            case "Root":
                RootPage.Visibility = Visibility.Visible;
                break;

            case "Terminal":
                TerminalPage.Visibility = Visibility.Visible;
                break;

            case "Fastboot":
                FastbootPage.Visibility = Visibility.Visible;
                break;

            case "Windows":
                WindowsPage.Visibility = Visibility.Visible;
                break;

            case "Linux":
                LinuxPage.Visibility = Visibility.Visible;
                break;

            case "Resources":
                ResourcesPage.Visibility = Visibility.Visible;
                break;

            case "Plugins":
                PluginsPage.Visibility = Visibility.Visible;
                break;

            default:
                DashboardPage.Visibility = Visibility.Visible;
                break;
        }
    }

    private async void Scan_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.RefreshDevicesAsync();
        }
        catch (Exception ex)
        {
            ShowError("设备扫描失败", ex);
        }
    }

    private async void RunTerminal_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.RunTerminalAsync();
        }
        catch (Exception ex)
        {
            ShowError("终端执行失败", ex);
        }
    }

    private async void Sfc_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.RunSfcAsync();
        }
        catch (Exception ex)
        {
            ShowError("SFC 执行失败", ex);
        }
    }

    private async void Dism_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.RunDismAsync();
        }
        catch (Exception ex)
        {
            ShowError("DISM 执行失败", ex);
        }
    }

    private async void AddFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.AddFlashTaskAsync();
        }
        catch (Exception ex)
        {
            ShowError("添加刷机任务失败", ex);
        }
    }

    private async void LoadFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadFlashTasksAsync();
        }
        catch (Exception ex)
        {
            ShowError("加载刷机队列失败", ex);
        }
    }

    private async void ExecuteFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (FlashTaskList.SelectedItem is not FlashTask task)
            {
                MessageBox.Show(
                    "请先选择一个刷机任务。",
                    "Flash Center",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            await _vm.ExecuteFlashTaskAsync(task);
        }
        catch (Exception ex)
        {
            ShowError("执行刷机任务失败", ex);
        }
    }

    private static void ShowError(
        string title,
        Exception ex)
    {
        MessageBox.Show(
            $"{title}：\n\n{ex.Message}",
            "TechFix Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

