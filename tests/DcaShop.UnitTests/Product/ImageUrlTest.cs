using DcaShop.Product.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class ImageUrlTest
{
    [Fact]
    public void RefusesAnAddressLongerThan2000Characters()
    {
        var overlong = "https://images.example.com/" + new string('a', 2001 - "https://images.example.com/".Length);

        Assert.Throws<ArgumentException>(() => ImageUrl.Of(overlong));
    }
}