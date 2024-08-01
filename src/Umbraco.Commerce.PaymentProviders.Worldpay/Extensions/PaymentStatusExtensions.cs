using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Extensions
{
    public static class PaymentStatusExtensions
    {
        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#optional-parameters"/>
        /// </summary>
        /// <returns>Worldpay authMode value.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ToAuthMode(this PaymentStatus status)
        {
            const string ERROR_MESSAGE = $"Not matched to Worldpay {WorldpayParameters.Request.AuthMode}.";

            return status switch
            {
                PaymentStatus.Initialized => throw new NotImplementedException(ERROR_MESSAGE),
                PaymentStatus.Authorized => WorldpayValues.AuthMode.PreAuthorisation,
                PaymentStatus.Captured => WorldpayValues.AuthMode.FullAuthorisation,
                PaymentStatus.Cancelled => throw new NotImplementedException(ERROR_MESSAGE),
                PaymentStatus.Refunded => throw new NotImplementedException(ERROR_MESSAGE),
                PaymentStatus.PendingExternalSystem => throw new NotImplementedException(ERROR_MESSAGE),
                PaymentStatus.Error => throw new NotImplementedException(ERROR_MESSAGE),
                _ => throw new InvalidOperationException($"Status {status} is not supported by {nameof(PaymentStatus)}")
            };
        }
    }
}
