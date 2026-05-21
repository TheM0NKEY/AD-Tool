using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADTool.Models;

public enum ValidationStatus { Pending, Valid, NotFound, DuplicateNewUPN, InvalidDomain }
public enum ExecutionStatus { Pending, Success, Failed }

public class UPNChangeEntry : INotifyPropertyChanged
{
    private string _oldUpn = string.Empty;
    private string _newUpn = string.Empty;
    private string? _displayName;
    private ValidationStatus _validationStatus;
    private ExecutionStatus _executionStatus;
    private string? _errorTitle;
    private string? _errorDetail;

    public string OldUPN
    {
        get => _oldUpn;
        set { _oldUpn = value; OnPropertyChanged(); }
    }

    public string NewUPN
    {
        get => _newUpn;
        set { _newUpn = value; OnPropertyChanged(); }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
