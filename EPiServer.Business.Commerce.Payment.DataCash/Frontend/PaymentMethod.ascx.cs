using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Website;
using Mediachase.Commerce.Website.BaseControls;
using System.Web.UI;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    /// <summary>
    ///	Implements User interface for generic gateway
    /// </summary>
    public partial class PaymentMethod : UserControl, IPaymentOption
    {
        /// <summary>
        /// Validates input data for the control. In case of Credit card pre authentication will be the best way to
        /// validate. The error message if any should be displayed within a control itself.
        /// </summary>
        /// <returns>Returns false if validation is failed.</returns>
        public bool ValidateData()
        {
            return true;
        }

        /// <summary>
        /// This method is called before the order is completed. This method should check all the parameters
        /// and validate the credit card or other parameters accepted.
        /// </summary>
        /// <param name="form">The order form.</param>
        public Mediachase.Commerce.Orders.Payment PreProcess(OrderForm form)
        {
            return new DataCashPayment {TransactionType = TransactionType.Authorization.ToString()};
        }

        /// <summary>
        /// This method is called after the order is placed. This method should be used by the gateways that want to
        /// redirect customer to their site.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <returns></returns>
        public bool PostProcess(OrderForm form)
        {
            return true;
        }
    }
}