using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class UPNToolViewModel : BaseViewModel
{
    private readonly ObservableCollection<UPNChangeEntry> _entries = new();
    private BaseViewModel _currentStep;
    private readonly BaseViewModel[] _steps;

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set
        {
            SetField(ref _currentStep, value);
            OnPropertyChanged(nameof(CurrentStepNumber));
        }
    }

    public int CurrentStepNumber => Array.IndexOf(_steps, _currentStep) + 1;

    public RelayCommand ReturnHomeCommand { get; }

    public UPNToolViewModel(IAdService adService, CsvImportService csvService, Action returnHome)
    {
        var step1 = new Step1InputViewModel(_entries, csvService, adService, () => GoTo(2));
        var step2 = new Step2ValidateViewModel(_entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new Step3PreviewViewModel(_entries, () => GoTo(2), () => GoTo(4));
        var step4 = new Step4ExecuteViewModel(_entries, adService, Reset);

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

        ReturnHomeCommand = new RelayCommand(returnHome);

        void Reset()
        {
            _entries.Clear();
            returnHome();
        }
    }

    public void GoTo(int stepNumber)
    {
        if (stepNumber < 1 || stepNumber > _steps.Length)
            throw new ArgumentOutOfRangeException(nameof(stepNumber),
                $"Step must be between 1 and {_steps.Length}.");
        CurrentStep = _steps[stepNumber - 1];
    }
}
