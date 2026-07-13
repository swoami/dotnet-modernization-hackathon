using System;
using System.Web.Security;
using System.Web.UI;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Data;

namespace ContosoInsurance.Web
{
    public partial class LoginPage : Page
    {
        protected void SignInBtn_Click(object sender, EventArgs e)
        {
            var users = new UserRepository();
            var user = users.FindByUsername(UsernameBox.Text.Trim());
            if (user == null || !users.VerifyPassword(user, PasswordBox.Text))
            {
                AppLogger.Warn("Failed login for " + UsernameBox.Text);
                ErrorLabel.Text = "Invalid credentials.";
                return;
            }

            FormsAuthentication.SetAuthCookie(user.Username, false);
            Response.Redirect(FormsAuthentication.GetRedirectUrl(user.Username, false));
        }
    }
}
