namespace Play.WindowsServiceControlApi
{
    using System;
    using System.Net;

    public class Program : App
    {
        public static void Main(string[] args)
        {
            Initialise(args);

            var domainName = args[0];
            var targetMachine = args[1];
            var adminUser = args[2];
            var adminPassword = args[3];
            var serviceLogOnUser = args[4];
            var serviceLogOnPassword = args[5];
            var serviceExePath = args[6];
            var windowsServiceName = args[7];
            var windowsServiceDescription = args[8];

            var removeOnly 
                = args.Length > 9 
                && "-removeOnly".Equals(args[9], StringComparison.OrdinalIgnoreCase);

            var exists = WindowsServiceControlManager.IsServiceInstalled(
                targetMachine,
                windowsServiceName);

            var windowsServiceControlManager = new WindowsServiceControlManager(
                targetMachine,
                new NetworkCredential(adminUser, adminPassword, domainName));

            if (exists)
            {
                windowsServiceControlManager.RemoveService(windowsServiceName);
            }

            if (!removeOnly)
            {
                windowsServiceControlManager.InstallService(
                    windowsServiceName,
                    windowsServiceDescription,
                    serviceExePath,
                    new NetworkCredential(serviceLogOnUser, serviceLogOnPassword, domainName));
            }

            Finalise();
        }
    }
}
