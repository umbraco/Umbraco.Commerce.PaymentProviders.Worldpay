using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Umbraco.Commerce.PaymentProviders.Worldpay.Helpers;
using CheckResult = Umbraco.Commerce.PaymentProviders.Worldpay.WorldpayBusinessGateway350PaymentProvider.CallbackCheckResult;
using Provider = Umbraco.Commerce.PaymentProviders.Worldpay.WorldpayBusinessGateway350PaymentProvider;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.UnitTests;

public class WorldpayCallbackTests
{
    private const string CallbackBody = "msgType=authResult&transStatus=Y&transId=123456&authAmount=12.50&authCurrency=GBP&callbackPW=s3cret&authMode=A";

    private static DefaultHttpContext CreateCallbackRequest(string body, string queryString = "")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.QueryString = new QueryString(queryString);
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return httpContext;
    }

    private static NameValueCollection Form(string body) => System.Web.HttpUtility.ParseQueryString(body);

    [Fact]
    public async Task Callback_Data_Is_Read_From_The_Posted_Form()
    {
        DefaultHttpContext httpContext = CreateCallbackRequest(CallbackBody);

        Provider.WorldpayCallbackData data = await Provider.GetCallbackDataAsync(new Dictionary<string, object>(), httpContext.Request).ConfigureAwait(true);

        Assert.Equal("authResult", data.Form["msgType"]);
        Assert.Equal("Y", data.Form["transStatus"]);
        Assert.Equal("12.50", data.Form["authAmount"]);
    }

    [Fact]
    public async Task Callback_Data_Stored_While_Finding_The_Order_Is_Reused_When_Processing_The_Callback()
    {
        var additionalData = new Dictionary<string, object>();

        // First read, as done when locating the order
        await Provider.GetCallbackDataAsync(additionalData, CreateCallbackRequest(CallbackBody).Request).ConfigureAwait(true);

        // Second read, when processing the callback; the body has already been consumed
        Provider.WorldpayCallbackData data = await Provider.GetCallbackDataAsync(additionalData, CreateCallbackRequest(string.Empty).Request).ConfigureAwait(true);

        Assert.IsType<NameValueCollection>(additionalData["formData"]);
        Assert.Equal("Y", data.Form["transStatus"]);
        Assert.Equal("123456", data.Form["transId"]);
    }

    [Fact]
    public void Callback_Is_Refused_When_No_Response_Password_Is_Configured()
        => Assert.Equal(CheckResult.ResponsePasswordNotConfigured, Provider.CheckResponsePassword(Form(CallbackBody), string.Empty));

    [Fact]
    public void Callback_Is_Refused_When_The_Password_Does_Not_Match()
        => Assert.Equal(CheckResult.ResponsePasswordMismatch, Provider.CheckResponsePassword(Form(CallbackBody), "other"));

    [Fact]
    public void Callback_Is_Refused_When_The_Password_Is_Missing()
        => Assert.Equal(CheckResult.ResponsePasswordMismatch, Provider.CheckResponsePassword(Form("transStatus=Y"), "s3cret"));

    [Fact]
    public void Callback_Is_Accepted_When_The_Password_Matches()
        => Assert.Equal(CheckResult.Valid, Provider.CheckResponsePassword(Form(CallbackBody), "s3cret"));

    [Fact]
    public void Authorised_Payment_Matching_The_Order_Is_Accepted()
    {
        CheckResult result = Provider.CheckAuthorisedPayment(Form(CallbackBody), "12.50", "GBP", out var amount);

        Assert.Equal(CheckResult.Valid, result);
        Assert.Equal(12.50m, amount);
    }

    [Fact]
    public void Authorised_Amount_Lower_Than_The_Order_Total_Is_Refused()
        => Assert.Equal(CheckResult.AmountMismatch, Provider.CheckAuthorisedPayment(Form(CallbackBody), "125.00", "GBP", out _));

    [Fact]
    public void Authorised_Currency_Different_From_The_Order_Currency_Is_Refused()
        => Assert.Equal(CheckResult.CurrencyMismatch, Provider.CheckAuthorisedPayment(Form(CallbackBody), "12.50", "EUR", out _));

    [Theory]
    [InlineData("")]
    [InlineData("12,50")]
    [InlineData("-12.50")]
    [InlineData("abc")]
    public void Missing_Or_Invalid_Authorised_Amount_Is_Refused(string authAmount)
        => Assert.Equal(
            CheckResult.AmountMissingOrInvalid,
            Provider.CheckAuthorisedPayment(Form($"authAmount={authAmount}&authCurrency=GBP"), "12.50", "GBP", out _));

    [Fact]
    public void Authorised_Amount_Is_Read_The_Same_Way_In_Any_Culture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");

            Assert.Equal(CheckResult.Valid, Provider.CheckAuthorisedPayment(Form(CallbackBody), Provider.FormatAmount(12.5m), "GBP", out var amount));
            Assert.Equal(12.50m, amount);
            Assert.Equal("1250.00", Provider.FormatAmount(1250m));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Whole_Number_Authorised_Amount_Matches_The_Formatted_Order_Total()
        => Assert.Equal(
            CheckResult.Valid,
            Provider.CheckAuthorisedPayment(Form("authAmount=1000&authCurrency=JPY"), "1000.00", "JPY", out _));

    [Theory]
    [InlineData("s3cret", "s3cret", true)]
    [InlineData("s3cret", "s3cret ", false)]
    [InlineData("s3cret", "S3cret", false)]
    [InlineData("s3cret", "", false)]
    [InlineData("s3cret", null, false)]
    public void Password_Comparison(string expected, string? actual, bool equal)
        => Assert.Equal(equal, Provider.FixedTimeEquals(expected, actual!));

    [Fact]
    public void Logged_Callback_Data_Does_Not_Contain_The_Callback_Password()
    {
        string logged = Form(CallbackBody).ToMaskedFriendlyString();

        Assert.DoesNotContain("s3cret", logged, StringComparison.Ordinal);
        Assert.Contains("callbackPW=***", logged, StringComparison.Ordinal);
        Assert.Contains("transId=123456", logged, StringComparison.Ordinal);
    }
}
