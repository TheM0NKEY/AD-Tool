using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step2ValidateViewModel : BaseViewModel
{
    public Step2ValidateViewModel(ObservableCollection<UPNChangeEntry> entries, IAdService adService, Action onBack, Action onNext) { }
}
