namespace Umbraco.Commerce.PaymentProviders.Worldpay.Constants
{
    /// <summary>
    /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#parameters-explained"/>
    /// </summary>
    public static class WorldPayField
    {
        #region From request

        public const string INSTALLATION_ID = "instId";
        public const string TEST_MODE = "testMode";
        public const string AUTH_MODE = "authMode";
        public const string CART_ID = "cartId";
        public const string AMOUNT = "amount";
        public const string CURRENCY = "currency";
        public const string NAME = "name";
        public const string EMAIL = "email";
        public const string ADDRESS_1 = "address1";
        public const string TOWN = "town";
        public const string POSTCODE = "postcode";
        public const string COUNTRY = "country";
        public const string SIGNATURE = "signature";
        public const string CALLBACK_PASSWORD = "callbackPW";

        #endregion

        #region From response

        public const string MESSAGE_TYPE = "msgType";
        public const string TRANSACTION_STATUS = "transStatus";
        public const string TRANSACTION_ID = "transId";
        public const string AUTHORIZED_AMOUNT = "authAmount";

        #endregion

        /// <summary>
        /// Read more: <see href="https://developerengine.fisglobal.com/apis/bg350/send-order-details#custom-parameters"/>
        /// </summary>
        public static class Custom
        {
            public const string SHOPPER_RESULT_PREFIX = "C";
            public const string PAYMENT_RESPONSE_MESSAGE_PREFIX = "M";

            public const string ORDER_REFERENCE = $"{PAYMENT_RESPONSE_MESSAGE_PREFIX}{SHOPPER_RESULT_PREFIX}_ctx.OrderRef";
            public const string CANCEL_URL = $"{PAYMENT_RESPONSE_MESSAGE_PREFIX}{SHOPPER_RESULT_PREFIX}_cancelurl";
            public const string RETURN_URL = $"{PAYMENT_RESPONSE_MESSAGE_PREFIX}{SHOPPER_RESULT_PREFIX}_returnurl";
            public const string CALLBACK_URL = $"{PAYMENT_RESPONSE_MESSAGE_PREFIX}{SHOPPER_RESULT_PREFIX}_callbackurl";
        }
    }
}
