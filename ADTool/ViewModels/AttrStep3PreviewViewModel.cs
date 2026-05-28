using ADTool.Models;
using System.Collections.ObjectModel;
using System.Data;

namespace ADTool.ViewModels;

public class AttrStep3PreviewViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private DataTable _previewTable = new();
    private int _entryCount;

    public DataTable PreviewTable
    {
        get => _previewTable;
        private set => SetField(ref _previewTable, value);
    }

    public int EntryCount
    {
        get => _entryCount;
        private set => SetField(ref _entryCount, value);
    }

    public RelayCommand BackCommand { get; }
    public RelayCommand NextCommand { get; }

    public AttrStep3PreviewViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        Action onBack,
        Action onNext)
    {
        _entries = entries;
        BackCommand = new RelayCommand(onBack);
        NextCommand = new RelayCommand(onNext);
    }

    public void Refresh()
    {
        EntryCount = _entries.Count;
        PreviewTable = BuildPreviewTable(_entries);
    }

    private static DataTable BuildPreviewTable(IEnumerable<AttributeChangeEntry> entries)
    {
        var table = new DataTable();
        var list = entries.ToList();

        table.Columns.Add("Display Name", typeof(string));
        table.Columns.Add("UPN", typeof(string));

        var attrKeys = list
            .SelectMany(e => e.Attributes.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var key in attrKeys)
            table.Columns.Add(key, typeof(string));

        foreach (var entry in list)
        {
            var row = table.NewRow();
            row["Display Name"] = entry.DisplayName ?? "";
            row["UPN"]          = entry.UserUPN;
            foreach (var key in attrKeys)
                row[key] = entry.Attributes.TryGetValue(key, out var val) ? val ?? "" : "";
            table.Rows.Add(row);
        }

        return table;
    }
}
