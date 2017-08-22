using Mediachase.Commerce;
using System.Collections.Specialized;

namespace PaymentProviders.DIBS
{
    class TransactionRequest
    {
        private DIBSConfiguration _dibsConfiguration;
        private string _authKey;
        private string _md5Key;
        private string _merchant;

        public string Transact { get; }
        public string OrderId { get; }
        public string Currency { get; }
        public string Amount { get; }

        public TransactionRequest(NameValueCollection requestForm, DIBSConfiguration dibsConfiguration)
        {
            _dibsConfiguration = dibsConfiguration;

            Transact = requestForm["transact"];
            OrderId = requestForm["orderid"];
            Currency = requestForm["currency"];
            Amount = requestForm["amount"];

            _authKey = requestForm["authkey"];
            _md5Key = requestForm["md5key"];
            _merchant = requestForm["merchant"];
        }

        public bool IsProcessable()
        {
            return !string.IsNullOrEmpty(OrderId) && !string.IsNullOrEmpty(Currency) && !string.IsNullOrEmpty(Amount);
        }

        public bool IsSuccessful()
        {
            if (string.IsNullOrEmpty(_authKey) || string.IsNullOrEmpty(Transact))
            {
                return false;
            }

            var hashKey = Utilities.GetMD5ResponseKey(_dibsConfiguration, Transact, Amount, new Currency(Currency));
            return hashKey.Equals(_authKey);
        }

        public bool IsUnsuccessful()
        {
            if (string.IsNullOrEmpty(_merchant) || string.IsNullOrEmpty(_md5Key))
            {
                return false;
            }

            var hashKey = Utilities.GetMD5RequestKey(_dibsConfiguration, _merchant, OrderId, new Currency(Currency), Amount);
            return hashKey.Equals(_md5Key);
        }
    }
}
