using System.Security.Cryptography;
using System.Text;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Helpers
{
    public static class SignatureHelper
    {
        public static string EnrichPatternWithValues(WorldpayBusinessGateway350Settings settings, IReadOnlyDictionary<string, string> values)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(values);

            if (string.IsNullOrEmpty(settings.Md5Secret))
            {
                throw new InvalidOperationException($"Field {nameof(settings.Md5Secret)} required.");
            }

            if (string.IsNullOrEmpty(settings.SignaturePattern))
            {
                throw new InvalidOperationException($"Field {nameof(settings.SignaturePattern)} required.");
            }

            var keys = settings.SignaturePattern.Split(':');

            // TODO: it can be done more efficiently: use here StringBuilder, Span?

            var input = settings.Md5Secret;
            foreach (var key in keys)
            {
                var value = values[key];
                input = $"{input}:{value}";
            }

            return input;
        }

        public static string CreateSignature(string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var inputBytes = Encoding.UTF8.GetBytes(input);

            var cryptoServiceProvider = new MD5CryptoServiceProvider();

            var signature = cryptoServiceProvider.ComputeHash(inputBytes).ToHex(true);

            return signature;
        }

        private static string ToHex(this byte[] bytes, bool toLower)
        {
            var c = new char[bytes.Length * 2];
            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                var b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return toLower ? new string(c).ToLower(null) : new string(c);
        }
    }
}
