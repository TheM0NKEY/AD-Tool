using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class Step2ValidateViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries;
    private readonly IAdService _adService;
    private bool _isValidating;
    private int _validatedCount;

    public ObservableCollection<UPNChangeEntry> Entries => _entries;

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

    public Step2ValidateViewModel(
        ObservableCollection<UPNChangeEntry> entries,
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

        var tasks = _entries.Select(async entry =>
        {
            var result = await _adService.ValidateUserAsync(entry.OldUPN, entry.NewUPN);
            entry.DisplayName = result.DisplayName;
            entry.ValidationStatus = result.IsValid
                ? ValidationStatus.Valid
                : result.FailureType switch
                {
                    ValidationType.DuplicateNewUPN => ValidationStatus.DuplicateNewUPN,
                    ValidationType.InvalidDomain   => ValidationStatus.InvalidDomain,
                    _                              => ValidationStatus.NotFound
                };

            if (!result.IsValid)
            {
                (entry.ErrorTitle, entry.ErrorDetail) =
                    ErrorMessages.ForValidationFailure(result.FailureType, entry.OldUPN, entry.NewUPN);
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
