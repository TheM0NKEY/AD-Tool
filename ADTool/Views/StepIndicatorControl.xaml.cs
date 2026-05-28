// ADTool/Views/StepIndicatorControl.xaml.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public enum StepState { Pending, Active, Completed }

public class StepDisplayItem
{
    public string Label        { get; init; } = "";
    public string Number       { get; init; } = "";
    public StepState State     { get; init; }
    public bool ShowConnector  { get; init; }
}

public partial class StepIndicatorControl : UserControl
{
    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(nameof(Steps), typeof(IReadOnlyList<string>),
            typeof(StepIndicatorControl), new PropertyMetadata(null, OnStateChanged));

    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int),
            typeof(StepIndicatorControl), new PropertyMetadata(1, OnStateChanged));

    public IReadOnlyList<string>? Steps
    {
        get => (IReadOnlyList<string>?)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    private readonly ObservableCollection<StepDisplayItem> _displayItems = [];
    public ObservableCollection<StepDisplayItem> DisplayItems => _displayItems;

    public StepIndicatorControl() => InitializeComponent();

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StepIndicatorControl c) c.Rebuild();
    }

    private void Rebuild()
    {
        _displayItems.Clear();
        foreach (var item in BuildItems(Steps, CurrentStep))
            _displayItems.Add(item);
    }

    internal static IReadOnlyList<StepDisplayItem> BuildItems(
        IReadOnlyList<string>? steps, int currentStep)
    {
        if (steps == null || steps.Count == 0) return [];
        return steps.Select((label, i) => new StepDisplayItem
        {
            Label         = label,
            Number        = (i + 1).ToString(),
            State         = (i + 1) < currentStep ? StepState.Completed
                          : (i + 1) == currentStep ? StepState.Active
                          : StepState.Pending,
            ShowConnector = i < steps.Count - 1
        }).ToList();
    }
}
