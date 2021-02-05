using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Web.Console.Common;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Data;
using System.Web.UI.WebControls;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    /// <summary>
    ///		Summary description for ConfigurePayment.
    /// </summary>
    public partial class ConfigurePayment : System.Web.UI.UserControl, IGatewayControl
	{
		string _validationGroup = String.Empty;

		private PaymentMethodDto _paymentMethodDto = null;

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		protected void Page_Load(object sender, System.EventArgs e)
		{
			BindData();
		}
        
        /// <summary>
        /// Binds a data source to the invoked server control and all its child controls.
        /// </summary>
		public override void DataBind()
		{
			//BindData();

			base.DataBind();
		}

        /// <summary>
        /// Binds the data.
        /// </summary>
        public void BindData()
        {
			BindCancelStatusesDropDown();
			BindDefaultTransactionTypesDropDown();

			// fill in the form fields
			if (_paymentMethodDto != null && _paymentMethodDto.PaymentMethodParameter != null)
			{
				PaymentMethodDto.PaymentMethodParameterRow param = null;

                // TestMode parameter
                param = GetParameterByName(AuthorizeTokenExGateway.TestParameterName);
                if (param != null)
                {
                    bool testFlag;
                    bool.TryParse(param.Value, out testFlag);
                    TestFlagYes.Checked = testFlag;
                    TestFlagNo.Checked = !testFlag;
                }

				param = GetParameterByName(AuthorizeTokenExGateway.UserParameterName);
				if (param != null)
					User.Text = param.Value;

				param = GetParameterByName(AuthorizeTokenExGateway.TransactionKeyParameterName);
				if (param != null)
					Password.Text = param.Value;

				param = GetParameterByName(AuthorizeTokenExGateway.PaymentOptionParameterName);
				if (param != null)
				{
					ListItem li = RadioButtonListOptions.Items.FindByValue(param.Value);
					if (li != null)
						li.Selected = true;
				}

				param = GetParameterByName(AuthorizeTokenExGateway.RecurringMethodParameterName);
				if (param != null)
                    ManagementHelper.SelectListItem(ddlRecurringMethod, param.Value, StringComparer.Ordinal);

				param = GetParameterByName(AuthorizeTokenExGateway.CancelStatusParameterName);
				if (param != null)
                    ManagementHelper.SelectListItem(ddlCancelStatus, param.Value, StringComparer.Ordinal);
			}
			else
				this.Visible = false;
        }

		private void BindDefaultTransactionTypesDropDown()
		{
			ddlCancelStatus.DataSource = OrderStatusManager.GetDefinedOrderStatuses();
			ddlCancelStatus.DataBind();

			if (ddlCancelStatus.Items.Count == 0)
			{
				ddlCancelStatus.Items.Clear();
				ddlCancelStatus.Items.Add(new ListItem(Mediachase.BusinessFoundation.UtilHelper.GetResFileString("{OrderStrings:RecurringPayment_Select_CancelStatus}"), ""));
			}
		}

		private void BindCancelStatusesDropDown()
		{
			RadioButtonListOptions.Items.Clear();
			RadioButtonListOptions.Items.AddRange(new ListItem[] {
				new ListItem(TransactionType.Authorization.ToString(), TransactionType.Authorization.ToString()),
				new ListItem(TransactionType.Sale.ToString(), TransactionType.Sale.ToString())
				});
		}

		#region IGatewayControl Members
        /// <summary>
        /// Saves the object changes.
        /// </summary>
        /// <param name="dto">The dto.</param>
		public void SaveChanges(object dto)
		{
			if (this.Visible)
			{
				_paymentMethodDto = dto as PaymentMethodDto;

				if (_paymentMethodDto != null && _paymentMethodDto.PaymentMethodParameter != null)
				{
					Guid paymentMethodId = Guid.Empty;
					if (_paymentMethodDto.PaymentMethod.Count > 0)
						paymentMethodId = _paymentMethodDto.PaymentMethod[0].PaymentMethodId;

					PaymentMethodDto.PaymentMethodParameterRow param = null;

                    // TestFlag parameter
                    param = GetParameterByName(AuthorizeTokenExGateway.TestParameterName);
                    if (param != null)
                    {
                        param.Value = TestFlagYes.Checked.ToString();
                    }
                    else
                    {
                        CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.TestParameterName, TestFlagYes.Checked.ToString(), paymentMethodId);
                    }

					param = GetParameterByName(AuthorizeTokenExGateway.UserParameterName);
					if (param != null)
						param.Value = User.Text;
					else
						CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.UserParameterName, User.Text, paymentMethodId);

					param = GetParameterByName(AuthorizeTokenExGateway.TransactionKeyParameterName);
					if (param != null)
						param.Value = Password.Text;
					else
						CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.TransactionKeyParameterName, Password.Text, paymentMethodId);

					#region Regular Transaction Parameters

					param = GetParameterByName(AuthorizeTokenExGateway.PaymentOptionParameterName);
					if (param != null)
						param.Value = RadioButtonListOptions.SelectedValue;
					else
						CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.PaymentOptionParameterName, RadioButtonListOptions.SelectedValue, paymentMethodId);
					#endregion

					#region Recurring Transaction Parameters

					param = GetParameterByName(AuthorizeTokenExGateway.RecurringMethodParameterName);
					if (param != null)
						param.Value = ddlRecurringMethod.SelectedValue;
					else
						CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.RecurringMethodParameterName, ddlRecurringMethod.SelectedValue, paymentMethodId);

					param = GetParameterByName(AuthorizeTokenExGateway.CancelStatusParameterName);
					if (param != null)
						param.Value = ddlCancelStatus.SelectedValue;
					else
						CreateParameter(_paymentMethodDto, AuthorizeTokenExGateway.CancelStatusParameterName, ddlCancelStatus.SelectedValue, paymentMethodId);
					#endregion
				}
			}
		}

        /// <summary>
        /// Gets the parameter by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
		private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
		{
			PaymentMethodDto.PaymentMethodParameterRow[] rows = (PaymentMethodDto.PaymentMethodParameterRow[])_paymentMethodDto.PaymentMethodParameter.Select(String.Format("Parameter = '{0}'", name));
			if (rows != null && rows.Length > 0)
				return rows[0];
			else
				return null;
		}

        /// <summary>
        /// Creates the parameter.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="paymentMethodId">The payment method id.</param>
		private void CreateParameter(PaymentMethodDto dto, string name, string value, Guid paymentMethodId)
		{
			PaymentMethodDto.PaymentMethodParameterRow newRow = dto.PaymentMethodParameter.NewPaymentMethodParameterRow();
			newRow.PaymentMethodId = paymentMethodId;
			newRow.Parameter = name;
			newRow.Value = value;

			// add the row to the dto
			if (newRow.RowState == DataRowState.Detached)
				dto.PaymentMethodParameter.Rows.Add(newRow);
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
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>The validation group.</value>
		public string ValidationGroup
		{
			get
			{
				return _validationGroup;
			}
			set
			{
				_validationGroup = value;
			}
		}
		#endregion
	}
}
