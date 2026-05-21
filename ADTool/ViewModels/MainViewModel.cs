using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentStep;

    public ObservableCollection<UPNChangeEntry> Entries { get; } = new();

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public RelayCommand ResetCommand { get; }

    private readonly BaseViewModel[] _steps;

    public MainViewModel(IAdService adService, CsvImportService csvService)
    {
        var step1 = new Step1InputViewModel(Entries, csvService, () => GoTo(2));
        var step2 = new Step2ValidateViewModel(Entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new Step3PreviewViewModel(Entries, () => GoTo(2), () => GoTo(4));
        var step4 = new Step4ExecuteViewModel(Entries, adService, () => Reset());

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        ResetCommand = new RelayCommand(Reset);
    }

    public void GoTo(int stepNumber)
    {
        if (stepNumber < 1 || stepNumber > _steps.Length)
            throw new ArgumentOutOfRangeException(nameof(stepNumber), $"Step must be between 1 and {_steps.Length}.");
        CurrentStep = _steps[stepNumber - 1];
    }

    private void Reset()
    {
        Entries.Clear();
        GoTo(1);
    }
}
