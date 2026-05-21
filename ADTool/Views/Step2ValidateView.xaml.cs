using ADTool.ViewModels;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step2ValidateView : UserControl
{
    public Step2ValidateView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is Step2ValidateViewModel vm)
            await vm.ValidateAllAsync();
    }
}
