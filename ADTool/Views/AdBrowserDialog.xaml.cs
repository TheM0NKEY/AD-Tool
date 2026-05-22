using ADTool.Models;
using ADTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ADTool.Views;

public partial class AdBrowserDialog : Window
{
    public AdBrowserDialog(AdBrowserViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += (_, _) => Close();
        Loaded += async (_, _) => await vm.LoadTreeAsync();
    }

    private void OnOuSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AdBrowserViewModel vm && e.NewValue is OuNode ou)
            vm.SelectedOu = ou;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
