using System.Collections.Specialized;
using System.Web;
using Umbraco.Commerce.Core.PaymentProviders;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Extensions
{
    public static class PaymentProviderContextExtensions
    {
        public static NameValueCollection GetQueryData(this PaymentProviderContext<WorldpayBusinessGateway350Settings> context)
        {
            ArgumentNullException.ThrowIfNull(context);

            const string KEY = "queryData";

            if (context.AdditionalData.TryGetValue(KEY, out var data))
            {
                return (NameValueCollection)data;
            }

            var requestUri = context.Request.RequestUri ?? throw new InvalidOperationException($"{nameof(context.Request.RequestUri)} was null");

            var queryData = HttpUtility.ParseQueryString(requestUri.Query);

            context.AdditionalData.Add(KEY, queryData);

            return queryData;
        }

        public static async Task<NameValueCollection> GetFormDataAsync(this PaymentProviderContext<WorldpayBusinessGateway350Settings> context, CancellationToken cancellationToken)
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
