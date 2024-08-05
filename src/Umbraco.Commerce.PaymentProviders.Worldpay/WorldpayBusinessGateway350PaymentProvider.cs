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
                    ? WorldpayValues.TestMode.Enabled
                    : WorldpayValues.TestMode.Disabled;

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
                    var signature = SignatureHelper.CreateSignature(signatureBody);

                    orderDetails.Add(WorldpayParameters.Request.Signature, signature);

                    if (context.Settings.VerboseLogging)
                    {
                        _logger.Info("Before Md5: {SignatureBody}", signatureBody);
                        _logger.Info("Signature: {Signature}", signature);
                    }
                }

                var action = GetFormAction(context);

                if (context.Settings.VerboseLogging)
                {
                    _logger.Info("Payment url {Url}", action);
                    // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                    _logger.Info("Form data {Values}", IDictionaryExtensions.ToFriendlyString(orderDetails));
                }

                var form = new PaymentForm(action, PaymentFormMethod.Post)
                {
                    Inputs = orderDetails
                };

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

            var formData = await GetFormDataAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Worldpay data {FormData}", NameValueCollectionExtensions.ToFriendlyString(formData));
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    if (context.Settings.VerboseLogging)
                    {
                        _logger.Warn($"{nameof(context.Settings.ResponsePassword)} was incorrect");
                    }

                    return null;
                }
            }

            if (OrderReference.TryParse(formData[WorldpayParameters.Request.Custom.OrderReference], out var orderReference))
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

            const string MessageTypeValue = "authResult";
            if (queryData[WorldpayParameters.Query.MsgType] != MessageTypeValue)
            {
                if (context.Settings.VerboseLogging)
                {
                    // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                    _logger.Warn("Worldpay callback doesn't have {Value} in query: {Query}", MessageTypeValue, NameValueCollectionExtensions.ToFriendlyString(queryData));
                }

                return CallbackResult.Ok();
            }

            var formData = await GetFormDataAsync(context, cancellationToken).ConfigureAwait(false);

            _logger.Info("Payment call back for cart {OrderNumber}", context.Order.OrderNumber);

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Worldpay data {Data}", NameValueCollectionExtensions.ToFriendlyString(formData));
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    _logger.Warn("Payment call back for cart {OrderNumber} response password incorrect", context.Order.OrderNumber);

                    return CallbackResult.Ok();
                }
            }

            // if still here, password was not required or matched

            var transactionStatus = formData[WorldpayParameters.Response.TransStatus];
            if (transactionStatus != WorldpayValues.TransStatus.Succeed)
            {
                _logger.Error($"Payment call back for cart {context.Order.OrderNumber} payment not authorised or error: transaction status = {transactionStatus}");

                return CallbackResult.Ok();
            }

            var rawTotalAmount = formData[WorldpayParameters.Response.AuthAmount] ?? throw new InvalidOperationException($"Value of {nameof(formData)}[{WorldpayParameters.Response.AuthAmount}] was null");
            var totalAmount = decimal.Parse(rawTotalAmount, CultureInfo.InvariantCulture);
            var transactionId = formData[WorldpayParameters.Response.TransId];
            var paymentStatus = formData[WorldpayParameters.Request.AuthMode] == WorldpayValues.AuthMode.FullAuthorisation
                ? PaymentStatus.Captured
                : PaymentStatus.Authorized;

            _logger.Info("Payment call back for cart {OrderNumber} payment {Action}",
                context.Order.OrderNumber,
                paymentStatus.ToString().ToLowerInvariant()
            );

            var transactionInfo = new TransactionInfo
            {
                AmountAuthorized = totalAmount,
                TransactionFee = 0m,
                TransactionId = transactionId,
                PaymentStatus = paymentStatus,
            };

            return CallbackResult.Ok(transactionInfo);
        }

        private static string GetFormAction(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            const string LIVE_BASE_URL = "https://secure.worldpay.com/wcc/purchase";
            const string TEST_BASE_URL = "https://secure-test.worldpay.com/wcc/purchase";

            var url = context.Settings.TestMode ? TEST_BASE_URL : LIVE_BASE_URL;

            return url;
        }

        private string GetCurrencyCode(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            var currency = Context.Services.CurrencyService.GetCurrency(context.Order.CurrencyId);
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

            var billingCountry = Context.Services.CountryService.GetCountry(context.Order.PaymentInfo.CountryId.Value);

            var billingCountryCode = billingCountry.Code.ToUpperInvariant();

            var iso3166Countries = Context.Services.CountryService.GetIso3166CountryRegions();
            if (iso3166Countries.All(x => x.Code != billingCountryCode)) // Ensure billing country has valid ISO 3166 code
            {
                throw new InvalidOperationException($"Country must be a valid ISO 3166 billing country code: {billingCountry.Name}");
            }

            return billingCountryCode;
        }

        private static bool IsResponsePasswordValid(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, NameValueCollection formData) => context.Settings.ResponsePassword == formData[WorldpayParameters.Response.CallbackPW];

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
