using System.Collections.Specialized;
using Umbraco.Commerce.PaymentProviders.Worldpay.Helpers;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.UnitTests;

public class WorldpaySignatureTests
{
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("The quick brown fox jumps over the lazy dog", "9e107d9d372bb6826bd81d3542a419d6")]
    public void CreateMd5_Is_Lower_Case_Hex_Md5(string input, string expected)
        => Assert.Equal(expected, Md5Helper.CreateMd5(input));

    [Fact]
    public void ToHex_Writes_Each_Byte_As_Two_Digits()
    {
        byte[] bytes = [0x00, 0x0f, 0xa0, 0xff];

        Assert.Equal("000fa0ff", bytes.ToHex(true));
        Assert.Equal("000fa0ff", bytes.ToHex(false).ToLowerInvariant());
    }

    [Fact]
    public void Order_Signature_Is_The_Md5_Of_Secret_And_Default_Signature_Fields()
    {
        string signature = WorldpayBusinessGateway350PaymentProvider.CreateOrderSignature("s3cret", "12.50", "GBP", "1234567", "ORDER-0001");

        Assert.Equal(Md5Helper.CreateMd5("s3cret:12.50:GBP:1234567:ORDER-0001"), signature);
        Assert.Matches("^[0-9a-f]{32}$", signature);
    }

    [Fact]
    public void Order_Signature_Changes_With_The_Amount()
        => Assert.NotEqual(
            WorldpayBusinessGateway350PaymentProvider.CreateOrderSignature("s3cret", "12.50", "GBP", "1234567", "ORDER-0001"),
            WorldpayBusinessGateway350PaymentProvider.CreateOrderSignature("s3cret", "1.25", "GBP", "1234567", "ORDER-0001"));

    [Fact]
    public void ToFriendlyString_Lists_Dictionary_Entries()
        => Assert.Equal("{a=1,b=2}", new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }.ToFriendlyString());

    [Fact]
    public void ToFriendlyString_Lists_Every_Value_Of_A_NameValueCollection()
    {
        var nvc = new NameValueCollection { { "a", "1" }, { "a", "2" }, { "b", "3" } };

        Assert.Equal("{a=1,a=2,b=3}", nvc.ToFriendlyString());
    }
}
