using ADTool.Services;

namespace ADTool.Tests;

public class ProxyAddressLogicTests
{
    [Fact]
    public void DemotesOldPrimaryAndAddsNewPrimary()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.DoesNotContain("SMTP:jsmith@old.com", result);
    }

    [Fact]
    public void PreservesExistingSecondaryAddresses()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com", "smtp:jsmith@other.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@alias.com", result);
        Assert.Contains("smtp:jsmith@other.com", result);
    }

    [Fact]
    public void ExactlyOnePrimaryAfterUpdate()
    {
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }

    [Fact]
    public void HandlesNoPrimaryExisting()
    {
        var existing = new[] { "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }

    [Fact]
    public void HandlesEmptyExistingProxyAddresses()
    {
        var result = ProxyAddressHelper.UpdateProxyAddresses([], "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("smtp:jsmith@old.com", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
    }

    [Fact]
    public void MatchIsCaseInsensitiveForOldPrimary()
    {
        var existing = new[] { "SMTP:JSMITH@OLD.COM" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        // Old uppercase primary should be gone; replaced with lowercase secondary
        Assert.DoesNotContain("SMTP:JSMITH@OLD.COM", result);
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }

    [Fact]
    public void NoPrimary_OldUpnAlreadySecondary_NoDuplicate()
    {
        var existing = new[] { "smtp:jsmith@old.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Single(result.Where(a => a.Equals("smtp:jsmith@old.com", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains("SMTP:jsmith@new.com", result);
    }

    [Fact]
    public void NewUpnAlreadyExistsAsSecondary_PromotedToPrimaryNoDuplicate()
    {
        // smtp:jsmith@new.com already exists as a secondary — should be promoted, not duplicated
        var existing = new[] { "SMTP:jsmith@old.com", "smtp:jsmith@new.com", "smtp:jsmith@alias.com" };
        var result = ProxyAddressHelper.UpdateProxyAddresses(existing, "jsmith@old.com", "jsmith@new.com");
        Assert.Contains("SMTP:jsmith@new.com", result);
        Assert.DoesNotContain("smtp:jsmith@new.com", result);
        Assert.Single(result.Where(a => a.StartsWith("SMTP:")));
    }
}
