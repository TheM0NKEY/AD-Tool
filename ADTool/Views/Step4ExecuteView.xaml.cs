using ADTool.ViewModels;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class Step4ExecuteView : UserControl
{
    public Step4ExecuteView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is Step4ExecuteViewModel vm)
            await vm.ExecuteAllAsync();
    }
}
