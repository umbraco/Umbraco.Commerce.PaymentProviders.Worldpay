using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Helpers;

namespace Umbraco.Commerce.PaymentProviders.Worldpay
{
    [PaymentProvider("worldpay-bs350", Icon = "icon-credit-card")]
    public class WorldpayBusinessGateway350PaymentProvider : PaymentProviderBase<WorldpayBusinessGateway350Settings>
    {
        private const string LiveBaseUrl = "https://secure.worldpay.com/wcc/purchase";
        private const string TestBaseUrl = "https://secure-test.worldpay.com/wcc/purchase";

        private const string OrderReferenceField = "MC_ctx.OrderRef";
        private const string QueryDataKey = "queryData";
        private const string FormDataKey = "formData";

        private readonly ILogger<WorldpayBusinessGateway350PaymentProvider> _logger;

        public override bool FinalizeAtContinueUrl => false;

        public WorldpayBusinessGateway350PaymentProvider(
            UmbracoCommerceContext ctx,
            ILogger<WorldpayBusinessGateway350PaymentProvider> logger)
            : base(ctx)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public override string GetCancelUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            return ctx.Settings.CancelUrl;
        }

        public override string GetContinueUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ContinueUrl.MustNotBeNull("ctx.Settings.ContinueUrl");

            return ctx.Settings.ContinueUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ErrorUrl.MustNotBeNull("ctx.Settings.ErrorUrl");

