using ADTool.ViewModels;

namespace ADTool.Tests;

public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsAction()
    {
        bool called = false;
        var cmd = new RelayCommand(() => called = true);
        cmd.Execute(null);
        Assert.True(called);
    }

    [Fact]
    public void CanExecute_ReturnsTrueWhenNoPredicateGiven()
    {
        var cmd = new RelayCommand(() => { });
        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsFalseWhenPredicateFalse()
    {
        var cmd = new RelayCommand(() => { }, () => false);
        Assert.False(cmd.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_FiresEvent()
    {
        var cmd = new RelayCommand(() => { });
        bool fired = false;
        cmd.CanExecuteChanged += (_, _) => fired = true;
        cmd.RaiseCanExecuteChanged();
        Assert.True(fired);
    }

    [Fact]
    public void GenericRelayCommand_Execute_PassesParameter()
    {
        string? received = null;
        var cmd = new RelayCommand<string>(s => received = s);
        cmd.Execute("hello");
        Assert.Equal("hello", received);
    }
}
