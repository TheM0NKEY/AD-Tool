using ADTool.Models;
using ADTool.Services;
using System.Collections.ObjectModel;

namespace ADTool.ViewModels;

public class AttributeToolViewModel : BaseViewModel
{
    private readonly ObservableCollection<AttributeChangeEntry> _entries = new();
    private BaseViewModel _currentStep;
    private readonly BaseViewModel[] _steps;

    public BaseViewModel CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public AttributeToolViewModel(IAdService adService, Action returnHome)
    {
        var step1 = new AttrStep1InputViewModel(_entries, adService, () => GoTo(2));
        var step2 = new AttrStep2ValidateViewModel(_entries, adService, () => GoTo(1), () => GoTo(3));
        var step3 = new AttrStep3PreviewViewModel(_entries, () => GoTo(2), () => GoTo(4));
        var step4 = new AttrStep4ExecuteViewModel(_entries, adService, Reset);

        _steps = [step1, step2, step3, step4];
        _currentStep = step1;

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
