using ADTool.Models;
using ADTool.Services;
using ADTool.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace ADTool.ViewModels;

public class Step1InputViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly CsvImportService _csvService;
    private readonly IAdService _adService;
    private readonly Action _onNext;
    private string _oldSuffix = string.Empty;
    private string _newSuffix = string.Empty;

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

    public string OldSuffix
    {
        get => _oldSuffix;
        set { SetField(ref _oldSuffix, value); ApplySuffixSwapCommand.RaiseCanExecuteChanged(); }
    }

    public string NewSuffix
    {
        get => _newSuffix;
        set { SetField(ref _newSuffix, value); ApplySuffixSwapCommand.RaiseCanExecuteChanged(); }
    }

    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand OpenAdBrowserCommand { get; }
    public RelayCommand ApplySuffixSwapCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand<UPNChangeEntry> DeleteRowCommand { get; }
    public RelayCommand NextCommand { get; }

    public Step1InputViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        CsvImportService csvService,
        IAdService adService,
        Action onNext)
    {
        _entries = entries;
        _csvService = csvService;
        _adService = adService;
        _onNext = onNext;

        ImportCsvCommand = new RelayCommand(ImportCsv);
        OpenAdBrowserCommand = new RelayCommand(OpenAdBrowser);
        ApplySuffixSwapCommand = new RelayCommand(ApplySuffixSwap, CanApplySuffixSwap);
        AddRowCommand = new RelayCommand(() => _entries.Add(new UPNChangeEntry()));
        DeleteRowCommand = new RelayCommand<UPNChangeEntry>(e => { if (e != null) _entries.Remove(e); });
        NextCommand = new RelayCommand(Next, () => _entries.Count > 0);

        _entries.CollectionChanged += (_, _) => NextCommand.RaiseCanExecuteChanged();
    }

    private bool CanApplySuffixSwap() =>
        !string.IsNullOrWhiteSpace(_oldSuffix) && !string.IsNullOrWhiteSpace(_newSuffix);

    private void ApplySuffixSwap()
    {
        foreach (var entry in _entries)
            if (entry.OldUPN.EndsWith(_oldSuffix, StringComparison.OrdinalIgnoreCase))
                entry.NewUPN = entry.OldUPN[..^_oldSuffix.Length] + _newSuffix;
    }

    private void ImportCsv()
    {
        var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dlg.ShowDialog() != true) return;

        var existing = _entries.Select(e => e.OldUPN);
        var result = _csvService.Import(dlg.FileName, existing);

        foreach (var (oldUpn, newUpn) in result.Rows)
            _entries.Add(new UPNChangeEntry { OldUPN = oldUpn, NewUPN = newUpn });

        if (result.Errors.Count > 0)
            MessageBox.Show(string.Join("\n", result.Errors), "Import warnings",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OpenAdBrowser()
    {
        var vm = new AdBrowserViewModel(_adService, AddUsersFromBrowser);
        var dialog = new AdBrowserDialog(vm) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    private void AddUsersFromBrowser(IReadOnlyList<AdUser> users)
    {
        var existingUpns = new HashSet<string>(_entries.Select(e => e.OldUPN), StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            if (existingUpns.Contains(user.UPN)) continue;
            _entries.Add(new UPNChangeEntry { OldUPN = user.UPN, NewUPN = string.Empty });
            existingUpns.Add(user.UPN);
        }
    }

    private void Next()
    {
        foreach (var e in _entries)
        {
            e.ValidationStatus = ValidationStatus.Pending;
            e.ErrorTitle = null;
            e.ErrorDetail = null;
        }
        _onNext();
    }
}
