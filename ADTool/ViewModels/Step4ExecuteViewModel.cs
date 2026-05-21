using ADTool.Models;
using ADTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace ADTool.ViewModels;

public class Step4ExecuteViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isExecuting;
    private int _successCount;
    private int _failCount;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set => SetField(ref _isExecuting, value);
    }

    public int SuccessCount
    {
        get => _successCount;
        private set => SetField(ref _successCount, value);
    }

    public int FailCount
    {
        get => _failCount;
        private set => SetField(ref _failCount, value);
    }

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

    public RelayCommand ExportResultsCommand { get; }
    public RelayCommand StartNewRunCommand { get; }

    public Step4ExecuteViewModel(
        ObservableCollection<UPNChangeEntry> entries,
        IAdService adService,
        Action onReset)
    {
        _entries = entries;
        _adService = adService;
        ExportResultsCommand = new RelayCommand(ExportResults);
        StartNewRunCommand = new RelayCommand(onReset);
    }

    public async Task ExecuteAllAsync()
    {
        IsExecuting = true;
        SuccessCount = 0;
        FailCount = 0;

        foreach (var entry in _entries)
        {
            var result = await _adService.UpdateUserAsync(entry.OldUPN, entry.NewUPN);
            entry.ExecutionStatus = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed;

            if (result.Success)
            {
                SuccessCount++;
            }
            else
            {
                (entry.ErrorTitle, entry.ErrorDetail) =
                    ErrorMessages.ForExecutionFailure(result.ErrorType, result.TechnicalDetail);
                FailCount++;
            }
        }

        IsExecuting = false;
    }

    private void ExportResults()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"upn-results-{DateTime.Now:yyyy-MM-dd-HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        using var writer = new StreamWriter(dlg.FileName);
        writer.WriteLine("OldUPN,NewUPN,DisplayName,Status,ErrorTitle,ErrorDetail");
        foreach (var e in _entries)
            writer.WriteLine($"{Escape(e.OldUPN)},{Escape(e.NewUPN)},{Escape(e.DisplayName ?? "")}," +
                             $"{e.ExecutionStatus},{Escape(e.ErrorTitle ?? "")},{Escape(e.ErrorDetail ?? "")}");
    }

    private static string Escape(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
}
