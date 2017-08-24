using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public partial class ConfigurePayment : UserControl, IGatewayControl
    {
        private PaymentMethodDto _paymentMethodDto;

        public string ValidationGroup { get; set; }

        public ConfigurePayment()
        {
            ValidationGroup = string.Empty;
            _paymentMethodDto = null;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            BindData();
        }
        
        public void LoadObject(object dto)
        {
            _paymentMethodDto = dto as PaymentMethodDto;
        }

        public void SaveChanges(object dto)
        {
            if (!Visible)
            {
                return;
            }

            _paymentMethodDto = dto as PaymentMethodDto;
            if (_paymentMethodDto == null || _paymentMethodDto.PaymentMethodParameter == null)
            {
                return;
            }

            var paymentMethodId = _paymentMethodDto.PaymentMethod.Count > 0 ? _paymentMethodDto.PaymentMethod[0].PaymentMethodId : Guid.Empty;
                 
            UpdateParameter(DataCashConfiguration.HostAddressParameter, HostAddress.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.UserIdParameter, APIUser.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.PasswordParameter, Password.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.PathToLogFileParameter, LogFilePath.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.TimeOutParameter, TimeOut.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.LoggingLevelParameter, LoggingLevel.SelectedValue, paymentMethodId);
            UpdateParameter(DataCashConfiguration.ProxyParameter, Proxy.Text, paymentMethodId);
            UpdateParameter(DataCashConfiguration.PaymentPageIdParameter, string.IsNullOrEmpty(PaymentPageId.Text) ? "1" : PaymentPageId.Text, paymentMethodId);
        }

        private void UpdateParameter(string parameterName, string value, Guid paymentMethodId)
        {
            var parameterRow = DataCashConfiguration.GetParameterByName(_paymentMethodDto, parameterName);
            if (parameterRow != null)
            {
                parameterRow.Value = value;
            }
            else
            {
                var row = _paymentMethodDto.PaymentMethodParameter.NewPaymentMethodParameterRow();
                row.PaymentMethodId = paymentMethodId;
                row.Parameter = parameterName;
                row.Value = value;
                _paymentMethodDto.PaymentMethodParameter.Rows.Add(row);
            }
        }

        private void BindData()
        {
            if (_paymentMethodDto?.PaymentMethodParameter == null)
            {
                Visible = false;
                return;
            }
            
            BindParameterData(DataCashConfiguration.HostAddressParameter, HostAddress);
            BindParameterData(DataCashConfiguration.UserIdParameter, APIUser);
            BindParameterData(DataCashConfiguration.PasswordParameter, Password);
            BindParameterData(DataCashConfiguration.PathToLogFileParameter, LogFilePath);
            BindParameterData(DataCashConfiguration.TimeOutParameter, TimeOut);
            BindParameterData(DataCashConfiguration.ProxyParameter, Proxy);
            BindParameterData(DataCashConfiguration.PaymentPageIdParameter, PaymentPageId);
            
            var selectedValue = DataCashConfiguration.GetParameterValueByName(_paymentMethodDto, DataCashConfiguration.LoggingLevelParameter);
            LoggingLevel.SelectedValue = string.IsNullOrEmpty(selectedValue) ? "5" : selectedValue;
        }

        private void BindParameterData(string parameterName, TextBox parameterControl)
        {
            var parameterByName = DataCashConfiguration.GetParameterValueByName(_paymentMethodDto, parameterName);
            if (parameterByName != null)
            {
                parameterControl.Text = parameterByName;
            }
        }
    }
}