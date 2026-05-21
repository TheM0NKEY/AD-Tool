using ADTool.Services;

namespace ADTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IAdService _adService;
    private readonly CsvImportService _csvImportService;

    public MainViewModel(IAdService adService, CsvImportService csvImportService)
    {
        _adService = adService;
        _csvImportService = csvImportService;
    }
}
