using Umbraco.Commerce.Core.PaymentProviders;

namespace Umbraco.Commerce.PaymentProviders.Worldpay
{
    public class WorldpayBusinessGateway350Settings
    {
        // TODO: add support for validation settings
        // PaymentProviderSetting doesn't have validation property to set mandatory option.

        [PaymentProviderSetting(
            Name = "Continue Url",
            Description = "The Continue URL",
            SortOrder = 1000)]
        public string ContinueUrl { get; set; } = default!;

        [PaymentProviderSetting(
            Name = "Cancel Url",
            Description = "The Cancel URL",
            SortOrder = 2000)]
        public string CancelUrl { get; set; } = default!;

        [PaymentProviderSetting(
            Name = "Error Url",
            Description = "The Error URL",
            SortOrder = 3000)]
        public string ErrorUrl { get; set; } = default!;

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
            Description = "A unique 27-character reference assigned for the website",
            SortOrder = 9000)]
        public string InstallationReference { get; set; } = default!;

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/enhancing-security-with-md5#if-you-use-a-bespoke-setup"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = "MD5 Secret",
            Description = "The Worldpay MD5 secret to use when create MD5 hashes",
            SortOrder = 13000)]
        public string? Md5Secret { get; set; }

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/enhancing-security-with-md5#if-you-use-a-bespoke-setup"/>
        /// </summary>
        [PaymentProviderSetting(
            Name = "Signature Pattern",
            Description = "The Worldpay's \"SignatureFields\" property. Required if MD5 Secret is not empty. Example value: instId:amount:currency:cartId:MC_userId",
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
        public bool IsCaptureEnabled { get; set; }

        [PaymentProviderSetting(
            Name = "Test Mode",
            Description = "Set whether to process payments in test mode",
            SortOrder = 17000)]
        public bool IsTestModeEnabled { get; set; }

        #region Advanced

        [PaymentProviderSetting(
            Name = "Verbose Logging",
            Description = "Enable verbose logging",
            IsAdvanced = true,
            SortOrder = 18000)]
        public bool IsVerboseLoggingEnabled { get; set; }

        #endregion
    }
}
