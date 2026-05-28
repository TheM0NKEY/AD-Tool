using ADTool.Views;
using Xunit;

namespace ADTool.Tests.Views;

public class StepIndicatorControlTests
{
    private static readonly string[] Labels = ["Input", "Validate", "Preview", "Execute"];

    [Fact]
    public void BuildItems_Step1Active_AllOthersPending()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.Equal(StepState.Active,  items[0].State);
        Assert.Equal(StepState.Pending, items[1].State);
        Assert.Equal(StepState.Pending, items[2].State);
        Assert.Equal(StepState.Pending, items[3].State);
    }

    [Fact]
    public void BuildItems_Step2Active_Step1Completed()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 2);
        Assert.Equal(StepState.Completed, items[0].State);
        Assert.Equal(StepState.Active,    items[1].State);
        Assert.Equal(StepState.Pending,   items[2].State);
    }

    [Fact]
    public void BuildItems_LastItemHasNoConnector()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.False(items[3].ShowConnector);
        Assert.True(items[0].ShowConnector);
    }

    [Fact]
    public void BuildItems_LabelsAndNumbersCorrect()
    {
        var items = StepIndicatorControl.BuildItems(Labels, 1);
        Assert.Equal("Input",    items[0].Label);
        Assert.Equal("Validate", items[1].Label);
        Assert.Equal("1",        items[0].Number);
        Assert.Equal("2",        items[1].Number);
    }

    [Fact]
    public void BuildItems_NullSteps_ReturnsEmpty()
    {
        var items = StepIndicatorControl.BuildItems(null, 1);
        Assert.Empty(items);
    }
}
