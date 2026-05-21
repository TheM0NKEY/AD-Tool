using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step4ExecuteViewModel : BaseViewModel
{
    public Step4ExecuteViewModel(ObservableCollection<UPNChangeEntry> entries, IAdService adService, Action onReset) { }
}
