<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="EPiServer.Business.Commerce.Payment.DIBS.ConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal1" runat="server" Text="Configure DIBS Account" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
	    <tr>
		    <td class="FormLabelCell"><asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:OrderStrings, Payment_API_User %>" />:</td>
		    <td class="FormFieldCell"><asp:TextBox Runat="server" ID="User" Width="230"></asp:TextBox><br>
			    <asp:RequiredFieldValidator ControlToValidate="User" Display="dynamic" Font-Name="verdana" Font-Size="9pt" ErrorMessage="<%$ Resources:OrderStrings, Payment_User_Required %>"
				    runat="server" id="RequiredFieldValidator2"></asp:RequiredFieldValidator>
		    </td>
	    </tr>
	    <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
		    <td class="FormLabelCell"><asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:OrderStrings, Payment_Merchant_Password%>" />:</td>
		    <td class="FormFieldCell"><asp:TextBox Runat="server" ID="Password" TextMode="Password" Width="230"></asp:TextBox><br>
			    <asp:RequiredFieldValidator ControlToValidate="Password" Display="dynamic" Font-Name="verdana" Font-Size="9pt" ErrorMessage="<%$ Resources:OrderStrings, Payment_Password_Required %>"
				    runat="server" id="RequiredFieldValidator4"></asp:RequiredFieldValidator>
		    </td>
	    </tr>
	    <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal10" runat="server" Text="<%$ Resources:SharedStrings, Processing_URL %>" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="ProcessingUrl" Width="300px"></asp:TextBox><br>
		            <asp:RequiredFieldValidator ControlToValidate="ProcessingUrl" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="<%$ Resources:OrderStrings, Payment_Processing_Url_Required %>" runat="server" id="Requiredfieldvalidator5"></asp:RequiredFieldValidator>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal2" runat="server" Text="MD5 key 1" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="MD5key1" Width="300px"></asp:TextBox><br>
		            <asp:RequiredFieldValidator ControlToValidate="MD5key1" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="MD5 key 1 is required" runat="server" id="Requiredfieldvalidator1"></asp:RequiredFieldValidator>
	          </td>
        </tr>
         <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal3" runat="server" Text="MD5 key 2" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="MD5key2" Width="300px"></asp:TextBox><br>
		            <asp:RequiredFieldValidator ControlToValidate="MD5key2" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="MD5 key 2 is required" runat="server" id="Requiredfieldvalidator3"></asp:RequiredFieldValidator>
	          </td>
        </tr>
    </table>
</div>