            return ctx.Settings.ErrorUrl;
        }

        public override async Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ctx.Settings.VerboseLogging)
                {
                    _logger.Info($"GenerateForm method called for cart {ctx.Order.OrderNumber}");
                }

                ctx.Settings.InstallId.MustNotBeNull("ctx.Settings.InstallId");

                var firstname = ctx.Order.CustomerInfo.FirstName;
                var surname = ctx.Order.CustomerInfo.LastName;

                if (!string.IsNullOrEmpty(ctx.Settings.BillingFirstNamePropertyAlias))
                {
                    firstname = ctx.Order.Properties[ctx.Settings.BillingFirstNamePropertyAlias];
                }

                if (!string.IsNullOrEmpty(ctx.Settings.BillingLastNamePropertyAlias))
                {
                    surname = ctx.Order.Properties[ctx.Settings.BillingLastNamePropertyAlias];
                }

                var address1 = ctx.Order.Properties[ctx.Settings.BillingAddressLine1PropertyAlias] ?? string.Empty;
                var city = ctx.Order.Properties[ctx.Settings.BillingAddressCityPropertyAlias] ?? string.Empty;
                var postcode = ctx.Order.Properties[ctx.Settings.BillingAddressZipCodePropertyAlias] ?? string.Empty;
                var billingCountry = await Context.Services.CountryService.GetCountryAsync(ctx.Order.PaymentInfo.CountryId.Value);
                var billingCountryCode = billingCountry.Code.ToUpperInvariant();
                var amount = FormatAmount(ctx.Order.TransactionAmount.Value.Value);
                var currency = await Context.Services.CurrencyService.GetCurrencyAsync(ctx.Order.CurrencyId);
                var currencyCode = currency.Code.ToUpperInvariant();

                // Ensure billing country has valid ISO 3166 code
                var iso3166Countries = await Context.Services.CountryService.GetIso3166CountryRegionsAsync();
                if (iso3166Countries.All(x => x.Code != billingCountryCode))
                {
                    throw new Exception("Country must be a valid ISO 3166 billing country code: " + billingCountry.Name);
                }

                // Ensure currency has valid ISO 4217 code
                if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
                {
                    throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
                }

                var orderDetails = new Dictionary<string, string>
                {
                    { "instId", ctx.Settings.InstallId },
                    { "testMode", ctx.Settings.TestMode ? "100" : "0" },
                    { "authMode", ctx.Settings.Capture ? "A" : "E" },
                    { "cartId", ctx.Order.OrderNumber },
                    { "amount", amount },
                    { "currency", currencyCode },
                    { "name", firstname + " " + surname },
                    { "email", ctx.Order.CustomerInfo.Email },
                    { "address1", address1 },
                    { "town", city },
                    { "postcode", postcode },
                    { "country", billingCountryCode },
                    { OrderReferenceField, ctx.Order.GenerateOrderReference() },
                    { "MC_cancelurl", ctx.Urls.CancelUrl },
                    { "MC_returnurl", ctx.Urls.ContinueUrl },
                    { "MC_callbackurl", ctx.Urls.CallbackUrl }
                };

                if (!string.IsNullOrEmpty(ctx.Settings.Md5Secret))
                {
                    var orderSignature = Md5Helper.CreateMd5(ctx.Settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + ctx.Settings.InstallId + ":" + ctx.Order.OrderNumber);

                    orderDetails.Add("signature", orderSignature);

                    if (ctx.Settings.VerboseLogging)
                    {
                        _logger.Info($"Signature fields: {amount}:{currencyCode}:{ctx.Settings.InstallId}:{ctx.Order.OrderNumber}");
                        _logger.Info($"Signature: " + orderSignature);
                    }
                }

                var url = ctx.Settings.TestMode ? TestBaseUrl : LiveBaseUrl;
                var form = new PaymentForm(url, PaymentFormMethod.Post)
                {
                    Inputs = orderDetails
                };

                if (ctx.Settings.VerboseLogging)
                {
                    _logger.Info($"Payment url {url}");
                    _logger.Info($"Form data {orderDetails.ToFriendlyString()}");
                }

                return new PaymentFormResult
                {
                    Form = form
                };
            }
            catch (Exception e)
            {
                _logger.Error($"Exception thrown for cart {ctx.Order.OrderNumber} - with error {e.Message}");

                throw;
            }
        }

        public override async Task<OrderReference> GetOrderReferenceAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Settings);

            WorldpayCallbackData callbackData = await GetCallbackDataAsync(context.AdditionalData, context.HttpContext.Request, cancellationToken).ConfigureAwait(false);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info($"Worldpay data {callbackData.Form.ToMaskedFriendlyString()}");
            }

            CallbackCheckResult passwordCheck = CheckResponsePassword(callbackData.Form, context.Settings.ResponsePassword);
            if (passwordCheck != CallbackCheckResult.Valid)
            {
                _logger.Warn($"Worldpay callback refused: {DescribeCheckResult(passwordCheck)}");

                return null;
            }

            if (OrderReference.TryParse(callbackData.Form[OrderReferenceField], out var orderReference))
            {
                return orderReference;
            }

            return await base.GetOrderReferenceAsync(context, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentNullException.ThrowIfNull(ctx.Settings);

            // The request may already have been read by GetOrderReferenceAsync, in which case
            // the parsed data is reused from the context; otherwise it's read here.
            WorldpayCallbackData callbackData = await GetCallbackDataAsync(ctx.AdditionalData, ctx.HttpContext.Request, cancellationToken).ConfigureAwait(false);
            NameValueCollection formData = callbackData.Form;

            var msgType = formData["msgType"] ?? callbackData.Query["msgType"];
            if (msgType != "authResult")
            {
                return CallbackResult.Ok();
            }

            _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber}");

            if (ctx.Settings.VerboseLogging)
            {
                _logger.Info($"Worldpay data {formData.ToMaskedFriendlyString()}");
            }

            CallbackCheckResult passwordCheck = CheckResponsePassword(formData, ctx.Settings.ResponsePassword);
            if (passwordCheck != CallbackCheckResult.Valid)
            {
                _logger.Warn($"Payment call back for cart {ctx.Order.OrderNumber} refused: {DescribeCheckResult(passwordCheck)}");

                return CallbackResult.Ok();
            }

            if (formData["transStatus"] != "Y")
            {
                _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber} payment not authorised or error");

                return CallbackResult.Ok();
            }

            var currency = await Context.Services.CurrencyService.GetCurrencyAsync(ctx.Order.CurrencyId).ConfigureAwait(false);
            var expectedAmount = FormatAmount(ctx.Order.TransactionAmount.Value.Value);
            var expectedCurrencyCode = currency.Code.ToUpperInvariant();

            CallbackCheckResult paymentCheck = CheckAuthorisedPayment(formData, expectedAmount, expectedCurrencyCode, out var authorisedAmount);
            if (paymentCheck != CallbackCheckResult.Valid)
            {
                _logger.Warn($"Payment call back for cart {ctx.Order.OrderNumber} refused: {DescribeCheckResult(paymentCheck)} "
                    + $"(expected {expectedAmount} {expectedCurrencyCode}, received {formData["authAmount"]} {formData["authCurrency"]})");

                return CallbackResult.Ok();
            }

            var transactionId = formData["transId"];
            var paymentStatus = GetPaymentStatus(formData["authMode"], ctx.Settings.Capture);

            _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber} payment authorised");

            return CallbackResult.Ok(new TransactionInfo
            {
                AmountAuthorized = authorisedAmount,
                TransactionFee = 0m,
                TransactionId = transactionId,
                PaymentStatus = paymentStatus
            });
        }

        /// <summary>
        /// Formats an amount the way it's sent to Worldpay in the payment form.
        /// </summary>
        internal static string FormatAmount(decimal amount)
            => amount.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>
        /// Maps the <c>authMode</c> Worldpay returns in the callback to a payment status.
        /// <c>A</c> is a full authorisation, which Worldpay settles itself, so the payment is captured.
        /// <c>E</c> is a pre-authorisation, which still has to be captured in the Worldpay Merchant Interface.
        /// Any other value falls back to the Capture setting the payment form was sent with.
        /// </summary>
        internal static PaymentStatus GetPaymentStatus(string authMode, bool capture) => authMode switch
        {
            "A" => PaymentStatus.Captured,
            "E" => PaymentStatus.Authorized,
            _ => capture ? PaymentStatus.Captured : PaymentStatus.Authorized,
        };

        /// <summary>
        /// Reads the callback's query string and form, reusing the copy stored in
        /// <paramref name="additionalData"/> if the request has already been read.
        /// </summary>
        internal static async Task<WorldpayCallbackData> GetCallbackDataAsync(IDictionary<string, object> additionalData, HttpRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(additionalData);
            ArgumentNullException.ThrowIfNull(request);

            if (additionalData.TryGetValue(QueryDataKey, out var storedQuery) && storedQuery is NameValueCollection query
                && additionalData.TryGetValue(FormDataKey, out var storedForm) && storedForm is NameValueCollection form)
            {
                return new WorldpayCallbackData(query, form);
            }

            query = HttpUtility.ParseQueryString(request.QueryString.Value ?? string.Empty);
            form = new NameValueCollection();

            if (request.HasFormContentType)
            {
                IFormCollection formCollection = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                foreach (KeyValuePair<string, StringValues> field in formCollection)
                {
                    foreach (var value in field.Value)
                    {
                        form.Add(field.Key, value);
                    }
                }
            }

            additionalData[QueryDataKey] = query;
            additionalData[FormDataKey] = form;

            return new WorldpayCallbackData(query, form);
        }

        /// <summary>
        /// Checks the callback password sent by Worldpay against the configured Response Password.
        /// A Response Password must be configured.
        /// </summary>
        internal static CallbackCheckResult CheckResponsePassword(NameValueCollection formData, string responsePassword)
        {
            ArgumentNullException.ThrowIfNull(formData);

            if (string.IsNullOrEmpty(responsePassword))
            {
                return CallbackCheckResult.ResponsePasswordNotConfigured;
            }

            return FixedTimeEquals(responsePassword, formData["callbackPW"])
                ? CallbackCheckResult.Valid
                : CallbackCheckResult.ResponsePasswordMismatch;
        }

        /// <summary>
        /// Checks the amount and currency Worldpay authorised (<c>authAmount</c> and <c>authCurrency</c>)
        /// against the amount and currency the payment form was generated for.
        /// </summary>
        internal static CallbackCheckResult CheckAuthorisedPayment(NameValueCollection formData, string expectedAmount, string expectedCurrencyCode, out decimal authorisedAmount)
        {
            ArgumentNullException.ThrowIfNull(formData);

            authorisedAmount = 0m;

            if (!decimal.TryParse(formData["authAmount"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var receivedAmount)
                || !decimal.TryParse(expectedAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var expected))
            {
                return CallbackCheckResult.AmountMissingOrInvalid;
            }

            if (!string.Equals(formData["authCurrency"], expectedCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return CallbackCheckResult.CurrencyMismatch;
            }

            if (receivedAmount != expected)
            {
                return CallbackCheckResult.AmountMismatch;
            }

            authorisedAmount = receivedAmount;

            return CallbackCheckResult.Valid;
        }

        internal static bool FixedTimeEquals(string expected, string actual)
        {
            if (expected == null || actual == null)
            {
                return false;
            }

            // Hash both values first so the comparison time doesn't depend on their lengths either.
            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
        }

        private static string DescribeCheckResult(CallbackCheckResult result) => result switch
        {
            CallbackCheckResult.ResponsePasswordNotConfigured => "no Response Password is configured on the payment method; set it to match the Payment Response password in Worldpay",
            CallbackCheckResult.ResponsePasswordMismatch => "the callback password doesn't match the Response Password configured on the payment method",
            CallbackCheckResult.AmountMissingOrInvalid => "the authorised amount is missing or isn't a valid amount",
            CallbackCheckResult.CurrencyMismatch => "the authorised currency doesn't match the order currency",
            CallbackCheckResult.AmountMismatch => "the authorised amount doesn't match the order total",
            _ => result.ToString(),
        };

        internal enum CallbackCheckResult
        {
            Valid,
            ResponsePasswordNotConfigured,
            ResponsePasswordMismatch,
            AmountMissingOrInvalid,
            CurrencyMismatch,
            AmountMismatch,
        }

        internal sealed record WorldpayCallbackData(NameValueCollection Query, NameValueCollection Form);
    }
}
