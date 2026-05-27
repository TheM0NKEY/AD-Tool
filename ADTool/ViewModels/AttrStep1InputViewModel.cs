using ADTool.Models;
using ADTool.Services;
using ADTool.Views;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;

namespace ADTool.ViewModels;

public class AttrStep1InputViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private readonly IAdService _adService;
    private readonly Action _onNext;
    private readonly DataTable _inputTable;

    public DataTable InputTable => _inputTable;

    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand AddColumnCommand { get; }
    public RelayCommand OpenAdBrowserCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand NextCommand { get; }

    public AttrStep1InputViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        IAdService adService,
        Action onNext)
    {
        _entries = entries;
        _adService = adService;
        _onNext = onNext;

        _inputTable = new DataTable();
        _inputTable.Columns.Add("UPN", typeof(string));

        ImportCsvCommand     = new RelayCommand(ImportCsv);
        AddColumnCommand     = new RelayCommand(AddColumn);
        OpenAdBrowserCommand = new RelayCommand(OpenAdBrowser);
        AddRowCommand        = new RelayCommand(AddRow);
        NextCommand          = new RelayCommand(Next, () => _inputTable.Rows.Count > 0);
    }

    public void AddUsersFromBrowser(IReadOnlyList<AdUser> users)
    {
        var existing = new HashSet<string>(
            _inputTable.AsEnumerable().Select(r => r["UPN"]?.ToString() ?? ""),
            StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
        {
            if (existing.Contains(user.UPN)) continue;
            var row = _inputTable.NewRow();
            row["UPN"] = user.UPN;
            _inputTable.Rows.Add(row);
            existing.Add(user.UPN);
        }

        NextCommand.RaiseCanExecuteChanged();
    }

    private void ImportCsv()
    {
        var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var reader = new StreamReader(dlg.FileName, System.Text.Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated   = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];

            int identityIdx = -1;
            var attrCols = new List<(int HeaderIndex, string LdapName)>();

            for (int i = 0; i < headers.Length; i++)
            {
                if (AttributeColumnMap.IdentityHeaders.Contains(headers[i]))
                {
                    identityIdx = i;
                }
                else
                {
                    var ldap = AttributeColumnMap.Resolve(headers[i]);
                    if (ldap != null && !_inputTable.Columns.Contains(ldap))
                    {
                        _inputTable.Columns.Add(ldap, typeof(string));
                        attrCols.Add((i, ldap));
                    }
                }
            }

            if (identityIdx < 0)
            {
                MessageBox.Show("CSV must have a 'UPN' or 'UserPrincipalName' column.",
                    "Import error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            while (csv.Read())
            {
                var upn = csv.GetField(identityIdx)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(upn)) continue;

                var row = _inputTable.NewRow();
                row["UPN"] = upn;
                foreach (var (idx, ldap) in attrCols)
                    row[ldap] = csv.GetField(idx)?.Trim() ?? "";
                _inputTable.Rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import CSV: {ex.Message}", "Import error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        NextCommand.RaiseCanExecuteChanged();
    }

    private void AddColumn()
    {
        var dialog = new AddColumnDialog { Owner = Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true) return;

        foreach (var ldapName in dialog.SelectedLdapNames)
            if (!string.IsNullOrWhiteSpace(ldapName) && !_inputTable.Columns.Contains(ldapName))
                _inputTable.Columns.Add(ldapName, typeof(string));
    }

    private void OpenAdBrowser()
    {
        var vm = new AdBrowserViewModel(_adService, AddUsersFromBrowser);
        var dialog = new AdBrowserDialog(vm) { Owner = Application.Current?.MainWindow };
        dialog.ShowDialog();
    }

    private void AddRow()
    {
        _inputTable.Rows.Add(_inputTable.NewRow());
        NextCommand.RaiseCanExecuteChanged();
    }

    private void Next()
    {
        _entries.Clear();

        foreach (DataRow row in _inputTable.Rows)
        {
            var upn = row["UPN"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(upn)) continue;

            var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in _inputTable.Columns)
            {
                if (col.ColumnName == "UPN") continue;
                var val = row[col]?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    attrs[col.ColumnName] = val;
            }

            _entries.Add(new AttributeChangeEntry
            {
                UserUPN    = upn,
                Attributes = attrs
            });
        }

        _onNext();
    }
}
