using ADTool.Models;

namespace ADTool.Tests;

public class AttributeColumnMapTests
{
    [Theory]
    [InlineData("UPN")]
    [InlineData("upn")]
    [InlineData("UserPrincipalName")]
    [InlineData("userprincipalname")]
    public void Resolve_IdentityColumn_ReturnsNull(string header)
    {
        Assert.Null(AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("Department",   "department")]
    [InlineData("DEPARTMENT",   "department")]
    [InlineData("Title",        "title")]
    [InlineData("Company",      "company")]
    [InlineData("Office",       "physicalDeliveryOfficeName")]
    [InlineData("Phone",        "telephoneNumber")]
    [InlineData("Manager",      "manager")]
    [InlineData("Description",  "description")]
    public void Resolve_WellKnownHeader_ReturnsLdapName(string header, string expectedLdap)
    {
        Assert.Equal(expectedLdap, AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("CustomAttribute1",  "extensionAttribute1")]
    [InlineData("customattribute1",  "extensionAttribute1")]
    [InlineData("CustomAttribute15", "extensionAttribute15")]
    [InlineData("customattribute7",  "extensionAttribute7")]
    public void Resolve_CustomAttributeHeader_ReturnsExtensionAttribute(string header, string expectedLdap)
    {
        Assert.Equal(expectedLdap, AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("msDS-cloudExtensionAttribute1")]
    [InlineData("employeeID")]
    [InlineData("someRawLdapName")]
    public void Resolve_UnknownHeader_ReturnsHeaderVerbatim(string header)
    {
        Assert.Equal(header, AttributeColumnMap.Resolve(header));
    }

    [Fact]
    public void WellKnownAttributes_HasExpectedCount()
    {
        // 8 HR attributes + 15 custom = 23
        Assert.Equal(23, AttributeColumnMap.WellKnownAttributes.Count);
    }
}
