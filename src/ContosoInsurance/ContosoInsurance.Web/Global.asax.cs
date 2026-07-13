using System;
using System.Web;
using ContosoInsurance.Common.Logging;

namespace ContosoInsurance.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            AppLogger.Configure();
            AppLogger.Info("ContosoInsurance.Web starting");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            AppLogger.Error("Unhandled exception", ex);
        }

        protected void Application_End(object sender, EventArgs e)
        {
            AppLogger.Info("ContosoInsurance.Web stopping");
        }
    }
}
