using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Web.UI.WebControls;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public partial class ConfigurePayment : System.Web.UI.UserControl, IGatewayControl
    {
        private PaymentMethodDto _paymentMethodDto;

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>The validation group.</value>
        public string ValidationGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurePayment"/> class.
        /// </summary>
        public ConfigurePayment()
        {
            ValidationGroup = string.Empty;
            _paymentMethodDto = null;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            BindData();
        }

        /// <summary>
        /// Loads the object.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void LoadObject(object dto)
        {
            _paymentMethodDto = dto as PaymentMethodDto;
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void SaveChanges(object dto)
        {
            if (!Visible)
            {
                return;
            }

            _paymentMethodDto = dto as PaymentMethodDto;
            if (_paymentMethodDto?.PaymentMethodParameter == null)
            {
                return;
            }

            var paymentMethodId = _paymentMethodDto.PaymentMethod.Count > 0 ? _paymentMethodDto.PaymentMethod[0].PaymentMethodId : Guid.Empty;

            UpdateOrCreateParameter(DIBSConfiguration.UserParameter, User, paymentMethodId);
            UpdateOrCreateParameter(DIBSConfiguration.PasswordParameter, Password, paymentMethodId);
            UpdateOrCreateParameter(DIBSConfiguration.ProcessingUrlParamter, ProcessingUrl, paymentMethodId);
            UpdateOrCreateParameter(DIBSConfiguration.HMACKeyParameter, HMACkey, paymentMethodId);
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            if (_paymentMethodDto?.PaymentMethodParameter == null)
            {
                Visible = false;
                return;
            }

            BindParameterData(DIBSConfiguration.UserParameter, User);
            BindParameterData(DIBSConfiguration.PasswordParameter, Password);
            BindParameterData(DIBSConfiguration.ProcessingUrlParamter, ProcessingUrl);
            BindParameterData(DIBSConfiguration.HMACKeyParameter, HMACkey);
        }

        private void UpdateOrCreateParameter(string parameterName, TextBox parameterControl, Guid paymentMethodId)
        {
            var parameter = GetParameterByName(parameterName);
            if (parameter != null)
            {
                parameter.Value = parameterControl.Text;
            }
            else
            {
                var row = _paymentMethodDto.PaymentMethodParameter.NewPaymentMethodParameterRow();
                row.PaymentMethodId = paymentMethodId;
                row.Parameter = parameterName;
                row.Value = parameterControl.Text;
                _paymentMethodDto.PaymentMethodParameter.Rows.Add(row);
            }
        }

        private void BindParameterData(string parameterName, TextBox parameterControl)
        {
            var parameterByName = GetParameterByName(parameterName);
            if (parameterByName != null)
            {
                parameterControl.Text = parameterByName.Value;
            }
        }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            return DIBSConfiguration.GetParameterByName(_paymentMethodDto, name);
        }
    }
}