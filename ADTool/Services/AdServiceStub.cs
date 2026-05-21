namespace ADTool.Services;

public class AdServiceStub : IAdService
{
    public Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn)
    {
        string displayName = $"[Stub] {oldUpn.Split('@')[0]}";
        return Task.FromResult(new ValidationResult(true, displayName));
    }

    public Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn)
        => Task.FromResult(new ExecutionResult(true));
}
