using ADTool.Models;
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

                de.Properties["mail"].Value = newUpn;
                de.Properties["mailNickname"].Value = newUpn.IndexOf('@') > 0 ? newUpn[..newUpn.IndexOf('@')] : newUpn;

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

    public async Task<bool> CheckIsDomainAdminAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var user = UserPrincipal.Current;
                using var groups = user.GetAuthorizationGroups();
                return groups.Any(g => g.Name.Equals("Domain Admins", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<IReadOnlyList<OuNode>> GetOuTreeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var rootDse = new DirectoryEntry("LDAP://RootDSE");
                string defaultNC = rootDse.Properties["defaultNamingContext"][0]!.ToString()!;
                string rootName = string.Join(".", defaultNC
                    .Split(',')
                    .Select(p => p.TrimStart())
                    .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p[3..]));
                var children = GetOuChildren(defaultNC);
                return (IReadOnlyList<OuNode>)[new OuNode(rootName, defaultNC, children)];
            }
            catch
            {
                return Array.Empty<OuNode>();
            }
        });
    }

    public async Task<IReadOnlyList<AdUser>> GetUsersInOuAsync(string ouDistinguishedName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{ouDistinguishedName}");
                using var searcher = new DirectorySearcher(entry)
                {
                    Filter = "(&(objectClass=user)(objectCategory=person)(userPrincipalName=*))",
                    SearchScope = SearchScope.Subtree,
                    PageSize = 1000
                };
                searcher.PropertiesToLoad.AddRange(new[] { "userPrincipalName", "displayName" });

                using var results = searcher.FindAll();
                var users = new List<AdUser>();
                foreach (SearchResult result in results)
                {
                    string upn = result.Properties["userPrincipalName"][0]?.ToString() ?? "";
                    string displayName = result.Properties["displayName"].Count > 0
                        ? result.Properties["displayName"][0]?.ToString() ?? upn
                        : upn;
                    if (!string.IsNullOrEmpty(upn))
                        users.Add(new AdUser(upn, displayName));
                }
                return (IReadOnlyList<AdUser>)users;
            }
            catch
            {
                return Array.Empty<AdUser>();
            }
        });
    }

    public async Task<ValidationResult> ValidateUserExistsAsync(string upn)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
                if (user is null)
                    return new ValidationResult(false, null, ValidationType.UserNotFound);
                return new ValidationResult(true, user.DisplayName);
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, null, ValidationType.UserNotFound, ex.Message);
            }
        });
    }

    public async Task<ExecutionResult> UpdateAttributesAsync(string upn, Dictionary<string, string> attributes)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
                if (user is null)
                    return new ExecutionResult(false, ExecutionErrorType.UnexpectedError,
                        "User not found at execution time.");

                var de = (DirectoryEntry)user.GetUnderlyingObject();
                foreach (var (ldapName, value) in attributes)
                    de.Properties[ldapName].Value = value;
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

    private static IReadOnlyList<OuNode> GetOuChildren(string parentDn, int depth = 0)
    {
        if (depth > 50) return Array.Empty<OuNode>();
        try
        {
            using var entry = new DirectoryEntry($"LDAP://{parentDn}");
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=organizationalUnit)",
                SearchScope = SearchScope.OneLevel,
                PageSize = 1000
            };
            searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName" });

            using var results = searcher.FindAll();
            var nodes = new List<OuNode>();
            foreach (SearchResult result in results)
            {
                string dn = result.Properties["distinguishedName"][0]?.ToString() ?? "";
                string name = result.Properties["name"][0]?.ToString() ?? dn;
                nodes.Add(new OuNode(name, dn, GetOuChildren(dn, depth + 1)));
            }
            return nodes;
        }
        catch
        {
            return Array.Empty<OuNode>();
        }
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
