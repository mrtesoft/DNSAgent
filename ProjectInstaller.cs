using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DNSAgent
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {

            
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = "DNSAgent";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
          
            // Automatically start after install
            AfterInstall += (sender, args) =>
            {
                using (var serviceController = new ServiceController(serviceInstaller.ServiceName))
                    serviceController.Start();
            };

            Installers.AddRange(new Installer[]
            {
                serviceProcessInstaller,
                serviceInstaller
            });
             
        }
    }
}