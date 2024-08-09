using System.Collections.Specialized;
using System.Globalization;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.PaymentProviders.Worldpay.Constants;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Factories
{
    internal class TransactionInfoFactory
    {
        public static TransactionInfo Create(NameValueCollection formData)
        {
            ArgumentNullException.ThrowIfNull(formData);

            var rawTotalAmount = formData[WorldpayParameters.Response.AuthAmount]
                ?? throw new InvalidOperationException($"Value of {nameof(formData)}[{WorldpayParameters.Response.AuthAmount}] was null");
            var totalAmount = decimal.Parse(rawTotalAmount, CultureInfo.InvariantCulture);

            var transactionId = formData[WorldpayParameters.Response.TransId];

            var rawPaymentStatus = formData[WorldpayParameters.Request.AuthMode];

            var paymentStatus = rawPaymentStatus switch
            {
                WorldpayValues.Response.AuthMode.FullAuthorisation => PaymentStatus.Captured,
                WorldpayValues.Response.AuthMode.PreAuthorisation => PaymentStatus.Authorized,
                _ => throw new NotSupportedException($"Payment status {rawPaymentStatus} is not supported")
            };

            var transactionInfo = new TransactionInfo
            {
                AmountAuthorized = totalAmount,
                TransactionFee = 0m,
                TransactionId = transactionId,
                PaymentStatus = paymentStatus,
            };

            return transactionInfo;
        }
    }
}
