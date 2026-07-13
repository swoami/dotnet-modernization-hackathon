using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ContosoInsurance.Worker
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "ContosoClaimsExporter",
                DisplayName = "Contoso Claims Exporter",
                Description = "Exports claims to CSV on a schedule.",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
