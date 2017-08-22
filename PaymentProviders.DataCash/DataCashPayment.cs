using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;
using System;
using System.Runtime.Serialization;

namespace PaymentProviders.DataCash
{
    /// <summary>
    /// Represents Payment class for DataCash.
    /// </summary>
    [Serializable]
    public class DataCashPayment : Payment
    {
        private static MetaClass _metaClass;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCashPayment"/> class.
        /// </summary>
        public DataCashPayment()
            : base(DataCashPaymentMetaClass)
        {
            PaymentType = PaymentType.Other;
            ImplementationClass = GetType().AssemblyQualifiedName; // need to have assembly name in order to retrieve the correct type in ClassInfo
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCashPayment"/> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected DataCashPayment(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        {
            PaymentType = PaymentType.Other;
            ImplementationClass = GetType().AssemblyQualifiedName; // need to have assembly name in order to retrieve the correct type in ClassInfo
        }

        /// <summary>
        /// Gets the credit card payment meta class.
        /// </summary>
        /// <value>The credit card payment meta class.</value>
        public static MetaClass DataCashPaymentMetaClass => _metaClass ?? (_metaClass = MetaClass.Load(OrderContext.MetaDataContext, "DataCashPayment"));

        /// <summary>
        /// Represents the DataCash reference string
        /// </summary>
        public string DataCashReference
        {
            get { return GetString(DataCashPaymentGateway.DataCashReferencePropertyName); }
            set { this[DataCashPaymentGateway.DataCashReferencePropertyName] = value; }
        }

        /// <summary>
        /// Represents the DataCash authenticate code
        /// </summary>
        public string DataCashAuthenticateCode
        {
            get { return GetString(DataCashPaymentGateway.DataCashAuthenticateCodePropertyName); }
            set { this[DataCashPaymentGateway.DataCashAuthenticateCodePropertyName] = value; }
        }

        /// <summary>
        /// Represents the merchant reference, which is a unique reference number for each transaction (Min 6, max 30 alphanumeric character)
        /// </summary>
        public string DataCashMerchantReference
        {
            get { return GetString(DataCashPaymentGateway.DataCashMerchantReferencePropertyName); }
            set { this[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] = value; }
        }
    }
}
