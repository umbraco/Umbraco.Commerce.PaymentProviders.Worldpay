using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;

namespace Umbraco.Commerce.PaymentProviders.Worldpay
{
    public class WorldpayBusinessGateway350Settings
    {
        // TODO: add support for validation settings: https://github.com/umbraco/Umbraco.Commerce.Issues/discussions/546
        // PaymentProviderSetting doesn't have validation property to set mandatory option.

        [PaymentProviderSetting(
            Name = "Continue Url",
            Description = "The Continue URL (required)",
            SortOrder = 1000)]
        public string? ContinueUrl { get; set; }

        [PaymentProviderSetting(
            Name = "Cancel Url",
            Description = "The Cancel URL (required)",
            SortOrder = 2000)]
        public string? CancelUrl { get; set; }

        [PaymentProviderSetting(
            Name = "Error Url",
            Description = "The Error URL (required)",
            SortOrder = 3000)]
        public string? ErrorUrl { get; set; }

        [PaymentProviderSetting(
            Name = "Billing Property First Name",
            Description = "The order property alias containing the first name of the customer",
            SortOrder = 4000)]
        public string? BillingFirstNamePropertyAlias { get; set; }

        [PaymentProviderSetting(
            Name = "Billing Property Last Name",
            Description = "The order property alias containing the last name of the customer",
            SortOrder = 5000)]
        public string? BillingLastNamePropertyAlias { get; set; }

        [PaymentProviderSetting(
            Name = "Billing Address (Line 1) Property Alias",
            Description = "The order property alias containing line 1 of the billing address",
            SortOrder = 6000)]
        public string? BillingAddressLine1PropertyAlias { get; set; }

        [PaymentProviderSetting(
            Name = "Billing Address City Property Alias",
            Description = "The order property alias containing the city of the billing address",
            SortOrder = 7000)]
        public string? BillingAddressCityPropertyAlias { get; set; }

        [PaymentProviderSetting(
            Name = "Billing Address ZipCode Property Alias",
            Description = "The order property alias containing the zip code of the billing address",
            SortOrder = 8000)]
        public string? BillingAddressZipCodePropertyAlias { get; set; }

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#parameters-explained"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = "Installation Reference",
            Description = "A unique 27-character reference assigned for the website (required)",
            SortOrder = 9000)]
        public string? InstallId { get; set; }

        private const string Md5SecretPropertyName = "MD5 Secret";
        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/enhancing-security-with-md5#if-you-use-a-bespoke-setup"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = Md5SecretPropertyName,
            Description = "The Worldpay MD5 secret to use when create MD5 hashes",
            SortOrder = 13000)]
        public string? Md5Secret { get; set; }

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/enhancing-security-with-md5#if-you-use-a-bespoke-setup"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = "Signature Pattern",
            Description = $"The Worldpay's \"SignatureFields\" property. Example value: amount:currency:instId:cartId (required if {Md5SecretPropertyName} is not empty)",
            SortOrder = 14000)]
        public string? SignaturePattern { get; set; }

        [PaymentProviderSetting(
            Name = "Response Password",
            Description = "The Worldpay payment response password to use to valida payment responses",
            SortOrder = 15000)]
        public string? ResponsePassword { get; set; }

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#optional-parameters"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = "Capture",
            Description = "Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture",
            SortOrder = 16000)]
        public bool Capture { get; set; }

        [PaymentProviderSetting(
            Name = "Test Mode",
            Description = "Set whether to process payments in test mode",
            SortOrder = 17000)]
        public bool TestMode { get; set; }

        #region Advanced

        [PaymentProviderSetting(
            Name = "Verbose Logging",
            Description = "Enable verbose logging",
            IsAdvanced = true,
            SortOrder = 18000)]
        public bool VerboseLogging { get; set; }

        [PaymentProviderSetting(
            Name = "Disable Cancel Url",
            Description = $"If this option enabled, then \"{WorldpayParameters.Request.Custom.CancelUrl}\" won't be included in generated form",
            IsAdvanced = true,
            SortOrder = 19000)]
        public bool DisableCancelUrl { get; set; }

        [PaymentProviderSetting(
            Name = "Disable Return Url",
            Description = $"If this option enabled, then \"{WorldpayParameters.Request.Custom.ReturnUrl}\" won't be included in generated form",
            IsAdvanced = true,
            SortOrder = 19001)]
        public bool DisableReturnUrl { get; set; }

        [PaymentProviderSetting(
            Name = "Disable Callback Url",
            Description = $"If this option enabled, then \"{WorldpayParameters.Request.Custom.CallbackUrl}\" won't be included in generated form",
            IsAdvanced = true,
            SortOrder = 19002)]
        public bool DisableCallbackUrl { get; set; }

        #endregion
    }
}
