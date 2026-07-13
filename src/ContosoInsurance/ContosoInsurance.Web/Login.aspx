<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="ContosoInsurance.Web.LoginPage" %>
<!DOCTYPE html>
<html>
<head runat="server"><title>Sign in</title></head>
<body>
    <form id="loginForm" runat="server">
        <h1>Sign in</h1>
        <p>Username: <asp:TextBox ID="UsernameBox" runat="server" /></p>
        <p>Password: <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" /></p>
        <asp:Button ID="SignInBtn" runat="server" Text="Sign in" OnClick="SignInBtn_Click" />
        <asp:Label ID="ErrorLabel" runat="server" ForeColor="Red" />
    </form>
</body>
</html>
