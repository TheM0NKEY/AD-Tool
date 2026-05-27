using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class AttrStep2ValidateViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isValidating;
    private int _validatedCount;

    public ObservableCollection<AttributeChangeEntry> Entries => _entries;

    public bool IsValidating
    {
        get => _isValidating;
        private set { SetField(ref _isValidating, value); NextCommand.RaiseCanExecuteChanged(); }
    }

    public int ValidatedCount
    {
        get => _validatedCount;
        private set => SetField(ref _validatedCount, value);
    }

    public int TotalCount => _entries.Count;

    public bool HasInvalidRows =>
        _entries.Any(e => e.ValidationStatus != ValidationStatus.Valid
                       && e.ValidationStatus != ValidationStatus.Pending);

    public RelayCommand BackCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand RemoveInvalidRowsCommand { get; }

    public AttrStep2ValidateViewModel(
        ObservableCollection<AttributeChangeEntry> entries,
        IAdService adService,
        Action onBack,
        Action onNext)
    {
        _entries = entries;
        _adService = adService;
        BackCommand = new RelayCommand(onBack);
        NextCommand = new RelayCommand(onNext, CanGoNext);
        RemoveInvalidRowsCommand = new RelayCommand(RemoveInvalidRows);
    }

    public async Task ValidateAllAsync()
    {
        IsValidating = true;
        ValidatedCount = 0;
        OnPropertyChanged(nameof(TotalCount));

        // Pre-pass: flag same-batch duplicate UPNs without hitting AD
        var batchDuplicates = _entries
            .GroupBy(e => e.UserUPN, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToHashSet();

        foreach (var entry in batchDuplicates)
        {
            entry.ValidationStatus = ValidationStatus.DuplicateNewUPN;
            entry.ErrorTitle = "Duplicate user in batch";
            entry.ErrorDetail = $"The UPN '{entry.UserUPN}' appears more than once in this batch. Each user can only appear once per run.";
            Interlocked.Increment(ref _validatedCount);
            OnPropertyChanged(nameof(ValidatedCount));
        }

        var tasks = _entries.Where(e => !batchDuplicates.Contains(e)).Select(async entry =>
        {
            var result = await _adService.ValidateUserExistsAsync(entry.UserUPN);
            entry.DisplayName = result.DisplayName;
            entry.ValidationStatus = result.IsValid ? ValidationStatus.Valid : ValidationStatus.NotFound;

            if (!result.IsValid)
            {
                entry.ErrorTitle = "User not found";
                entry.ErrorDetail = $"No user with UPN '{entry.UserUPN}' exists in Active Directory.";
            }

            Interlocked.Increment(ref _validatedCount);
            OnPropertyChanged(nameof(ValidatedCount));
        });

        await Task.WhenAll(tasks);

        IsValidating = false;
        OnPropertyChanged(nameof(HasInvalidRows));
        NextCommand.RaiseCanExecuteChanged();
        RemoveInvalidRowsCommand.RaiseCanExecuteChanged();
    }

    private bool CanGoNext() =>
        !_isValidating && _entries.Any() && _entries.All(e => e.ValidationStatus == ValidationStatus.Valid);

    private void RemoveInvalidRows()
    {
        var invalid = _entries.Where(e => e.ValidationStatus != ValidationStatus.Valid).ToList();
        foreach (var entry in invalid)
            _entries.Remove(entry);
        OnPropertyChanged(nameof(HasInvalidRows));
        NextCommand.RaiseCanExecuteChanged();
    }
}
