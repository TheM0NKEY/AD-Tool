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
        ("Custom Attribute 1",  "extensionAttribute1"),
        ("Custom Attribute 2",  "extensionAttribute2"),
        ("Custom Attribute 3",  "extensionAttribute3"),
        ("Custom Attribute 4",  "extensionAttribute4"),
        ("Custom Attribute 5",  "extensionAttribute5"),
        ("Custom Attribute 6",  "extensionAttribute6"),
        ("Custom Attribute 7",  "extensionAttribute7"),
        ("Custom Attribute 8",  "extensionAttribute8"),
        ("Custom Attribute 9",  "extensionAttribute9"),
        ("Custom Attribute 10", "extensionAttribute10"),
        ("Custom Attribute 11", "extensionAttribute11"),
        ("Custom Attribute 12", "extensionAttribute12"),
        ("Custom Attribute 13", "extensionAttribute13"),
        ("Custom Attribute 14", "extensionAttribute14"),
        ("Custom Attribute 15", "extensionAttribute15"),
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
            ["CustomAttribute1"]  = "extensionAttribute1",
            ["CustomAttribute2"]  = "extensionAttribute2",
            ["CustomAttribute3"]  = "extensionAttribute3",
            ["CustomAttribute4"]  = "extensionAttribute4",
            ["CustomAttribute5"]  = "extensionAttribute5",
            ["CustomAttribute6"]  = "extensionAttribute6",
            ["CustomAttribute7"]  = "extensionAttribute7",
            ["CustomAttribute8"]  = "extensionAttribute8",
            ["CustomAttribute9"]  = "extensionAttribute9",
            ["CustomAttribute10"] = "extensionAttribute10",
            ["CustomAttribute11"] = "extensionAttribute11",
            ["CustomAttribute12"] = "extensionAttribute12",
            ["CustomAttribute13"] = "extensionAttribute13",
            ["CustomAttribute14"] = "extensionAttribute14",
            ["CustomAttribute15"] = "extensionAttribute15",
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
