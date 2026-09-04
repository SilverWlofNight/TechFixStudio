using System.Windows;
using TechFixStudio.Models;
using TechFixStudio.ViewModels;

namespace TechFixStudio;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel =
            new MainViewModel();

        DataContext =
            _viewModel;

        Loaded +=
            MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel
            .RefreshDevicesAsync();

        await _viewModel
            .LoadFlashTasksAsync();
    }

    private async void ScanDevices_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel
            .RefreshDevicesAsync();
    }

    private async void RunTerminal_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel
            .RunTerminalAsync();
    }

    private async void AddFlashTask_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel
            .AddFlashTaskAsync();
    }

    private async void ExecuteFlash_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        if (button.DataContext is not FlashTask task)
        {
            return;
        }

        await _viewModel
            .ExecuteFlashTaskAsync(task);
    }
}





