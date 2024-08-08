using static System.Net.WebRequestMethods;

namespace Umbraco.Commerce.PaymentProviders.Worldpay.Constants
{

    public static class WorldpayParameters
    {
        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#parameters-explained"/>
        /// </summary>
        public static class Request
        {
            public const string InstId = "instId";
            public const string TestMode = "testMode";
            public const string AuthMode = "authMode";
            public const string CartId = "cartId";
            public const string Amount = "amount";
            public const string Currency = "currency";
            public const string Name = "name";
            public const string Email = "email";
            public const string Address1 = "address1";
            public const string Town = "town";
            public const string Postcode = "postcode";
            public const string Country = "country";
            public const string Signature = "signature";

            /// <summary>
            /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#custom-parameters"/>
            /// </summary>
            public static class Custom
            {
                public const string ShopperResultPrefix = "C";
                public const string PaymentResponseMessagePrefix = "M";

                public const string OrderReference = $"{PaymentResponseMessagePrefix}{ShopperResultPrefix}_ctx.OrderRef";
                public const string CancellUrl = $"{PaymentResponseMessagePrefix}{ShopperResultPrefix}_cancelurl";
                public const string ReturnUrl = $"{PaymentResponseMessagePrefix}{ShopperResultPrefix}_returnurl";
                public const string CallbackUrl = $"{PaymentResponseMessagePrefix}{ShopperResultPrefix}_callbackurl";
            }
        }

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/paymentresponseparameters"/>
        /// </summary>
        public static class Response
        {
            public const string CallbackPW = "callbackPW";
            public const string TransStatus = "transStatus";
            public const string TransId = "transId";
            public const string AuthAmount = "authAmount";
        }

        public static class Query
        {
            public const string MsgType = "msgType";
        }

    }
}
