namespace Umbraco.Commerce.PaymentProviders.Worldpay.Extensions
{
    public static class IDictionaryExtensions
    {
        public static string ToFriendlyString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            var items = dictionary.Select(x => $"{x.Key}={x.Value}").ToList();

            var friendlyString = $"{{{string.Join(',', items)}}}";

            return friendlyString;
        }
    }
}
