using Umbraco.Commerce.Core.PaymentProviders;

namespace Umbraco.Commerce.PaymentProviders.Worldpay
{
    public class WorldpayBusinessGateway350Settings
    {
        [PaymentProviderSetting(SortOrder = 1000)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(SortOrder = 2000)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(SortOrder = 3000)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(SortOrder = 4000)]
        public string BillingFirstNamePropertyAlias { get; set; }

        [PaymentProviderSetting(SortOrder = 5000)]
        public string BillingLastNamePropertyAlias { get; set; }

        [PaymentProviderSetting(SortOrder = 6000)]
        public string BillingAddressLine1PropertyAlias { get; set; }

        [PaymentProviderSetting(SortOrder = 7000)]
        public string BillingAddressCityPropertyAlias { get; set; }

        [PaymentProviderSetting(SortOrder = 8000)]
        public string BillingAddressZipCodePropertyAlias { get; set; }

        [PaymentProviderSetting(SortOrder = 9000)]
        public string InstallId { get; set; }

        [PaymentProviderSetting(SortOrder = 13000)]
        public string Md5Secret { get; set; }

        [PaymentProviderSetting(SortOrder = 14000)]
        public string ResponsePassword { get; set; }

        [PaymentProviderSetting(SortOrder = 14000)]
        public bool Capture { get; set; }

        [PaymentProviderSetting(
            SortOrder = 15000)]
        public bool TestMode { get; set; }

        // ============================
        // Advanced
        // ============================

        [PaymentProviderSetting(IsAdvanced = true, SortOrder = 16000)]
        public bool VerboseLogging { get; set; }
    }
}
