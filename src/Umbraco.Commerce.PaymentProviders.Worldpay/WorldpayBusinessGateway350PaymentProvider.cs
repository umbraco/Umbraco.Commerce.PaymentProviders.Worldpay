using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;
using Umbraco.Commerce.PaymentProviders.Worldpay.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Helpers;

namespace Umbraco.Commerce.PaymentProviders.Worldpay
{
    /// <summary>
    /// Docs: <see href="https://developerengine.fisglobal.com/apis/bg350"/>
    /// </summary>
    [PaymentProvider("worldpay-bs350", "Worldpay Business Gateway 350", "Worldpay Business Gateway 350 payment provider", Icon = "icon-credit-card")]
    public class WorldpayBusinessGateway350PaymentProvider : PaymentProviderBase<WorldpayBusinessGateway350Settings>
    {
        private const string LiveBaseUrl = "https://secure.worldpay.com/wcc/purchase";
        private const string TestBaseUrl = "https://secure-test.worldpay.com/wcc/purchase";

        private readonly ILogger<WorldpayBusinessGateway350PaymentProvider> _logger;

        public override bool FinalizeAtContinueUrl { get; } = false;

        public WorldpayBusinessGateway350PaymentProvider(
            UmbracoCommerceContext ctx,
            ILogger<WorldpayBusinessGateway350PaymentProvider> logger)
            : base(ctx)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string GetCancelUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Settings);
            ArgumentNullException.ThrowIfNull(context.Settings.CancelUrl);

