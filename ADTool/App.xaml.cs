using ADTool.Services;
using ADTool.ViewModels;
using ADTool.Views;
using System.Windows;

namespace ADTool;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool dryRun = e.Args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        IAdService adService = dryRun ? new AdServiceStub() : new AdService();

        var mainVm = new MainViewModel(adService, new CsvImportService());
        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}


