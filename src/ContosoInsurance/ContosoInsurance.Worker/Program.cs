using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ContosoInsurance.Worker
{
    [SupportedOSPlatform("windows")]
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ServiceBase[] { new ClaimsExporterService() });
        }
    }
}
