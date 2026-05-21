using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step1InputViewModel : BaseViewModel
{
    public RelayCommand NextCommand { get; }

    public Step1InputViewModel(ObservableCollection<UPNChangeEntry> entries, CsvImportService csvService, Action onNext)
    {
        NextCommand = new RelayCommand(onNext, () => entries.Count > 0);
    }
}
