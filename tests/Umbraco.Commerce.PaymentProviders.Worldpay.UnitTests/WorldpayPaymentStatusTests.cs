using Umbraco.Commerce.Core.Models;
using Provider = Umbraco.Commerce.PaymentProviders.Worldpay.WorldpayBusinessGateway350PaymentProvider;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.UnitTests;

public class WorldpayPaymentStatusTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Full_Authorisation_Is_Captured(bool capture)
        => Assert.Equal(PaymentStatus.Captured, Provider.GetPaymentStatus("A", capture));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Pre_Authorisation_Is_Authorized(bool capture)
        => Assert.Equal(PaymentStatus.Authorized, Provider.GetPaymentStatus("E", capture));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("O")]
    public void An_Unknown_Auth_Mode_Uses_The_Capture_Setting(string? authMode)
    {
        Assert.Equal(PaymentStatus.Captured, Provider.GetPaymentStatus(authMode!, capture: true));
        Assert.Equal(PaymentStatus.Authorized, Provider.GetPaymentStatus(authMode!, capture: false));
    }
}
