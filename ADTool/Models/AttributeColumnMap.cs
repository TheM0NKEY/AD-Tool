namespace ADTool.Models;

/// <summary>
/// Maps CSV column headers to AD LDAP attribute names.
/// Unknown headers pass through verbatim as raw LDAP attribute names (advanced mode).
/// </summary>
public static class AttributeColumnMap
{
    /// <summary>All well-known attributes shown in the Add Column picker.</summary>
    public static readonly IReadOnlyList<(string DisplayName, string LdapName)> WellKnownAttributes =
    [
        ("Department",          "department"),
        ("Description",         "description"),
        ("Title",               "title"),
        ("Company",             "company"),
        ("Office",              "physicalDeliveryOfficeName"),
        ("Phone",               "telephoneNumber"),
        ("Manager",             "manager"),
        ("Employee ID",         "employeeID"),
        ("Pager",               "pager"),
        ("Cloud Attribute 1",  "msDS-cloudExtensionAttribute1"),
        ("Cloud Attribute 2",  "msDS-cloudExtensionAttribute2"),
        ("Cloud Attribute 3",  "msDS-cloudExtensionAttribute3"),
        ("Cloud Attribute 4",  "msDS-cloudExtensionAttribute4"),
        ("Cloud Attribute 5",  "msDS-cloudExtensionAttribute5"),
        ("Cloud Attribute 6",  "msDS-cloudExtensionAttribute6"),
        ("Cloud Attribute 7",  "msDS-cloudExtensionAttribute7"),
        ("Cloud Attribute 8",  "msDS-cloudExtensionAttribute8"),
        ("Cloud Attribute 9",  "msDS-cloudExtensionAttribute9"),
        ("Cloud Attribute 10", "msDS-cloudExtensionAttribute10"),
        ("Cloud Attribute 11", "msDS-cloudExtensionAttribute11"),
        ("Cloud Attribute 12", "msDS-cloudExtensionAttribute12"),
        ("Cloud Attribute 13", "msDS-cloudExtensionAttribute13"),
        ("Cloud Attribute 14", "msDS-cloudExtensionAttribute14"),
        ("Cloud Attribute 15", "msDS-cloudExtensionAttribute15"),
        ("Cloud Attribute 16", "msDS-cloudExtensionAttribute16"),
        ("Cloud Attribute 17", "msDS-cloudExtensionAttribute17"),
        ("Cloud Attribute 18", "msDS-cloudExtensionAttribute18"),
        ("Cloud Attribute 19", "msDS-cloudExtensionAttribute19"),
        ("Cloud Attribute 20", "msDS-cloudExtensionAttribute20"),
    ];

    public static readonly IReadOnlySet<string> IdentityHeaders =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UPN", "UserPrincipalName" };

    private static readonly Dictionary<string, string> _aliasMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Department"]        = "department",
            ["Description"]       = "description",
            ["Title"]             = "title",
            ["Company"]           = "company",
            ["Office"]            = "physicalDeliveryOfficeName",
            ["Phone"]             = "telephoneNumber",
            ["Manager"]           = "manager",
            ["EmployeeID"]        = "employeeID",
            ["Employee ID"]       = "employeeID",
            ["Pager"]             = "pager",
            ["CloudAttribute1"]    = "msDS-cloudExtensionAttribute1",
            ["Cloud Attribute 1"]  = "msDS-cloudExtensionAttribute1",
            ["CloudAttribute2"]    = "msDS-cloudExtensionAttribute2",
            ["Cloud Attribute 2"]  = "msDS-cloudExtensionAttribute2",
            ["CloudAttribute3"]    = "msDS-cloudExtensionAttribute3",
            ["Cloud Attribute 3"]  = "msDS-cloudExtensionAttribute3",
            ["CloudAttribute4"]    = "msDS-cloudExtensionAttribute4",
            ["Cloud Attribute 4"]  = "msDS-cloudExtensionAttribute4",
            ["CloudAttribute5"]    = "msDS-cloudExtensionAttribute5",
            ["Cloud Attribute 5"]  = "msDS-cloudExtensionAttribute5",
            ["CloudAttribute6"]    = "msDS-cloudExtensionAttribute6",
            ["Cloud Attribute 6"]  = "msDS-cloudExtensionAttribute6",
            ["CloudAttribute7"]    = "msDS-cloudExtensionAttribute7",
            ["Cloud Attribute 7"]  = "msDS-cloudExtensionAttribute7",
            ["CloudAttribute8"]    = "msDS-cloudExtensionAttribute8",
            ["Cloud Attribute 8"]  = "msDS-cloudExtensionAttribute8",
            ["CloudAttribute9"]    = "msDS-cloudExtensionAttribute9",
            ["Cloud Attribute 9"]  = "msDS-cloudExtensionAttribute9",
            ["CloudAttribute10"]   = "msDS-cloudExtensionAttribute10",
            ["Cloud Attribute 10"] = "msDS-cloudExtensionAttribute10",
            ["CloudAttribute11"]   = "msDS-cloudExtensionAttribute11",
            ["Cloud Attribute 11"] = "msDS-cloudExtensionAttribute11",
            ["CloudAttribute12"]   = "msDS-cloudExtensionAttribute12",
            ["Cloud Attribute 12"] = "msDS-cloudExtensionAttribute12",
            ["CloudAttribute13"]   = "msDS-cloudExtensionAttribute13",
            ["Cloud Attribute 13"] = "msDS-cloudExtensionAttribute13",
            ["CloudAttribute14"]   = "msDS-cloudExtensionAttribute14",
            ["Cloud Attribute 14"] = "msDS-cloudExtensionAttribute14",
            ["CloudAttribute15"]   = "msDS-cloudExtensionAttribute15",
            ["Cloud Attribute 15"] = "msDS-cloudExtensionAttribute15",
            ["CloudAttribute16"]   = "msDS-cloudExtensionAttribute16",
            ["Cloud Attribute 16"] = "msDS-cloudExtensionAttribute16",
            ["CloudAttribute17"]   = "msDS-cloudExtensionAttribute17",
            ["Cloud Attribute 17"] = "msDS-cloudExtensionAttribute17",
            ["CloudAttribute18"]   = "msDS-cloudExtensionAttribute18",
            ["Cloud Attribute 18"] = "msDS-cloudExtensionAttribute18",
            ["CloudAttribute19"]   = "msDS-cloudExtensionAttribute19",
            ["Cloud Attribute 19"] = "msDS-cloudExtensionAttribute19",
            ["CloudAttribute20"]   = "msDS-cloudExtensionAttribute20",
            ["Cloud Attribute 20"] = "msDS-cloudExtensionAttribute20",
        };

    /// <summary>
    /// Returns the LDAP attribute name for the given CSV header.
    /// Returns null if the header is an identity column (UPN/UserPrincipalName).
    /// Returns the header verbatim if not in the alias map.
    /// </summary>
    public static string? Resolve(string header)
    {
        if (IdentityHeaders.Contains(header)) return null;
        return _aliasMap.TryGetValue(header, out var ldap) ? ldap : header;
    }
}
