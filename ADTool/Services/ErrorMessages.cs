namespace ADTool.Services;

public static class ErrorMessages
{
    public static (string Title, string Detail) ForValidationFailure(
        ValidationType type, string oldUpn, string newUpn) => type switch
    {
        ValidationType.UserNotFound => (
            "User not found",
            $"No user with UPN '{oldUpn}' exists in Active Directory. Check for typos or verify the domain suffix."),
        ValidationType.DuplicateNewUPN => (
            "UPN already in use",
            $"The new UPN '{newUpn}' is already assigned to another user. Choose a different UPN."),
        ValidationType.InvalidDomain => (
            "Unknown UPN suffix",
            $"The suffix '@{(newUpn.Contains('@') ? newUpn.Split('@')[^1] : newUpn)}' is not a registered UPN suffix in this forest. " +
            "Add it in Active Directory Domains and Trusts first."),
        _ => ("Validation failed", "An unexpected error occurred during validation.")
    };

    public static (string Title, string Detail) ForExecutionFailure(
        ExecutionErrorType type, string? technicalDetail) => type switch
    {
        ExecutionErrorType.InsufficientPermissions => (
            "Insufficient permissions",
            "Your account doesn't have permission to modify this user. You need Write access to " +
            "userPrincipalName and proxyAddresses on the target OU, or run this tool as a Domain Admin."),
        ExecutionErrorType.ProxyAddressConflict => (
            "Proxy address conflict",
            "The old UPN already exists as a proxy address on another AD object. " +
            "Manual cleanup is required before this entry can be processed."),
        _ => (
            "Unexpected error",
            $"An unexpected error occurred. Technical details: {technicalDetail}")
    };
}
