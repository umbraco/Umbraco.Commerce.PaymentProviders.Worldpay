using System.Collections.Specialized;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Extensions
{
    public static class NameValueCollectionExtensions
    {
        public static string ToFriendlyString(this NameValueCollection collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            var items = collection.AllKeys.Select(key => $"{key}={collection[key]}").ToList();

            var friendlyString = $"{{{string.Join(',', items)}}}";

            return friendlyString;
        }
    }
}
