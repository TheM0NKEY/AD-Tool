using ADTool.Services;

namespace ADTool.ViewModels;

public class AppShellViewModel : BaseViewModel
{
    private BaseViewModel _currentView;
    private readonly IAdService _adService;
    private readonly CsvImportService _csvService;

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set
        {
            SetField(ref _currentView, value);
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public string WindowTitle => CurrentView switch
    {
        UPNToolViewModel => "AD Tool — UPN Modifier",
        _                => "AD Tool"
    };

    public RelayCommand LaunchUPNModifierCommand { get; }
    public RelayCommand LaunchAttributeEditorCommand { get; }

    public AppShellViewModel(IAdService adService, CsvImportService csvService)
    {
        _adService = adService;
        _csvService = csvService;
        LaunchUPNModifierCommand = new RelayCommand(LaunchUPNModifier);
        LaunchAttributeEditorCommand = new RelayCommand(LaunchAttributeEditor);
        _currentView = new HomeViewModel(LaunchUPNModifier, LaunchAttributeEditor);
    }

    public void ReturnHome()
    {
        CurrentView = new HomeViewModel(LaunchUPNModifier, LaunchAttributeEditor);
    }

    private void LaunchUPNModifier()
    {
        CurrentView = new UPNToolViewModel(_adService, _csvService, ReturnHome);
    }

    private void LaunchAttributeEditor()
    {
        // Wired in Task 8 — AttributeToolViewModel not yet available
    }
}
