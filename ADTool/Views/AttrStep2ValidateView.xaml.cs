using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AttrStep2ValidateView : UserControl
{
    public AttrStep2ValidateView() { InitializeComponent(); }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttrStep2ValidateViewModel vm)
            await vm.ValidateAllAsync();
    }
}
