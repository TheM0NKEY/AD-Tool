using ADTool.Models;
using System.Windows;

namespace ADTool.Views;

public partial class AddColumnDialog : Window
{
    public IReadOnlyList<string> SelectedLdapNames { get; private set; } = [];

    public AddColumnDialog()
    {
        InitializeComponent();
        AttributeList.ItemsSource = AttributeColumnMap.WellKnownAttributes
            .Select(a => new AttributeCheckItem { DisplayName = a.DisplayName, LdapName = a.LdapName })
            .ToList();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ((IEnumerable<AttributeCheckItem>)AttributeList.ItemsSource)
            .Where(i => i.IsChecked)
            .Select(i => i.LdapName)
            .ToList();

        var custom = CustomLdapTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(custom))
            selected.Add(custom);

        SelectedLdapNames = selected;
        DialogResult = true;
    }
}

public class AttributeCheckItem
{
    public string DisplayName { get; set; } = "";
    public string LdapName    { get; set; } = "";
    public bool   IsChecked   { get; set; }
}
