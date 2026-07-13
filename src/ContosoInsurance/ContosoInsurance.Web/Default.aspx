<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ContosoInsurance.Web.Default" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Contoso Claims Portal</title>
</head>
<body>
    <form id="mainForm" runat="server">
        <h1>Recent Claims</h1>
        <asp:GridView ID="ClaimsGrid" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:BoundField DataField="ClaimId" HeaderText="Id" />
                <asp:BoundField DataField="PolicyNumber" HeaderText="Policy" />
                <asp:BoundField DataField="ClaimantName" HeaderText="Claimant" />
                <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:C}" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="FiledOn" HeaderText="Filed" DataFormatString="{0:yyyy-MM-dd}" />
                <asp:BoundField DataField="Score" HeaderText="Score" />
            </Columns>
        </asp:GridView>
        <p><a href="Upload.aspx">Upload a claim document</a></p>
    </form>
</body>
</html>
