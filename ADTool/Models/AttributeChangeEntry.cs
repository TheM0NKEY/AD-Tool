using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADTool.Models;

public class AttributeChangeEntry : INotifyPropertyChanged
{
    private string _userUpn = string.Empty;
    private string? _displayName;
    private ValidationStatus _validationStatus;
    private ExecutionStatus _executionStatus;
    private string? _errorTitle;
    private string? _errorDetail;

    public string UserUPN
    {
        get => _userUpn;
        set { _userUpn = value; OnPropertyChanged(); }
    }

    public string? DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public ValidationStatus ValidationStatus
    {
        get => _validationStatus;
        set { _validationStatus = value; OnPropertyChanged(); }
    }

    public ExecutionStatus ExecutionStatus
    {
        get => _executionStatus;
        set { _executionStatus = value; OnPropertyChanged(); }
    }

    public string? ErrorTitle
    {
        get => _errorTitle;
        set { _errorTitle = value; OnPropertyChanged(); }
    }

    public string? ErrorDetail
    {
        get => _errorDetail;
        set { _errorDetail = value; OnPropertyChanged(); }
    }

    // LDAP attribute name → value to write. Empty/null values are skipped at execute time.
    public Dictionary<string, string?> Attributes { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
