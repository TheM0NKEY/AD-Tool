namespace ADTool.ViewModels;

public class HomeViewModel : BaseViewModel
{
    public RelayCommand LaunchUPNModifierCommand { get; }
    public RelayCommand LaunchAttributeEditorCommand { get; }

    public HomeViewModel(Action launchUPN, Action launchAttributeEditor)
    {
        LaunchUPNModifierCommand = new RelayCommand(launchUPN);
        LaunchAttributeEditorCommand = new RelayCommand(launchAttributeEditor);
    }
}
