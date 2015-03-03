namespace Play.WindowsServiceControlApi
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Security.Principal;

    internal class Impersonator : IDisposable
    {
        private readonly WindowsImpersonationContext context;

        private bool disposed;

        internal Impersonator(NetworkCredential credential)
        {
            IntPtr adminToken;

            int returnValue = Win32Impersonation.LogonUser(
                credential.UserName,
                credential.Domain,
                credential.Password,
                (int)Win32Impersonation.LogonType.Interactive,
                (int)Win32Impersonation.LogonProvider.WinNT50,
                out adminToken);

            if (returnValue != 0)
            {
                this.context = WindowsIdentity.Impersonate(adminToken);
                return;
            }

            var win32Exception = new Win32Exception();

            throw new Exception(
                string.Format("Failed to logon as user {0} for impersonation.", credential.UserName),
                win32Exception);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // free managed objects here
                this.context.Dispose();
            }

            // free unmanaged objects here
            this.disposed = true;
        }
    }
}
