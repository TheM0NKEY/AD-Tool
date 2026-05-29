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
    [InlineData("EmployeeID",   "employeeID")]
    [InlineData("Employee ID",  "employeeID")]
    public void Resolve_WellKnownHeader_ReturnsLdapName(string header, string expectedLdap)
    {
        Assert.Equal(expectedLdap, AttributeColumnMap.Resolve(header));
    }

    [Theory]
    [InlineData("CloudAttribute1",    "msDS-cloudExtensionAttribute1")]
    [InlineData("cloudattribute1",    "msDS-cloudExtensionAttribute1")]
    [InlineData("Cloud Attribute 1",  "msDS-cloudExtensionAttribute1")]
    [InlineData("CloudAttribute7",    "msDS-cloudExtensionAttribute7")]
    [InlineData("CloudAttribute15",   "msDS-cloudExtensionAttribute15")]
    [InlineData("Cloud Attribute 20", "msDS-cloudExtensionAttribute20")]
    public void Resolve_CloudAttributeHeader_ReturnsMsDsCloudExtensionAttribute(string header, string expectedLdap)
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
        // 8 HR attributes + 20 cloud extension attributes = 28
        Assert.Equal(28, AttributeColumnMap.WellKnownAttributes.Count);
    }
}
