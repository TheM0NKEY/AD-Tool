using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace ADTool.ViewModels;

public class SelectableAdUser : INotifyPropertyChanged
{
    private bool _isSelected;

    public AdUser User { get; }
    public string UPN => User.UPN;
    public string DisplayName => User.DisplayName;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public SelectableAdUser(AdUser user) => User = user;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class AdBrowserViewModel : BaseViewModel
{
    private readonly IAdService _adService;
    private readonly Action<IReadOnlyList<AdUser>> _onAddToList;

    private IReadOnlyList<OuNode> _ouNodes = [];
    private OuNode? _selectedOu;
    private ObservableCollection<SelectableAdUser> _users = [];
    private bool _isLoadingTree;
    private bool _isLoadingUsers;

    public IReadOnlyList<OuNode> OuNodes
    {
        get => _ouNodes;
        private set => SetField(ref _ouNodes, value);
    }

    public OuNode? SelectedOu
    {
        get => _selectedOu;
        set
        {
            SetField(ref _selectedOu, value);
            LatestLoadUsersTask = LoadUsersAsync(value);
        }
    }

    public ObservableCollection<SelectableAdUser> Users
    {
        get => _users;
        private set => SetField(ref _users, value);
    }

    public bool IsLoadingTree
    {
        get => _isLoadingTree;
        private set => SetField(ref _isLoadingTree, value);
    }

    public bool IsLoadingUsers
    {
        get => _isLoadingUsers;
        private set => SetField(ref _isLoadingUsers, value);
    }

    public Task LatestLoadUsersTask { get; private set; } = Task.CompletedTask;

    public RelayCommand AddSelectedToListCommand { get; }
    public RelayCommand ExportToCsvCommand { get; }

    public event EventHandler? RequestClose;

    public AdBrowserViewModel(IAdService adService, Action<IReadOnlyList<AdUser>> onAddToList)
    {
        _adService = adService;
        _onAddToList = onAddToList;
        AddSelectedToListCommand = new RelayCommand(AddSelectedToList, () => _users.Any(u => u.IsSelected));
        ExportToCsvCommand = new RelayCommand(ExportToCsv, () => !_isLoadingTree && _users.Count > 0);
    }

    public async Task LoadTreeAsync()
    {
        IsLoadingTree = true;
        OuNodes = await _adService.GetOuTreeAsync();
        IsLoadingTree = false;
        ExportToCsvCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadUsersAsync(OuNode? ou)
    {
        Users = [];
        AddSelectedToListCommand.RaiseCanExecuteChanged();
        ExportToCsvCommand.RaiseCanExecuteChanged();
        if (ou is null) return;

        IsLoadingUsers = true;
        var rawUsers = await _adService.GetUsersInOuAsync(ou.DistinguishedName);
        var selectable = rawUsers.Select(u =>
        {
            var s = new SelectableAdUser(u);
            s.PropertyChanged += (_, _) => AddSelectedToListCommand.RaiseCanExecuteChanged();
            return s;
        });
        Users = new ObservableCollection<SelectableAdUser>(selectable);
        IsLoadingUsers = false;
        ExportToCsvCommand.RaiseCanExecuteChanged();
    }

    private void AddSelectedToList()
    {
        var selected = _users.Where(u => u.IsSelected).Select(u => u.User).ToList();
        _onAddToList(selected);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void ExportToCsv()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"ad-users-{DateTime.Now:yyyy-MM-dd-HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        using var writer = new StreamWriter(dlg.FileName);
        writer.WriteLine("OldUPN,NewUPN,DisplayName");
        foreach (var u in _users)
            writer.WriteLine($"{u.UPN},,{EscapeCsv(u.DisplayName)}");
    }

    private static string EscapeCsv(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
