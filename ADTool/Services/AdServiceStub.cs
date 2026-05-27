using ADTool.Models;

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

    public Task<bool> CheckIsDomainAdminAsync()
        => Task.FromResult(true);

    public Task<IReadOnlyList<OuNode>> GetOuTreeAsync()
    {
        IReadOnlyList<OuNode> tree =
        [
            new OuNode("contoso.com", "DC=contoso,DC=com",
            [
                new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []),
                new OuNode("IT", "OU=IT,DC=contoso,DC=com",
                [
                    new OuNode("Operations", "OU=Operations,OU=IT,DC=contoso,DC=com", [])
                ])
            ])
        ];
        return Task.FromResult(tree);
    }

    public Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName)
    {
        IReadOnlyList<AdUser> users =
        [
            new AdUser("alice@contoso.com", "Alice Smith"),
            new AdUser("bob@contoso.com", "Bob Jones"),
            new AdUser("carol@contoso.com", "Carol White"),
        ];
        return Task.FromResult(users);
    }

    public Task<ValidationResult> ValidateUserExistsAsync(string upn)
    {
        string displayName = $"[Stub] {(upn.Contains('@') ? upn.Split('@')[0] : upn)}";
        return Task.FromResult(new ValidationResult(true, displayName));
    }

    public Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes)
        => Task.FromResult(new ExecutionResult(true));
}
