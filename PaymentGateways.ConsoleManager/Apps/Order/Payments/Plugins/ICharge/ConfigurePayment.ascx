<%@ Control Language="c#" Inherits="PaymentGateways.ConsoleManager.Apps.Order.Payments.Plugins.ICharge.ConfigurePayment" CodeBehind="ConfigurePayment.ascx.cs" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
        <tr>
            <td class="FormLabelCell" colspan="2"><b>
                <asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:OrderStrings, Order_Configure_IBiz_E_Payment_Integrator_Component %>" /></b></td>
        </tr>
    </table>
    <br />
    <table class="DataForm">
        <tr>
            <td class="FormLabelCell"><b>
                <asp:Literal ID="Literal2" runat="server" Text="<%$ Resources:SharedStrings, Gateway %>" />:</b></td>
            <td class="FormFieldCell">
                <asp:UpdatePanel UpdateMode="Conditional" ID="GatewaysContentPanel" runat="server" RenderMode="Inline" ChildrenAsTriggers="true">
                    <ContentTemplate>
                        <asp:DropDownList AutoPostBack="True" runat="server" ID="Gateways" Width="330px">
                            <asp:ListItem Value="gwBeanstream" Text="Beanstream" />
                            <asp:ListItem Value="gwAuthorizeNet" Text="<%$ Resources:OrderStrings, Gateway_Authorize_Net %>" />
                            <asp:ListItem Value="gwDPI" Text="<%$ Resources:OrderStrings, Gateway_DPI_Link %>" />
                            <asp:ListItem Value="gwEprocessing" Text="<%$ Resources:OrderStrings, Gateway_eProcessing %>" />
                            <asp:ListItem Value="gwGoRealTime" Text="<%$ Resources:OrderStrings, Gateway_GoRealTime %>" />
                            <asp:ListItem Value="gwIBill" Text="<%$ Resources:OrderStrings, Gateway_IBill_Processing_Plus %>" />
                            <asp:ListItem Value="gwIntellipay" Text="<%$ Resources:OrderStrings, Gateway_Intellipay_ExpertLink %>" />
                            <asp:ListItem Value="gwIOnGate" Text="<%$ Resources:OrderStrings, Gateway_Iongate %>" />
                            <asp:ListItem Value="gwITransact" Text="<%$ Resources:OrderStrings, Gateway_iTransact_RediCharge_HTML %>" />
                            <asp:ListItem Value="gwNetBilling" Text="<%$ Resources:OrderStrings, Gateway_NetBilling %>" />
                            <asp:ListItem Value="gwPayFlowPro" Text="<%$ Resources:OrderStrings, Gateway_Verisign_PayFlow_Pro %>" />
                            <asp:ListItem Value="gwPayready" Text="<%$ Resources:OrderStrings, Gateway_Payready_Link %>" />
                            <asp:ListItem Value="gwUSAePay" Text="<%$ Resources:OrderStrings, Gateway_USA_ePay %>" />
                            <asp:ListItem Value="gwPlugNPay" Text="<%$ Resources:OrderStrings, Gateway_Plug_n_Pay %>" />
                            <asp:ListItem Value="gwPlanetPayment" Text="<%$ Resources:OrderStrings, Gateway_Planet_Payment %>" />
                            <asp:ListItem Value="gwMPCS" Text="<%$ Resources:OrderStrings, Gateway_MPCS %>" />
                            <asp:ListItem Value="gwRTWare" Text="<%$ Resources:OrderStrings, Gateway_RTWare %>" />
                            <asp:ListItem Value="gwECX" Text="<%$ Resources:OrderStrings, Gateway_ECX %>" />
                            <asp:ListItem Value="gwBankOfAmerica" Text="<%$ Resources:OrderStrings, Gateway_Bank_of_America %>" />
                            <asp:ListItem Value="gwInnovative" Text="<%$ Resources:OrderStrings, Gateway_Innovative_Gateway %>" />
                            <asp:ListItem Value="gwMerchantAnywhere" Text="<%$ Resources:OrderStrings, Gateway_Merchant_Anywhere_Transaction_Central %>" />
                            <asp:ListItem Value="gwSkipjack" Text="<%$ Resources:OrderStrings, Gateway_SkipJack %>" />
                            <asp:ListItem Value="gwECHOnline" Text="<%$ Resources:OrderStrings, Gateway_ECHOnline %>" />
                            <asp:ListItem Value="gw3DSI" Text="<%$ Resources:OrderStrings, Gateway_3DSI_EC_Linx %>" />
                            <asp:ListItem Value="gwTrustCommerce" Text="<%$ Resources:OrderStrings, Gateway_TrustCommerce %>" />
                            <asp:ListItem Value="gwPSIGate" Text="<%$ Resources:OrderStrings, Gateway_PSIGate %>" />
                            <asp:ListItem Value="gwPayFuse" Text="<%$ Resources:OrderStrings, Gateway_PayFuse_XML %>" />
                            <asp:ListItem Value="gwPayFlowLink" Text="<%$ Resources:OrderStrings, Gateway_PayFlowLink %>" />
                            <asp:ListItem Value="gwOrbital" Text="<%$ Resources:OrderStrings, Gateway_Paymentech_Orbital_Gateway %>" />
                            <asp:ListItem Value="gwLinkPoint" Text="<%$ Resources:OrderStrings, Gateway_LinkPoint %>" />
                            <asp:ListItem Value="gwMoneris" Text="<%$ Resources:OrderStrings, Gateway_Moneris_eSelect_Plus %>" />
                            <asp:ListItem Value="gwUSight" Text="<%$ Resources:OrderStrings, Gateway_uSight_Gateway %>" />
                            <asp:ListItem Value="gwFastTransact" Text="<%$ Resources:OrderStrings, Gateway_Fast_Transact %>" />
                            <asp:ListItem Value="gwNetworkMerchants" Text="<%$ Resources:OrderStrings, Gateway_NetworkMerchants %>" />
                            <asp:ListItem Value="gwOgone" Text="<%$ Resources:OrderStrings, Gateway_Ogone_DirectLink %>" />
                            <asp:ListItem Value="gwPRIGate" Text="<%$ Resources:OrderStrings, Gateway_Payment_Resources_International_PRIGate %>" />
                            <asp:ListItem Value="gwMerchantPartners" Text="<%$ Resources:OrderStrings, Gateway_Merchant_Partners %>" />
                            <asp:ListItem Value="gwCyberCash" Text="<%$ Resources:OrderStrings, Gateway_CyberCash %>" />
                            <asp:ListItem Value="gwFirstData" Text="<%$ Resources:OrderStrings, Gateway_FirstData_CardService_International %>" />
                            <asp:ListItem Value="gwYourPay" Text="<%$ Resources:OrderStrings, Gateway_YourPay %>" />
                            <asp:ListItem Value="gwACHPayments" Text="<%$ Resources:OrderStrings, Gateway_ACH_Payments %>" />
                            <asp:ListItem Value="gwPaymentsGateway" Text="<%$ Resources:OrderStrings, Gateway_Payments_Gateway %>" />
                            <asp:ListItem Value="gwCyberSource" Text="<%$ Resources:OrderStrings, Gateway_Cyber_Source %>" />
                            <asp:ListItem Value="gwEway" Text="<%$ Resources:OrderStrings, Gateway_Eway %>" />
                            <asp:ListItem Value="gwGoEMerchant" Text="<%$ Resources:OrderStrings, Gateway_Go_EMerchant %>" />
                            <asp:ListItem Value="gwPayStream" Text="<%$ Resources:OrderStrings, Gateway_Pay_Stream %>" />
                            <asp:ListItem Value="gwTransFirst" Text="<%$ Resources:OrderStrings, Gateway_Trans_First %>" />
                            <asp:ListItem Value="gwChase" Text="<%$ Resources:OrderStrings, Gateway_Chase %>" />
                            <asp:ListItem Value="gw5thDimension" Text="5th Dimension Logistics" />
                            <asp:ListItem Value="gwAdyen" Text="Adyen" />
                            <asp:ListItem Value="gwAuthorizeNetXML" Text="Authorize.NET AIM XML" />
                            <asp:ListItem Value="gwBarclay" Text="Barclay" />
                            <asp:ListItem Value="gwBluefin" Text="Bluefin" />
                            <asp:ListItem Value="gwBluePay" Text="BluePay" />
                            <asp:ListItem Value="gwBrainTree" Text="BrainTree" />
                            <asp:ListItem Value="gwConverge" Text="Converge" />
                            <asp:ListItem Value="gwCyberbit" Text="Cyberbit" />
                            <asp:ListItem Value="gwDataCash" Text="Data Cash" />
                            <asp:ListItem Value="gwExPay" Text="ExPay" />
                            <asp:ListItem Value="gwFirstAtlantic" Text="First Atlantic Commerce" />
                            <asp:ListItem Value="gwFirstDataE4" Text="First Data E4" />
                            <asp:ListItem Value="gwFirstDataPayPoint" Text="First Data PayPoint" />
                            <asp:ListItem Value="gwGlobalIris" Text="Global Iris" />
                            <asp:ListItem Value="gwGoToBilling" Text="GoToBilling" />
                            <asp:ListItem Value="gwHeartland" Text="Heartland" />
                            <asp:ListItem Value="gwHSBC" Text="HSBC" />
                            <asp:ListItem Value="gwIntuitPaymentSolutions" Text="Intuit Payment Solutions" />
                            <asp:ListItem Value="gwJetPay" Text="JetPay XML" />
                            <asp:ListItem Value="gwLitle" Text="Litle" />
                            <asp:ListItem Value="gwMerchantESolutions" Text="Merchant E-Solutions" />
                            <asp:ListItem Value="gwMIT" Text="MIT" />
                            <asp:ListItem Value="gwMonerisUSA" Text="Moneris USA" />
                            <asp:ListItem Value="gwMonetra" Text="Monetra" />
                            <asp:ListItem Value="gwMyVirtualMerchant" Text="My Virtual Merchant" />
                            <asp:ListItem Value="gwNetbanx" Text="Netbanx" />
                            <asp:ListItem Value="gwNexCommerce" Text="NexCommerce" />
                            <asp:ListItem Value="gwPayDirect" Text="Pay Direct" />
                            <asp:ListItem Value="gwPayeezy" Text="Payeezy Gateway" />
                            <asp:ListItem Value="gwPayJunction" Text="Pay Junction" />
                            <asp:ListItem Value="gwPayLeap" Text="Pay Leap" />
                            <asp:ListItem Value="gwPayPoint" Text="Pay Point" />
                            <asp:ListItem Value="gwPaymentExpress" Text="Payment Express" />
                            <asp:ListItem Value="gwPaymentWorkSuite" Text="Payment WorkSuite [3DSI]" />
                            <asp:ListItem Value="gwPayscape" Text="Payscape" />
                            <asp:ListItem Value="gwPayTrace" Text="PayTrace" />
                            <asp:ListItem Value="gwPayvision" Text="Payvision" />
                            <asp:ListItem Value="gwProPay" Text="ProPay XML" />
                            <asp:ListItem Value="gwPSIGateXML" Text="PSIGate XML" />
                            <asp:ListItem Value="gwQBMS" Text="QuickBooks Merchant Services" />
                            <asp:ListItem Value="gwSagePay" Text="Sage Payments" />
                            <asp:ListItem Value="gwSECPay" Text="SEC Pay" />
                            <asp:ListItem Value="gwSecurePay" Text="Secure Payments" />
                            <asp:ListItem Value="gwSterling" Text="Sterling XML" />
                            <asp:ListItem Value="gwTransactionCentral" Text="Transaction Central" />
                            <asp:ListItem Value="gwTransNationalBankcard" Text="TransNational Bankcard" />
                            <asp:ListItem Value="gwVerifi" Text="Verifi" />
                            <asp:ListItem Value="gwWorldPay" Text="WorldPay Select Junior" />
                            <asp:ListItem Value="gwWorldPayLink" Text="WorldPay US Link" />
                            <asp:ListItem Value="gwWorldPayXML" Text="WorldPay XML" />
                            <asp:ListItem Value="gwYKC" Text="YKC" />
                            <asp:ListItem Value="gwACHFederal" Text="ACH Federal" />
                        </asp:DropDownList>
                        <asp:RequiredFieldValidator runat="server" ID="GatewayRequired" ControlToValidate="Gateways" ErrorMessage="*" Display="Dynamic"></asp:RequiredFieldValidator>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell" colspan="2">
                <asp:Literal ID="Literal3" runat="server" Text="<%$ Resources:OrderStrings, Order_Get_IBiz_E_Payment_Integrator %>" /></td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell" colspan="2">
                <br />
                <b>
                    <asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:SharedStrings, Configuration_Parameters %>" /></b></td>
        </tr>
    </table>
    <asp:UpdatePanel UpdateMode="Conditional" ID="GatewayParametersUpdatePanel" runat="server" RenderMode="Inline">
        <ContentTemplate>
            <table class="DataForm">
                <tr>
                    <td class="FormLabelCell" style="width: 300px;">
                        <asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:OrderStrings, Order_Payment_Options %>" />:</td>
                    <td class="FormFieldCell" style="width: 330px;">
                        <asp:DropDownList runat="server" ID="TransactionTypeList">
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td colspan="2" class="FormSpacerCell"></td>
                </tr>
            </table>
            <table id="GenericTable" runat="server" class="DataForm">
            </table>
        </ContentTemplate>
    </asp:UpdatePanel>
</div>
