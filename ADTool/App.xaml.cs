using ADTool.Services;
using ADTool.ViewModels;
using ADTool.Views;
using System.Windows;

namespace ADTool;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool dryRun = e.Args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        IAdService adService = dryRun ? new AdServiceStub() : new AdService();

        if (!dryRun)
        {
            bool isAdmin = await adService.CheckIsDomainAdminAsync();
            if (!isAdmin)
            {
                MessageBox.Show(
                    "This tool requires Domain Admin privileges.\n\nYour account is not a member of the Domain Admins group.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        var mainVm = new MainViewModel(adService, new CsvImportService());
        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}


