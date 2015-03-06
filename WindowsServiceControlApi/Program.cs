namespace Play.WindowsServiceControlApi
{
    using System.Net;

    public class Program : App
    {
        private const string DomainName = "DomainName";

        private const string WindowsServiceName = "Test Windows Service";

        private const string TargetMachine = "MachineName";

        private const string ServiceExePath = @"C:\change\me.exe";

        private const string AdminUser = "administrator";

        private const string AdminPassword = "ChangeMe";

        private const string ServiceLogOnUser = "LogonUser";

        private const string ServiceLogOnPassword = "ChangeMe";

        public static void Main(string[] args)
        {
            Initialise(args);

            bool exists = WindowsServiceControlManager.IsServiceInstalled(
                TargetMachine,
                WindowsServiceName);

            var windowsServiceControlManager = new WindowsServiceControlManager(
                TargetMachine,
                new NetworkCredential(AdminUser, AdminPassword, DomainName));

            if (exists)
            {
                windowsServiceControlManager.RemoveService(WindowsServiceName);
            }

            windowsServiceControlManager.InstallService(
                WindowsServiceName,
                WindowsServiceName + " Description",
                ServiceExePath,
                new NetworkCredential(ServiceLogOnUser, ServiceLogOnPassword, DomainName));

            Finalise();
        }
    }
}
