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

        // Unsubscribe on close to prevent GC retention
        EventHandler? closeHandler = null;
        closeHandler = (_, _) =>
        {
            vm.RequestClose -= closeHandler;
            Close();
        };
        vm.RequestClose += closeHandler;

        // Guard async void Loaded handler to catch exceptions
        Loaded += async (_, _) =>
        {
            try { await vm.LoadTreeAsync(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load OU tree: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
    }

    private void OnOuSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AdBrowserViewModel vm)
            vm.SelectedOu = e.NewValue as OuNode;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