            return context.Settings.CancelUrl;
        }

        public override string GetContinueUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Settings);
            ArgumentNullException.ThrowIfNull(context.Settings.ContinueUrl);

            return context.Settings.ContinueUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Settings);
            ArgumentNullException.ThrowIfNull(context.Settings.ErrorUrl);

            return context.Settings.ErrorUrl;
        }

        public override Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            try
            {
                if (context.Settings.VerboseLogging)
                {
                    _logger.Info("{MethodName} method called for cart {OrderNumber}", nameof(GenerateFormAsync), context.Order.OrderNumber);
                }

                ArgumentNullException.ThrowIfNull(context.Settings.InstallId);

                if (!context.Order.PaymentInfo.CountryId.HasValue)
                {
                    throw new InvalidOperationException($"{nameof(context.Order.PaymentInfo.CountryId)} was null");
                }

                var billingCountry = Context.Services.CountryService.GetCountry(context.Order.PaymentInfo.CountryId.Value);
                var billingCountryCode = billingCountry.Code.ToUpperInvariant();

                // Ensure billing country has valid ISO 3166 code
                var iso3166Countries = Context.Services.CountryService.GetIso3166CountryRegions();
                if (iso3166Countries.All(x => x.Code != billingCountryCode))
                {
                    throw new InvalidOperationException($"Country must be a valid ISO 3166 billing country code: {billingCountry.Name}");
                }

                var currency = Context.Services.CurrencyService.GetCurrency(context.Order.CurrencyId);
                var currencyCode = currency.Code.ToUpperInvariant();

                // Ensure currency has valid ISO 4217 code
                if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
                {
                    throw new InvalidOperationException($"Currency must be a valid ISO 4217 currency code: {currency.Name}");
                }

                var firstName = string.IsNullOrEmpty(context.Settings.BillingFirstNamePropertyAlias)
                    ? context.Order.CustomerInfo.FirstName
                    : context.Order.Properties[context.Settings.BillingFirstNamePropertyAlias];
                var lastName = string.IsNullOrEmpty(context.Settings.BillingLastNamePropertyAlias)
                    ? context.Order.CustomerInfo.LastName
                    : context.Order.Properties[context.Settings.BillingLastNamePropertyAlias];
                var address1 = context.Order.Properties[context.Settings.BillingAddressLine1PropertyAlias] ?? string.Empty;
                var city = context.Order.Properties[context.Settings.BillingAddressCityPropertyAlias] ?? string.Empty;
                var postcode = context.Order.Properties[context.Settings.BillingAddressZipCodePropertyAlias] ?? string.Empty;
                var amount = context.Order.TransactionAmount.Value.Value.ToString("0.00", CultureInfo.InvariantCulture);
                var orderReference = context.Order.GenerateOrderReference();

                var orderDetails = new Dictionary<string, string>
                {
                    { WorldPayField.INSTALLATION_ID, context.Settings.InstallId },
                    { WorldPayField.TEST_MODE, context.Settings.TestMode ? "100" : "0" },
                    { WorldPayField.AUTH_MODE, context.Settings.IsFullAuthorisationEnabled ? "A" : "E" },
                    { WorldPayField.CART_ID, context.Order.OrderNumber },
                    { WorldPayField.AMOUNT, amount },
                    { WorldPayField.CURRENCY, currencyCode },
                    { WorldPayField.NAME, $"{firstName} {lastName}" },
                    { WorldPayField.EMAIL, context.Order.CustomerInfo.Email },
                    { WorldPayField.ADDRESS_1, address1 },
                    { WorldPayField.TOWN, city },
                    { WorldPayField.POSTCODE, postcode },
                    { WorldPayField.COUNTRY, billingCountryCode },
                    { WorldPayField.Custom.ORDER_REFERENCE, orderReference },
                    { WorldPayField.Custom.CANCEL_URL, context.Urls.CancelUrl },
                    { WorldPayField.Custom.RETURN_URL, context.Urls.ContinueUrl },
                    { WorldPayField.Custom.CALLBACK_URL, context.Urls.CallbackUrl }
                };

                if (!string.IsNullOrEmpty(context.Settings.Md5Secret))
                {
                    var signatureBody = SignatureHelper.EnrichPatternWithValues(context.Settings, orderDetails);
                    var signature = SignatureHelper.CreateSignature(signatureBody);

                    orderDetails.Add(WorldPayField.SIGNATURE, signature);

                    if (context.Settings.VerboseLogging)
                    {
                        _logger.Info("Before Md5: {SignatureBody}", signatureBody);
                        _logger.Info("Signature: {Signature}", signature);
                    }
                }

                var url = context.Settings.TestMode ? TestBaseUrl : LiveBaseUrl;

                var form = new PaymentForm(url, PaymentFormMethod.Post)
                {
                    Inputs = orderDetails
                };

                if (context.Settings.VerboseLogging)
                {
                    _logger.Info("Payment url {Url}", url);
                    _logger.Info("Form data {Values}", orderDetails.ToFriendlyString());
                }

                var paymentFormresult = new PaymentFormResult()
                {
                    Form = form
                };

                return Task.FromResult(paymentFormresult);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception thrown for cart {OrderNumber}", context.Order.OrderNumber);

                throw;
            }
        }

        public override async Task<OrderReference?> GetOrderReferenceAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Request);
            ArgumentNullException.ThrowIfNull(context.Settings);

            var queryData = GetQueryData(context);
            var formData = await GetFormDataAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("Worldpay data {FormData}", formData.ToFriendlyString());
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    if (context.Settings.VerboseLogging)
                    {
                        _logger.Info($"{nameof(context.Settings.ResponsePassword)} was incorrect");
                    }

                    return null;
                }
            }

            if (OrderReference.TryParse(formData[WorldPayField.Custom.ORDER_REFERENCE], out var orderReference))
            {
                return orderReference;
            }

            return await base.GetOrderReferenceAsync(context, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken = default)
        {
            // We can't rely on the request stream processing logic inside GetOrderReferenceAsync
            // GetOrderReferenceAsync called only when inbound request has following pattern: {domain name}/umbraco/commerce/payment/callback/worldpay-bs350/{payment method id}
            // In case when inbound request has following pattern: {domain name}/umbraco/commerce/payment/callback/worldpay-bs350/{payment method id}/{order number]/{hash}
            // GetOrderReferenceAsync won't be triggered.

            var queryData = GetQueryData(context);
            var formData = await GetFormDataAsync(context, cancellationToken).ConfigureAwait(false);

            if (formData[WorldPayField.MESSAGE_TYPE] != "authResult")
            {
                return CallbackResult.Ok();
            }

            _logger.Info("Payment call back for cart {OrderNumber}", context.Order.OrderNumber);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("Worldpay data {Data}", formData.ToFriendlyString());
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    _logger.Info("Payment call back for cart {OrderNumber} response password incorrect", context.Order.OrderNumber);

                    return CallbackResult.Ok();
                }
            }

            // if still here, password was not required or matched
            if (formData[WorldPayField.TRANSACTION_STATUS] == "Y")
            {
                var rawTotalAmount = formData[WorldPayField.AUTHORIZED_AMOUNT]
                    ?? throw new InvalidOperationException($"Value of {nameof(formData)}[{WorldPayField.AUTHORIZED_AMOUNT}] was null");
                var totalAmount = decimal.Parse(rawTotalAmount, CultureInfo.InvariantCulture);
                var transactionId = formData[WorldPayField.TRANSACTION_ID];

                // TODO: does this condition right?
                // Read more: https://github.com/umbraco/Umbraco.Commerce.PaymentProviders.Worldpay/issues/5
                var paymentStatus = formData[WorldPayField.AUTH_MODE] == "A" ? PaymentStatus.Captured : PaymentStatus.Authorized;

                _logger.Info("Payment call back for cart {OrderNumber} payment authorised", context.Order.OrderNumber);

                var transactionInfo = new TransactionInfo
                {
                    AmountAuthorized = totalAmount,
                    TransactionFee = 0m,
                    TransactionId = transactionId,
                    PaymentStatus = paymentStatus
                };

                return CallbackResult.Ok(transactionInfo);
            }

            _logger.Info($"Payment call back for cart {context.Order.OrderNumber} payment not authorised or error");

            return CallbackResult.Ok();
        }

        private static bool IsResponsePasswordValid(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, NameValueCollection formData) => context.Settings.ResponsePassword == formData[WorldPayField.CALLBACK_PASSWORD];

        private static NameValueCollection GetQueryData(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);

            const string KEY = "queryData";

            if (context.AdditionalData.TryGetValue(KEY, out var data))
            {
                return (NameValueCollection)data;
            }

            var requestUri = context.Request.RequestUri
                ?? throw new InvalidOperationException($"{nameof(context.Request.RequestUri)} was null");

            var queryData = HttpUtility.ParseQueryString(requestUri.Query);

            context.AdditionalData.Add(KEY, queryData);

            return queryData;
        }

        private static async Task<NameValueCollection> GetFormDataAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            const string KEY = "formData";

            if (context.AdditionalData.TryGetValue(KEY, out var data))
            {
                return (NameValueCollection)data;
            }

            var formData = await context.Request.Content.ReadAsFormDataAsync(cancellationToken).ConfigureAwait(false);

            context.AdditionalData.Add(KEY, formData);

            return formData;
        }
    }
}
