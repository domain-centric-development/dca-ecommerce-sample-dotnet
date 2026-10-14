using DcaShop.Product.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class ImageUrlTest
{
    private const string Prefix = "https://images.example.com/";

    [Fact]
    public void AcceptsAnAddressOfExactly2000Characters()
    {
        var longest = Prefix + new string('a', 2000 - Prefix.Length);

        Assert.Equal(longest, ImageUrl.Of(longest).Value);
    }

    [Fact]
    public void RefusesAnAddressLongerThan2000Characters()
    {
        var overlong = Prefix + new string('a', 2001 - Prefix.Length);

        Assert.Throws<ArgumentException>(() => ImageUrl.Of(overlong));
    }
}