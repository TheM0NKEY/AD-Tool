using ADTool.Models;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step3PreviewViewModel : BaseViewModel
{
    public ObservableCollection<UPNChangeEntry> Entries { get; }

    public RelayCommand BackCommand { get; }
    public RelayCommand ExecuteCommand { get; }

    public Step3PreviewViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        Action onBack,
        Action onExecute)
    {
        Entries = entries;
        BackCommand = new RelayCommand(onBack);
        ExecuteCommand = new RelayCommand(onExecute);
    }
}
