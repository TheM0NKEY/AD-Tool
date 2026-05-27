using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep4ExecuteView : UserControl
{
    public AttrStep4ExecuteView() { InitializeComponent(); }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttrStep4ExecuteViewModel vm)
            await vm.ExecuteAllAsync();
    }
}
