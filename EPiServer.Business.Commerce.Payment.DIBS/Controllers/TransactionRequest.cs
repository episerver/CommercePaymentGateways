using System.Collections.Generic;
using System.Collections.Specialized;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    internal class TransactionRequest
    {
        private readonly DIBSConfiguration _dibsConfiguration;

        private readonly Dictionary<string, object> _keyPairs;
        private readonly string _hmacKey;
        private const string HmacParamName = "MAC";
        private const string OrderIdParamName = "orderId";
        private const string TransactionParamName = "transaction";
        private const string CurrencyParamName = "currency";
        private const string AmountParamName = "amount";
        private const string StatusParamName = "status";

        public string TransactionId { get; set; }

        public string OrderId { get; set; }

        public TransactionRequest(NameValueCollection requestForm, DIBSConfiguration dibsConfiguration)
        {
            _dibsConfiguration = dibsConfiguration;
            _keyPairs = GetRequestKeyPairs(requestForm);
            _hmacKey = requestForm[HmacParamName];

            OrderId = GetKeyPairValue(OrderIdParamName);
            TransactionId = GetKeyPairValue(TransactionParamName);
        }

        public bool IsProcessable()
        {
            var currency = GetKeyPairValue(CurrencyParamName);
            var amount = GetKeyPairValue(AmountParamName);

            return !string.IsNullOrEmpty(OrderId) &&
                !string.IsNullOrEmpty(currency) &&
                !string.IsNullOrEmpty(amount);
        }

        public bool IsSuccessful()
        {
            if (!_hmacKey.Equals(Utilities.GetMACRequest(_dibsConfiguration, _keyPairs)))
            {
                return false;
            }

            // refer: https://tech.dibspayment.com/batch/d2integratedpwhostedoutputparametersreturnparameters
            return GetKeyPairValue(StatusParamName).Equals("ACCEPTED");
        }

        private Dictionary<string, object> GetRequestKeyPairs(NameValueCollection requestForm)
        {
            var keyPairs = new Dictionary<string, object>();

            foreach (string key in requestForm.Keys)
            {
                if (key.Equals(HmacParamName))
                {
                    continue;
                }

                keyPairs.Add(key, requestForm[key]);
            }

            return keyPairs;
        }

        private string GetKeyPairValue(string keyName)
        {
            if (!_keyPairs.ContainsKey(keyName))
            {
                return null;
            }

            return _keyPairs[keyName].ToString();
        }
    }
}
