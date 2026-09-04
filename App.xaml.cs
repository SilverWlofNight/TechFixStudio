using System.Windows;

namespace TechFixStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Infrastructure.AppPaths.EnsureDirectories();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"无法初始化 TechFix Studio 工作目录。\n\n{ex.Message}",
                "TechFix Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }
}