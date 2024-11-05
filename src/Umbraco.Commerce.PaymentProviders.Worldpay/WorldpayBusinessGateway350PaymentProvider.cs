using System.Collections.Specialized;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;
using Umbraco.Commerce.PaymentProviders.Worldpay.Extensions;
using Umbraco.Commerce.PaymentProviders.Worldpay.Factories;

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

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("{MethodName} method called for order: {OrderNumber}", nameof(GenerateFormAsync), context.Order.OrderNumber);
            }

            try
            {
                var factory = new PaymentFormFactory(Context.Services.CountryService, Context.Services.CurrencyService, _logger);

                var form = factory.Create(context);

                var paymentFormresult = new PaymentFormResult()
                {
                    Form = form
                };

                return Task.FromResult(paymentFormresult);
            }
            catch (Exception e)
            {
                // We log exception here to save context (OrderNumber) for furher debugging
                _logger.Error(e, "Exception thrown for cart {OrderNumber}", context.Order.OrderNumber);

                throw;
            }
        }

        public override async Task<OrderReference?> GetOrderReferenceAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Settings);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("{MethodName} method called", nameof(GetOrderReferenceAsync));
            }

            var formData = await context.GetFormDataAsync(cancellationToken).ConfigureAwait(false);

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Worldpay form data: {FormData}", NameValueCollectionExtensions.ToFriendlyString(formData));
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    _logger.Error("{PasswordParameter} was incorrect during processing {MethodName} method", WorldpayParameters.Response.CallbackPW, nameof(ProcessCallbackAsync));

                    return await base.GetOrderReferenceAsync(context, cancellationToken).ConfigureAwait(false);
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
            ArgumentNullException.ThrowIfNull(context);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("{MethodName} method called for order: {OrderNumber}", nameof(ProcessCallbackAsync), context.Order.OrderNumber);
            }

            var queryData = context.GetQueryData();

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Worldpay query data: {Data}", NameValueCollectionExtensions.ToFriendlyString(queryData));
            }

            const string AuthResult = "authResult";
            if (queryData[WorldpayParameters.Query.MsgType] != AuthResult)
            {
                _logger.Error("Worldpay query data doesn't have {Value}", AuthResult);

                return CallbackResult.Ok();
            }

            var formData = await context.GetFormDataAsync(cancellationToken).ConfigureAwait(false);

            if (context.Settings.VerboseLogging)
            {
                // TODO: after removing obsolete VerboseLoggingHelper use it like extension
                _logger.Info("Worldpay form data: {Data}", NameValueCollectionExtensions.ToFriendlyString(formData));
            }

            if (!string.IsNullOrEmpty(context.Settings.ResponsePassword))
            {
                if (!IsResponsePasswordValid(context, formData))
                {
                    _logger.Error("{PasswordParameter} for {OrderNumber} was incorrect during processing {MethodName} method", WorldpayParameters.Response.CallbackPW, context.Order.OrderNumber, nameof(ProcessCallbackAsync));

                    return CallbackResult.Ok();
                }
            }

            // if still here, password was not required or matched

            var transactionStatus = formData[WorldpayParameters.Response.TransStatus];

            var callbackResult = transactionStatus switch
            {
                WorldpayValues.Response.TransStatus.Cancelled => ProcessCancelledTransactionStatus(context),
                WorldpayValues.Response.TransStatus.Succeed => ProcessSucceedTransactionStatus(context, formData),
                _ => ProcessUnexpectedTransactionStatus(context, transactionStatus)
            };

            return callbackResult;
        }

        private CallbackResult ProcessSucceedTransactionStatus(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, NameValueCollection formData)
        {
            var transactionInfo = TransactionInfoFactory.Create(formData);

            if (context.Settings.VerboseLogging)
            {
                _logger.Info("Transaction for order {OrderNumber} was succeed", context.Order.OrderNumber);
            }

            return CallbackResult.Ok(transactionInfo);
        }

        private CallbackResult ProcessCancelledTransactionStatus(PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            if (context.Settings.VerboseLogging)
            {
                _logger.Info("Transaction for order {OrderNumber} was cancelled", context.Order.OrderNumber);
            }

            return CallbackResult.Ok();
        }

        private CallbackResult ProcessUnexpectedTransactionStatus(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                _logger.Error("Transaction for order {OrderNumber} had null, empty or white space status: \"{Status}\"", context.Order.OrderNumber, status);
            }
            else
            {
                _logger.Error("Transaction for order {OrderNumber} had unexpected status: {Status}", context.Order.OrderNumber, status);
            }

            return CallbackResult.Ok();
        }

        private static bool IsResponsePasswordValid(PaymentProviderContext<WorldpayBusinessGateway350Settings> context, NameValueCollection formData) => context.Settings.ResponsePassword == formData[WorldpayParameters.Response.CallbackPW];
    }
}
