using global::PayPal.PayPalAPIInterfaceService.Model;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Website;
using Mediachase.Commerce.Website.BaseControls;
using System.Net;

namespace EPiServer.Business.Commerce.Payment.PayPal
{
    /// <summary>
    ///	Implements User interface for generic gateway
    /// </summary>
    public partial class PaymentMethod : BaseStoreUserControl, IPaymentOption
    {
        private PayPalConfiguration _payPalConfiguration;

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Page_Load(object sender, System.EventArgs e)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (_payPalConfiguration == null)
            {
                _payPalConfiguration = new PayPalConfiguration();
            }

            var pal = _payPalConfiguration.PAL;
            if (string.IsNullOrEmpty(pal))
            {
                pal = ViewState["PAL"] as string;
            }

            if (string.IsNullOrEmpty(pal))
            {
                //Obtain the PAL code using API
                var caller = PayPalAPIHelper.GetPayPalAPICallerServices(_payPalConfiguration);
                if (caller != null)
                {
                    var palResponse = caller.GetPalDetails(new GetPalDetailsReq() { GetPalDetailsRequest = new GetPalDetailsRequestType() });
                    if (palResponse.Ack == AckCodeType.SUCCESSWITHWARNING || palResponse.Ack == AckCodeType.SUCCESS)
                    {
                        pal = palResponse.Pal;
                        ViewState["PAL"] = pal;
                    }
                }
            }

            SetupImageMark(_payPalConfiguration, pal);

            Message.Text = Utilities.Translate("PayPalText");

            if (string.Equals(Request["accept"], "false") && !string.IsNullOrEmpty(Request.QueryString["hash"]))
            {
                ErrorManager.GenerateError(Utilities.Translate("CancelMessage"));
                return;
            }
        }

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
            var payment = new PayPalPayment();
            var paymentAction = _payPalConfiguration.PaymentAction;
            if (paymentAction == "Authorization")
            {
                payment.TransactionType = TransactionType.Authorization.ToString();
            }
            else
            {
                payment.TransactionType = TransactionType.Sale.ToString();
            }
            return payment;
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

        private void SetupImageMark(PayPalConfiguration payPalConfiguration, string palParameter)
        {
            ImageMark.Attributes.Add("onclick", "javascript:var top= (screen.availHeight - 500)/2; var left=(screen.availWidth - 400)/2;window.open('https://www.paypal.com/se/cgi-bin/webscr?cmd=xpt/Marketing/popup/OLCWhatIsPayPal-outside','PayPalHelp', 'height=500, width=400, top=' +top +',left=' + left)");
            ImageMark.Style.Add(System.Web.UI.HtmlTextWriterStyle.Cursor, "pointer");

            if (payPalConfiguration.SandBox != "1")
            {
                // LIVE PayPal payment
                if (string.IsNullOrEmpty(palParameter))
                {
                    //Default image
                    ImageMark.ImageUrl = "https://fpdbs.paypal.com/dynamicimageweb?cmd=_dynamic-image&buttontype=ecmark";
                }
                else
                {
                    ImageMark.ImageUrl = string.Format("https://fpdbs.paypal.com/dynamicimageweb?cmd=_dynamic-image&buttontype=ecmark&pal={0}&locale={1}", palParameter, SiteContext.Current.LanguageName);
                }
            }
            else
            {
                // SANBOX PayPal payment
                if (string.IsNullOrEmpty(palParameter))
                {
                    //Default image
                    ImageMark.ImageUrl = "https://fpdbs.sandbox.paypal.com/dynamicimageweb?cmd=_dynamic-image&buttontype=ecmark";
                }
                else
                {
                    ImageMark.ImageUrl = string.Format("https://fpdbs.sandbox.paypal.com/dynamicimageweb?cmd=_dynamic-image&buttontype=ecmark&pal={0}&locale={1}", palParameter, SiteContext.Current.LanguageName);
                }
            }
        }
    }
}