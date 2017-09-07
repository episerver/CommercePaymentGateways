<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="EPiServer.Business.Commerce.Payment.DataCash.ConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal1" runat="server" Text="Configure DataCash Account" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
         <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal4" runat="server" Text="Host address" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="HostAddress" Width="300px" ToolTip="The URL of the DataCash server to send transactions to. If this is not present, any attempt to send a transaction will throw a BadConfigException"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="HostAddress" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Host address required" runat="server" id="Requiredfieldvalidator2"></asp:RequiredFieldValidator>
                            
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal5" runat="server" Text="vTID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="APIUser" Width="300px" ToolTip="This is your account number, for a test account this is likely to be 8 digits, starting '99'"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="APIUser" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="vTID required" runat="server" id="Requiredfieldvalidator4"></asp:RequiredFieldValidator>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal3" runat="server" Text="Password" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="Password" Width="300px" TextMode="Password" ToolTip="This is the password for your vTID"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="Password" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Password Required" runat="server" id="Requiredfieldvalidator3"></asp:RequiredFieldValidator>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal8" runat="server" Text="Log file" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="LogFilePath" Width="300px" ToolTip="A filename (with full path) where the API should write logging information. If this is not present, no logging will take place"></asp:TextBox>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal9" runat="server" Text="Timeout" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="TimeOut" Width="300px" ToolTip="The number of seconds after which a transaction should time out. If this is not present, a default of sixty seconds will be used."></asp:TextBox>
                    <asp:RangeValidator ControlToValidate="TimeOut" Display="dynamic" Font-Name="verdana" Font-Size="9pt" MinimumValue="0" MaximumValue="2147483647" Type="Integer"
			                ErrorMessage="Value must be a number of second." runat="server" id="Requiredfieldvalidator5" ></asp:RangeValidator>
	          </td>
        </tr>
         <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal11" runat="server" Text="Logging level" />:</td>
	          <td class="FormFieldCell">
                  <asp:DropDownList ID="LoggingLevel" runat="server" ToolTip="The level of logging which should take place">
                   <asp:ListItem Text ="Only critical events" Value ="1"></asp:ListItem>
                   <asp:ListItem Text ="Level 2" Value ="2"></asp:ListItem>
                   <asp:ListItem Text ="Level 3" Value ="3"></asp:ListItem>
                   <asp:ListItem Text ="Level 4" Value ="4"></asp:ListItem>
                   <asp:ListItem Text ="Debugging/testing" Value ="5"></asp:ListItem>
                  </asp:DropDownList>
	          </td>
        </tr>
         <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal10" runat="server" Text="Payment page Id" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="PaymentPageId" Width="300px" ToolTip="Multiple payment pages can be configured on your account. Set the id of the payment page(which has been configured at DataCash website) you wish to display here."></asp:TextBox>
                    <asp:RangeValidator ControlToValidate="PaymentPageId" Display="dynamic" Font-Name="verdana" Font-Size="9pt" MinimumValue="0" MaximumValue="2147483647" Type="Integer"
			                ErrorMessage="Value must be a decimal." runat="server" id="Requiredfieldvalidator6"></asp:RangeValidator>
	          </td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal7" runat="server" Text="Proxy" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="Proxy" Width="300px" ToolTip="The URL of a proxy (if any) to connect through. If this is not present, the API will use the computer's default proxy, which can be set via 'Internet Options' in Internet Explorer."></asp:TextBox>
	          </td>
        </tr>
    </table>
</div>