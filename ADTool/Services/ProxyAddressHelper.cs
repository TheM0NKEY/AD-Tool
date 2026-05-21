namespace ADTool.Services;

public static class ProxyAddressHelper
{
    public static IReadOnlyList<string> UpdateProxyAddresses(
        IEnumerable<string> existing, string oldUpn, string newUpn)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(oldUpn);
        ArgumentNullException.ThrowIfNull(newUpn);

        var result = new List<string>();
        bool foundPrimary = false;

        foreach (var addr in existing)
        {
            if (addr.Equals($"SMTP:{oldUpn}", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"smtp:{oldUpn}");
                foundPrimary = true;
            }
            else
            {
                result.Add(addr);
            }
        }

        if (!foundPrimary && !result.Any(a => a.Equals($"smtp:{oldUpn}", StringComparison.OrdinalIgnoreCase)))
            result.Add($"smtp:{oldUpn}");

        result.Add($"SMTP:{newUpn}");
        return result;
    }
}
