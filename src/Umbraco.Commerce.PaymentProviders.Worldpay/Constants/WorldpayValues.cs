namespace Umbraco.Commerce.PaymentProviders.Worldpay.Constants
{
    public static class WorldpayValues
    {
        public static class Request
        {
            /// <summary>
            /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#optional-parameters"/>
            /// </summary>
            public static class TestMode
            {
                public const string Enabled = "100";
                public const string Disabled = "0";
            }

            /// <summary>
            /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#optional-parameters"/>
            /// </summary>
            public static class AuthMode
            {
                public const string FullAuthorisation = "A";
                public const string PreAuthorisation = "E";
                public const string PostAuthorisation = "O";
            }
        }

        public static class Response
        {
            /// <summary>
            /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/paymentresponseparameters#param-purchase-token"/>
            /// </summary>
            public static class AuthMode
            {
                public const string FullAuthorisation = "A";
                public const string PreAuthorisation = "E";
            }

            /// <summary>
            /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/paymentresponseparameters#response-parameters"/>
            /// </summary>
            public static class TransStatus
            {
                public const string Succeed = "Y";
                public const string Cancelled = "C";
            }
        }
    }
}
