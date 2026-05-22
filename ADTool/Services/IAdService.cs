using ADTool.Models;

namespace ADTool.Services;

public enum ValidationType { None, UserNotFound, DuplicateNewUPN, InvalidDomain }
public enum ExecutionErrorType { None, InsufficientPermissions, ProxyAddressConflict, UnexpectedError }

public record ValidationResult(
    bool IsValid,
    string? DisplayName,
    ValidationType FailureType = ValidationType.None,
    string? TechnicalDetail = null);

public record ExecutionResult(
    bool Success,
    ExecutionErrorType ErrorType = ExecutionErrorType.None,
    string? TechnicalDetail = null);

public interface IAdService
{
    Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn);
    Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn);
    Task<bool> CheckIsDomainAdminAsync();
    Task<IReadOnlyList<OuNode>> GetOuTreeAsync();
    Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName);
}
