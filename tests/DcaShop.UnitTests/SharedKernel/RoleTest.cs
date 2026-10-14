using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.SharedKernel;

public sealed class RoleTest
{
    [Theory]
    [InlineData("CUSTOMER")]
    [InlineData("STAFF")]
    public void APublishedCodeNamesItsRole(string code) => Assert.Equal(code, Role.FromCode(code)!.Code);

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("staff")]
    [InlineData("")]
    public void ACodeNoRoleCarriesNamesNone(string code) => Assert.Null(Role.FromCode(code));
}