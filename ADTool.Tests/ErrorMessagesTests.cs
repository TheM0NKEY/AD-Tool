using ADTool.Services;

namespace ADTool.Tests;

public class ErrorMessagesTests
{
    [Theory]
    [InlineData(ValidationType.UserNotFound, "User not found")]
    [InlineData(ValidationType.DuplicateNewUPN, "UPN already in use")]
    [InlineData(ValidationType.InvalidDomain, "Unknown UPN suffix")]
    public void ForValidationFailure_ReturnsExpectedTitle(ValidationType type, string expectedTitle)
    {
        var (title, _) = ErrorMessages.ForValidationFailure(type, "old@test.com", "new@test.com");
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void ForValidationFailure_UserNotFound_DetailMentionsOldUpn()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.UserNotFound, "missing@test.com", "new@test.com");
        Assert.Contains("missing@test.com", detail);
    }

    [Fact]
    public void ForValidationFailure_DuplicateNewUPN_DetailMentionsNewUpn()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.DuplicateNewUPN, "old@test.com", "taken@test.com");
        Assert.Contains("taken@test.com", detail);
    }

    [Fact]
    public void ForValidationFailure_InvalidDomain_DetailMentionsSuffix()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.InvalidDomain, "old@test.com", "new@unknown.suffix");
        Assert.Contains("unknown.suffix", detail);
    }

    [Theory]
    [InlineData(ExecutionErrorType.InsufficientPermissions, "Insufficient permissions")]
    [InlineData(ExecutionErrorType.ProxyAddressConflict, "Proxy address conflict")]
    [InlineData(ExecutionErrorType.UnexpectedError, "Unexpected error")]
    public void ForExecutionFailure_ReturnsExpectedTitle(ExecutionErrorType type, string expectedTitle)
    {
        var (title, _) = ErrorMessages.ForExecutionFailure(type, null);
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void ForExecutionFailure_UnexpectedError_IncludesTechnicalDetail()
    {
        var (_, detail) = ErrorMessages.ForExecutionFailure(ExecutionErrorType.UnexpectedError, "Connection refused");
        Assert.Contains("Connection refused", detail);
    }

    [Fact]
    public void ForValidationFailure_InvalidDomain_NoAtSign_DoesNotReturnFullUPN()
    {
        var (_, detail) = ErrorMessages.ForValidationFailure(ValidationType.InvalidDomain, "old@test.com", "invaliddomain");
        Assert.Contains("invaliddomain", detail);
        Assert.Equal("Unknown UPN suffix", ErrorMessages.ForValidationFailure(ValidationType.InvalidDomain, "old@test.com", "invaliddomain").Title);
    }
}
