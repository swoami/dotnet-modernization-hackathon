using System.ServiceProcess;

namespace ContosoInsurance.Worker
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ServiceBase[] { new ClaimsExporterService() });
        }
    }
}
