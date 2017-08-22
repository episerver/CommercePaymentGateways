using Mediachase.BusinessFoundation;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Common;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;

namespace PaymentProviders.Nsoftware.PaymentGateway.ICharge
{
    /// <summary>
    ///		Summary description for ConfigurePayment.
    /// </summary>
    public partial class ConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        private const string _GatewayParameterName = "Gateway";
        private const string _TransactionTypeParameterName = "TransactionType";

        string _ValidationGroup = String.Empty;
        private PaymentMethodDto _PaymentMethodDto = null;

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (_PaymentMethodDto?.PaymentMethodParameter != null)
                {
                    var param = GetParameterByName(_TransactionTypeParameterName);
                    if (!string.IsNullOrEmpty(param?.Value))
                    {
                        ManagementHelper.SelectListItem(TransactionTypeList, param.Value, StringComparer.OrdinalIgnoreCase);
                    }

                    param = GetParameterByName(_GatewayParameterName);
                    if (!string.IsNullOrEmpty(param?.Value))
                    {
                        ManagementHelper.SelectListItem(Gateways, param.Value, StringComparer.OrdinalIgnoreCase);
                    }

                    BindData();
                    SortGatewayList();
                }
            }
        }

        private void SortGatewayList()
        {
            var listCopy = Gateways.Items.Cast<ListItem>().Where(i => !string.IsNullOrEmpty(i.Value)).ToList();
            Gateways.Items.Clear();
            Gateways.Items.Add(new ListItem("Pick a Gateway", string.Empty));
            Gateways.Items.AddRange(listCopy.OrderBy(item => item.Text).ToArray());
        }

        /// <summary>
        /// Binds a data source to the invoked server control and all its child controls.
        /// </summary>
		public override void DataBind()
        {
            if (IsPostBack)
            {
                BindData();
            }

            base.DataBind();
        }

        /// <summary>
        /// Gets the gateways.
        /// </summary>
        /// <returns></returns>
		public GatewayManager GetGateways()
        {
            var path = Server.MapPath("~/Apps/Order/Payments/Plugins/ICharge/ConfigParams.xml");
            using (XmlReader xmlReader = XmlReader.Create(path))
            {
                var mySerializer = new XmlSerializer(typeof(GatewayManager));
                return (GatewayManager)mySerializer.Deserialize(xmlReader);
            }
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
		public void BindData()
        {
            GenericTable.Rows.Clear();
            var gateway = GetGateways().GetGatewayById(Gateways.SelectedValue);

            if (gateway == null)
            {
                GenericTable.Visible = false;
                return;
            }
            else
            {
                GenericTable.Visible = true;
            }

            TransactionTypeList.Items.Clear();
            foreach (var type in gateway.TransactionTypes)
            {
                TransactionTypeList.Items.Add(new ListItem(UtilHelper.GetResFileString(type.FriendlyName), type.Name));
            }

            TransactionTypeList.DataBind();

            // fill in the form fields
            if (_PaymentMethodDto?.PaymentMethodParameter != null)
            {
                var param = GetParameterByName(_TransactionTypeParameterName);
                if (param != null)
                {
                    ManagementHelper.SelectListItem(TransactionTypeList, param.Value, StringComparer.OrdinalIgnoreCase);
                }

                param = GetParameterByName(_GatewayParameterName);
                if (param != null && string.Compare(param.Value, Gateways.SelectedValue, true) == 0)
                {
                    foreach (PaymentMethodDto.PaymentMethodParameterRow pRow in _PaymentMethodDto.PaymentMethodParameter)
                    {
                        // skip parameter with name "Gateway" and process all the other parameters
                        if (string.Compare(pRow.Parameter, _GatewayParameterName, true) != 0 &&
                            string.Compare(pRow.Parameter, _TransactionTypeParameterName, true) != 0)
                        {
                            var prop = gateway.FindPropertyByName(pRow.Parameter);
                            if (prop != null)
                            {
                                CreateRow(prop.Name, UtilHelper.GetResFileString(prop.FriendlyName), pRow.Value, prop.Required);
                            }
                        }
                    }
                }
                else
                {
                    if (gateway.Properties != null)
                    {
                        foreach (var prop in gateway.Properties)
                        {
                            CreateRow(prop.Name, UtilHelper.GetResFileString(prop.FriendlyName), "", prop.Required);
                        }
                    }
                }
            }
            else
            {
                Visible = false;
            }
        }

        /// <summary>
        /// Creates row with parameter name and value in the GenericTable.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="label">The label.</param>
        /// <param name="val">The val.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
		private void CreateRow(string id, string label, string val, bool required)
        {
            var row = new HtmlTableRow();
            var cell1 = new HtmlTableCell();
            var cell2 = new HtmlTableCell();
            cell1.Attributes.Add("class", "FormLabelCell");
            cell2.Attributes.Add("class", "FormFieldCell");

            // Create label cell
            var lbl = new Label();
            lbl.Text = required ? $"*{label}:" : $"{label}:";

            cell1.Controls.Add(lbl);
            cell1.Width = "300px";

            // Create value field
            var box = new TextBox();
            box.Text = val;
            box.Width = Unit.Parse("330");
            box.ID = id;

            cell2.Controls.Add(box);

            if (required)
            {
                var reqValidator = new RequiredFieldValidator()
                {
                    ID = $"reqval{id}",
                    ControlToValidate = id,
                    Display = ValidatorDisplay.Dynamic,
                    ErrorMessage = "*"
                };
                cell2.Controls.Add(reqValidator);
            }

            row.Cells.Add(cell1);
            row.Cells.Add(cell2);
            GenericTable.Rows.Add(row);

            // add empty row with line
            var rowLine = new HtmlTableRow();
            var cellLine = new HtmlTableCell();
            cellLine.Attributes.Add("class", "FormSpacerCell");
            cellLine.Attributes.Add("colspan", "2");
            rowLine.Cells.Add(cellLine);
            GenericTable.Rows.Add(rowLine);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            base.OnInit(e);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
		private void InitializeComponent()
        {
            Gateways.SelectedIndexChanged += new EventHandler(Gateways_SelectedIndexChanged);
            Load += new EventHandler(Page_Load);
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the Gateways control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Gateways_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            BindData();
            GatewayParametersUpdatePanel.Update();
        }

        /// <summary>
        /// Saves the object changes.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void SaveChanges(object dto)
        {
            if (Visible)
            {
                _PaymentMethodDto = dto as PaymentMethodDto;

                if (_PaymentMethodDto != null && _PaymentMethodDto.PaymentMethodParameter != null)
                {
                    var gateway = GetGateways().GetGatewayById(Gateways.SelectedValue);

                    var paymentMethodId = Guid.Empty;
                    if (_PaymentMethodDto.PaymentMethod.Count > 0)
                    {
                        paymentMethodId = _PaymentMethodDto.PaymentMethod[0].PaymentMethodId;
                    }

                    // add new parameters
                    var paramsFilter = new StringBuilder();

                    if (gateway.Properties != null)
                    {
                        foreach (var prop in gateway.Properties)
                        {
                            var ctrl = GenericTable.FindControl(prop.Name);
                            if (ctrl != null)
                            {
                                var val = ((TextBox)ctrl).Text;

                                var row = GetParameterByName(prop.Name);
                                if (row != null)
                                {
                                    row.Value = val;
                                }
                                else
                                {
                                    CreateParameter(_PaymentMethodDto, prop.Name, val, paymentMethodId);
                                }
                                paramsFilter.AppendFormat("Parameter <> '{0}' AND ", prop.Name);
                            }
                        }
                    }

                    // add gateway parameter
                    var gRow = GetParameterByName(_GatewayParameterName);
                    if (gRow != null)
                    {
                        gRow.Value = Gateways.SelectedValue;
                    }
                    else
                    {
                        CreateParameter(_PaymentMethodDto, _GatewayParameterName, Gateways.SelectedValue, paymentMethodId);
                    }
                    paramsFilter.AppendFormat("Parameter <> '{0}'", _GatewayParameterName);

                    // add transaction type parameter
                    var trRow = GetParameterByName(_TransactionTypeParameterName);
                    if (trRow != null)
                    {
                        trRow.Value = TransactionTypeList.SelectedValue;
                    }
                    else
                    {
                        CreateParameter(_PaymentMethodDto, _TransactionTypeParameterName, TransactionTypeList.SelectedValue, paymentMethodId);
                    }
                    paramsFilter.AppendFormat(" AND Parameter <> '{0}'", _TransactionTypeParameterName);

                    // remove parameters that are not used anymore
                    var filter = paramsFilter.ToString();
                    var rows = (PaymentMethodDto.PaymentMethodParameterRow[])_PaymentMethodDto.PaymentMethodParameter.Select(filter);
                    if (rows != null && rows.Length > 0)
                    {
                        foreach (var pRow in rows)
                        {
                            pRow.Delete();
                        }
                    }
                }
            }
        }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            var rows = (PaymentMethodDto.PaymentMethodParameterRow[])_PaymentMethodDto.PaymentMethodParameter.Select($"Parameter = '{name}'");
            if (rows != null && rows.Length > 0)
            {
                return rows[0];
            }
            else
            {
                return null;
            }
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
            {
                dto.PaymentMethodParameter.Rows.Add(newRow);
            }
        }

        /// <summary>
        /// Loads the object.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void LoadObject(object dto)
        {
            _PaymentMethodDto = dto as PaymentMethodDto;
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>The validation group.</value>
        public string ValidationGroup { get; set; }
    }

    #region Configuration classes
    [XmlRoot("GatewaySettings")]
    public class GatewayManager
    {
        [XmlArray("Gateways")]
        public Gateway[] Gateways;

        /// <summary>
        /// Gets the gateway by id.
        /// </summary>
        /// <param name="id">The gateway id.</param>
        /// <returns>The gateway</returns>
		public Gateway GetGatewayById(string id)
        {
            foreach (Gateway gateway in Gateways)
            {
                if (gateway.id == id)
                {
                    return gateway;
                }
            }

            return null;
        }
    }

    [XmlRoot("Gateway")]
    public class Gateway
    {
        [XmlAttribute]
        public string id = "";

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>The properties.</value>
		[XmlArray("Properties"), XmlArrayItem(ElementName = "Property")]
        public GatewayProperty[] Properties { get; set; }

        [XmlArray("TransactionTypes"), XmlArrayItem(ElementName = "TransactionType")]
        public TransactionType[] TransactionTypes;

        /// <summary>
        /// Finds a <see cref="GatewayProperty"/> by name.
        /// </summary>
        /// <param name="name">The gate way property name.</param>
        /// <returns>The gateway property.</returns>
		public GatewayProperty FindPropertyByName(string name)
        {
            if (Properties == null)
            {
                return null;
            }

            foreach (var prop in Properties)
            {
                if (prop.Name.CompareTo(name) == 0)
                {
                    return prop;
                }
            }

            return null;
        }
    }

    public class GatewayProperty
    {
        [XmlAttribute]
        public bool Required = false;
        [XmlAttribute]
        public string FriendlyName = "";
        [XmlAttribute]
        public string Name = "";
    }

    public class TransactionType
    {
        [XmlAttribute]
        public string FriendlyName = "";
        [XmlAttribute]
        public string Name = "";
    }
    #endregion
}
