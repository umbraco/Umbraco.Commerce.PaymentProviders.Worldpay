export default {
    ucPaymentProviders: {
        'worldpayBs350Label': 'Worldpay Business Gateway 350',
        'worldpayBs350Description': 'Worldpay Business Gateway 350 payment provider',
        'worldpayBs350SettingsContinueUrlLabel': 'Continue URL',
        'worldpayBs350SettingsContinueUrlDescription': 'The URL to continue to after this provider has done processing. eg: /continue/',
        'worldpayBs350SettingsCancelUrlLabel': 'Cancel URL',
        'worldpayBs350SettingsCancelUrlDescription': 'The URL to return to if the payment attempt is canceled. eg: /cart/',
        'worldpayBs350SettingsErrorUrlLabel': 'Error URL',
        'worldpayBs350SettingsErrorUrlDescription': 'The URL to return to if the payment attempt errors. eg: /error/',

        'worldpayBs350SettingsBillingFirstNamePropertyAliasLabel': 'Billing Property First Name',
        'worldpayBs350SettingsBillingFirstNamePropertyAliasDescription': 'The order property alias containing the first name of the customer',

        'worldpayBs350SettingsBillingLastNamePropertyAliasLabel': 'Billing Property Last Name',
        'worldpayBs350SettingsBillingLastNamePropertyAliasDescription': 'The order property alias containing the last name of the customer',

        'worldpayBs350SettingsBillingAddressLine1PropertyAliasLabel': 'Billing Address (Line 1) Property Alias',
        'worldpayBs350SettingsBillingAddressLine1PropertyAliasDescription': '[Required] The order property alias containing line 1 of the billing address',

        'worldpayBs350SettingsBillingAddressCityPropertyAliasLabel': 'Billing Address City Property Alias',
        'worldpayBs350SettingsBillingAddressCityPropertyAliasDescription': '[Required] The order property alias containing the city of the billing address',

        'worldpayBs350SettingsBillingAddressZipCodePropertyAliasLabel': 'Billing Address ZipCode Property Alias',
        'worldpayBs350SettingsBillingAddressZipCodePropertyAliasDescription': '[Required] The order property alias containing the zip code of the billing address',

        'worldpayBs350SettingsInstallIdLabel': 'Install ID',
        'worldpayBs350SettingsInstallIdDescription': 'The installation ID',

        'worldpayBs350SettingsMd5SecretLabel': 'MD5 Secret',
        'worldpayBs350SettingsMd5SecretDescription': 'The Worldpay MD5 secret to use when create MD5 hashes',

        'worldpayBs350SettingsResponsePasswordLabel': 'Response Password',
        'worldpayBs350SettingsResponsePasswordDescription': 'The Worldpay payment response password to use to valida payment responses',

        'worldpayBs350SettingsCaptureLabel': 'Capture',
        'worldpayBs350SettingsCaptureDescription': 'Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture',

        'worldpayBs350SettingsTestModeLabel': 'Test Mode',
        'worldpayBs350SettingsTestModeDescription': 'Set whether to process payments in test mode',

        'worldpayBs350SettingsVerboseLoggingLabel': 'Verbose Logging',
        'worldpayBs350SettingsVerboseLoggingDescription': 'Enable verbose logging',
    },
};