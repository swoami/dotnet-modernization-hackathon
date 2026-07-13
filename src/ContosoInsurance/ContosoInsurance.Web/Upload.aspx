<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="ContosoInsurance.Web.Upload" %>
<!DOCTYPE html>
<html>
<head runat="server"><title>Upload Claim Document</title></head>
<body>
    <form id="uploadForm" runat="server" enctype="multipart/form-data">
        <h1>Upload claim document</h1>
        <p>Claim ID: <asp:TextBox ID="ClaimIdBox" runat="server" /></p>
        <p>File: <asp:FileUpload ID="FileUploadCtrl" runat="server" /></p>
        <asp:Button ID="SubmitBtn" runat="server" Text="Upload" OnClick="SubmitBtn_Click" />
        <asp:Label ID="StatusLabel" runat="server" ForeColor="Green" />
    </form>
</body>
</html>
