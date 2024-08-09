using System.Globalization;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.Core.Services;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;
using Umbraco.Commerce.PaymentProviders.Worldpay.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Helpers;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Factories
{
    internal class PaymentFormFactory
    {
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<WorldpayBusinessGateway350PaymentProvider> _logger;

        public PaymentFormFactory(
            ICountryService countryService,
            ICurrencyService currencyService,
            ILogger<WorldpayBusinessGateway350PaymentProvider> logger)
        {
            _countryService = countryService;
            _currencyService = currencyService;
            _logger = logger;
        }

        public PaymentForm Create(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var orderDetails = GetOrderDetails(context);

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Form data: {Values}", IDictionaryExtensions.ToFriendlyString(orderDetails));
            }

            var action = GetFormAction(context);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("Payment url: {Url}", action);
            }

            var form = new PaymentForm(action, PaymentFormMethod.Post)
            {
                Inputs = orderDetails
            };

            return form;
        }

        private IDictionary<string, string> GetOrderDetails(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context.Settings.InstallId);

            var billingCountryCode = GetBillingCountryCode(context);
            var currencyCode = GetCurrencyCode(context);
            var address1 = context.Order.Properties[context.Settings.BillingAddressLine1PropertyAlias] ?? string.Empty;
            var city = context.Order.Properties[context.Settings.BillingAddressCityPropertyAlias] ?? string.Empty;
            var postcode = context.Order.Properties[context.Settings.BillingAddressZipCodePropertyAlias] ?? string.Empty;
            var amount = context.Order.TransactionAmount.Value.Value.ToString("0.00", CultureInfo.InvariantCulture);
            var orderReference = context.Order.GenerateOrderReference();
            var firstName = string.IsNullOrEmpty(context.Settings.BillingFirstNamePropertyAlias)
                ? context.Order.CustomerInfo.FirstName
                : context.Order.Properties[context.Settings.BillingFirstNamePropertyAlias];
            var lastName = string.IsNullOrEmpty(context.Settings.BillingLastNamePropertyAlias)
                ? context.Order.CustomerInfo.LastName
                : context.Order.Properties[context.Settings.BillingLastNamePropertyAlias];
            var authMode = context.Settings.Capture
                ? PaymentStatus.Captured.ToAuthMode()
                : PaymentStatus.Authorized.ToAuthMode();
            var testMode = context.Settings.TestMode
                ? WorldpayValues.Request.TestMode.Enabled
                : WorldpayValues.Request.TestMode.Disabled;

            var orderDetails = new Dictionary<string, string>
            {
                { WorldpayParameters.Request.InstId, context.Settings.InstallId },
                { WorldpayParameters.Request.TestMode, testMode },
                { WorldpayParameters.Request.AuthMode, authMode },
                { WorldpayParameters.Request.CartId, context.Order.OrderNumber },
                { WorldpayParameters.Request.Amount, amount },
                { WorldpayParameters.Request.Currency, currencyCode },
                { WorldpayParameters.Request.Name, $"{firstName} {lastName}" },
                { WorldpayParameters.Request.Email, context.Order.CustomerInfo.Email },
                { WorldpayParameters.Request.Address1, address1 },
                { WorldpayParameters.Request.Town, city },
                { WorldpayParameters.Request.Postcode, postcode },
                { WorldpayParameters.Request.Country, billingCountryCode },
                { WorldpayParameters.Request.Custom.OrderReference, orderReference },
                { WorldpayParameters.Request.Custom.CancellUrl, context.Urls.CancelUrl },
                { WorldpayParameters.Request.Custom.ReturnUrl, context.Urls.ContinueUrl },
                { WorldpayParameters.Request.Custom.CallbackUrl, context.Urls.CallbackUrl }
            };

            if (!string.IsNullOrEmpty(context.Settings.Md5Secret))
            {
                var signatureBody = SignatureHelper.EnrichPatternWithValues(context.Settings, orderDetails);

                if (context.Settings.VerboseLogging)
                {
                    _logger.Info("Signature body: {SignatureBody}", signatureBody);
                }

                var signature = SignatureHelper.CreateSignature(signatureBody);

                if (context.Settings.VerboseLogging)
                {
                    _logger.Info("Signature result: {Signature}", signature);
                }

                orderDetails.Add(WorldpayParameters.Request.Signature, signature);
            }

            return orderDetails;
        }

        private string GetCurrencyCode(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            var currency = _currencyService.GetCurrency(context.Order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode)) // Ensure currency has valid ISO 4217 code
            {
                throw new InvalidOperationException($"Currency must be a valid ISO 4217 currency code: {currency.Name}");
            }

            return currencyCode;
        }

        private string GetBillingCountryCode(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context.Order.PaymentInfo.CountryId);

            var billingCountry = _countryService.GetCountry(context.Order.PaymentInfo.CountryId.Value);

            var billingCountryCode = billingCountry.Code.ToUpperInvariant();

            var iso3166Countries = _countryService.GetIso3166CountryRegions();
            if (iso3166Countries.All(x => x.Code != billingCountryCode)) // Ensure billing country has valid ISO 3166 code
            {
                throw new InvalidOperationException($"Country must be a valid ISO 3166 billing country code: {billingCountry.Name}");
            }

            return billingCountryCode;
        }

        private static string GetFormAction(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            const string LIVE_BASE_URL = "https://secure.worldpay.com/wcc/purchase";
            const string TEST_BASE_URL = "https://secure-test.worldpay.com/wcc/purchase";

            var url = context.Settings.TestMode ? TEST_BASE_URL : LIVE_BASE_URL;

            return url;
        }
    }
}
