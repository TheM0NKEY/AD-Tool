using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace ADTool.Services;

public class AdService : IAdService
{
    public async Task<ValidationResult> ValidateUserAsync(string oldUpn, string newUpn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);

                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, oldUpn);
                if (user is null)
                    return new ValidationResult(false, null, ValidationType.UserNotFound);

                using var duplicate = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, newUpn);
                if (duplicate is not null)
                    return new ValidationResult(false, null, ValidationType.DuplicateNewUPN);

                string newSuffix = newUpn.Contains('@') ? newUpn.Split('@')[1] : string.Empty;
                if (!IsValidUpnSuffix(newSuffix))
                    return new ValidationResult(false, null, ValidationType.InvalidDomain);

                return new ValidationResult(true, user.DisplayName);
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, null, ValidationType.UserNotFound, ex.Message);
            }
        });
    }

    public async Task<ExecutionResult> UpdateUserAsync(string oldUpn, string newUpn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, oldUpn);

                if (user is null)
                    return new ExecutionResult(false, ExecutionErrorType.UnexpectedError, "User not found at execution time.");

                user.UserPrincipalName = newUpn;
                user.Save();

                var de = (DirectoryEntry)user.GetUnderlyingObject();
                var proxies = de.Properties["proxyAddresses"];
                var existing = proxies.Count > 0 ? proxies.Cast<string>().ToList() : new List<string>();
                var updated = ProxyAddressHelper.UpdateProxyAddresses(existing, oldUpn, newUpn);

                proxies.Clear();
                foreach (var addr in updated)
                    proxies.Add(addr);

                de.CommitChanges();

                return new ExecutionResult(true);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ExecutionResult(false, ExecutionErrorType.InsufficientPermissions, ex.Message);
            }
            catch (Exception ex)
            {
                return new ExecutionResult(false, ExecutionErrorType.UnexpectedError, ex.Message);
            }
        });
    }

    private static bool IsValidUpnSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return false;
        try
        {
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            string configNC = rootDse.Properties["configurationNamingContext"][0]!.ToString()!;
            string forestRoot = string.Join(".", configNC.Split(',')
                .Where(p => p.TrimStart().StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.TrimStart()[3..]));

            if (forestRoot.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                return true;

            using var partitions = new DirectoryEntry($"LDAP://CN=Partitions,{configNC}");
            foreach (string s in partitions.Properties["uPNSuffixes"])
                if (s.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
        catch
        {
            return true;
        }
    }
}
