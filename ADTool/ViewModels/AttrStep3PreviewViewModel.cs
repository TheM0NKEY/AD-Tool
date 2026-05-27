using ADTool.Models;
using System.Collections.ObjectModel;
using System.Data;

namespace ADTool.ViewModels;

public class AttrStep3PreviewViewModel : BaseViewModel
{
    public DataTable PreviewTable { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand NextCommand { get; }
    public int EntryCount { get; }

    public AttrStep3PreviewViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        Action onBack,
        Action onNext)
    {
        BackCommand = new RelayCommand(onBack);
        NextCommand = new RelayCommand(onNext);
        EntryCount = entries.Count;
        PreviewTable = BuildPreviewTable(entries);
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
