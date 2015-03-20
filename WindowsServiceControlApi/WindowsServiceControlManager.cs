namespace Play.WindowsServiceControlApi
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Wrapper around the Service Control Manager Win32 API
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms685942(v=vs.85).aspx
    /// </summary>
    public class WindowsServiceControlManager
    {
        private string hostName;

        private NetworkCredential adminUser;

        internal WindowsServiceControlManager(string hostName, NetworkCredential adminUser)
        {
            this.hostName = hostName;
            this.adminUser = adminUser;
        }

        internal void InstallService(
            string name,
            string description,
            string executablePath,
            NetworkCredential logOnAccount)
        {
            using (var impersonator = new Impersonator(this.adminUser))
            using (var serviceControlManagerHandle = OpenServiceControlManager(this.hostName))
            using (var serviceHandle = this.CreateService(serviceControlManagerHandle, name, executablePath, logOnAccount))
            {
                this.UpdateDescription(serviceHandle, description);
                this.UpdateFailureActions(serviceHandle);
            }
        }

        internal void RemoveService(string name)
        {
            using (var impersonator = new Impersonator(this.adminUser))
            using (var serviceControlManagerHandle = OpenServiceControlManager(this.hostName))
            using (var serviceHandle = this.OpenService(serviceControlManagerHandle, name))
            {
                this.DeleteService(serviceHandle);
            }
        }

        /// <summary>
        /// Hack method without having to impersonate admin user.
        /// </summary>
        internal static bool IsServiceInstalled(string hostName, string serviceName)
        {
            using (var scmHandle = OpenServiceControlManager(hostName, Win32ServiceControl.SCM_ACCESS.SC_MANAGER_CONNECT))
            {
                if (scmHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }

                using (
                    var serviceHandle = Win32ServiceControl.OpenService(
                        scmHandle,
                        serviceName,
                        Win32ServiceControl.SERVICE_ACCESS.SERVICE_QUERY_STATUS))
                {
                    if (!serviceHandle.IsInvalid)
                    {
                        return true;
                    }

                    var win32Exception = new Win32Exception();

                    switch (win32Exception.NativeErrorCode)
                    {
                        case 5:     // Access is denied, but that means it very likely exists!
                            return true;
                        case 1060:  // The specified service does not exist as an installed service
                            return false;
                        default:
                            throw win32Exception;
                    }
                }
            }
        }

        private void UpdateFailureActions(Win32ServiceControlHandle serviceHandle)
        {
            const int DelayMilliseconds = 60 * 1000;
            const int ActionCount = 3;
            var actions = new int[ActionCount * 2];
            int i = 0;

            actions[i++] = (int)Win32ServiceControl.ACTION_TYPE.SC_ACTION_RESTART;
            actions[i++] = DelayMilliseconds;
            actions[i++] = (int)Win32ServiceControl.ACTION_TYPE.SC_ACTION_RESTART;
            actions[i++] = DelayMilliseconds;
            actions[i++] = (int)Win32ServiceControl.ACTION_TYPE.SC_ACTION_RESTART;
            actions[i++] = DelayMilliseconds;

            IntPtr buffer = Marshal.AllocHGlobal(ActionCount * 8);

            Marshal.Copy(actions, 0, buffer, ActionCount * 2);

            var failureActions = new Win32ServiceControl.SERVICE_FAILURE_ACTIONS
            {
                cActions = ActionCount,
                dwResetPeriod = 4,
                lpCommand = null,
                lpRebootMsg = null,
                lpsaActions = new IntPtr(buffer.ToInt32())
            };

            int returnValue = Win32ServiceControl.ChangeServiceFailureActions(
                serviceHandle,
                Win32ServiceControl.INFO_LEVEL.SERVICE_CONFIG_FAILURE_ACTIONS,
                ref failureActions);

            if (returnValue == 0)
            {
                ThrowNewWin32Exception("Failed to update failure actions.");
            }
        }

        private void UpdateDescription(
            Win32ServiceControlHandle serviceHandle,
            string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            var desc = new Win32ServiceControl.SERVICE_DESCRIPTION
            {
                lpDescription = description
            };

            int returnValue = Win32ServiceControl.ChangeServiceDescription(
                serviceHandle,
                Win32ServiceControl.INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION,
                ref desc);

            if (returnValue == 0)
            {
                ThrowNewWin32Exception(
                    "Failed to update the description to '{0}' on {1}.",
                    description,
                    this.hostName);
            }
        }

        private Win32ServiceControlHandle CreateService(
            Win32ServiceControlHandle serviceControlManagerHandle,
            string name,
            string executablePath,
            NetworkCredential logOnAccount)
        {
            var serviceHandle = Win32ServiceControl.CreateService(
                serviceControlManagerHandle,
                name,
                name,
                Win32ServiceControl.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                Win32ServiceControl.SERVICE_TYPES.SERVICE_WIN32_OWN_PROCESS,
                Win32ServiceControl.SERVICE_START_TYPES.SERVICE_AUTO_START,
                Win32ServiceControl.SERVICE_ERROR_CONTROL.SERVICE_ERROR_NORMAL,
                executablePath,
                null,
                IntPtr.Zero,
                null,
                string.Format(@"{0}\{1}", logOnAccount.Domain, logOnAccount.UserName),
                logOnAccount.Password);

            if (serviceHandle.IsInvalid)
            {
                ThrowNewWin32Exception(
                    "Failed to create '{0}' on {1}.",
                    name,
                    this.hostName);
            }

            return serviceHandle;
        }

        private void DeleteService(Win32ServiceControlHandle serviceHandle)
        {
            int returnValue = Win32ServiceControl.DeleteService(serviceHandle);

            if (returnValue == 0)
            {
                ThrowNewWin32Exception(
                    "Failed to remove service on {0}.",
                    this.hostName);
            }
        }

        private static void ThrowNewWin32Exception(string messageFormat, params object[] args)
        {
            var win32Exception = new Win32Exception();

            Log.Error(messageFormat, args);

            throw win32Exception;
        }

        private static Win32ServiceControlHandle OpenServiceControlManager(
            string hostName,
            Win32ServiceControl.SCM_ACCESS access = Win32ServiceControl.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE)
        {
            if (Environment.MachineName.Equals(hostName, StringComparison.OrdinalIgnoreCase))
            {
                hostName = null;
            }

            var serviceControlManagerHandle = Win32ServiceControl.OpenSCManager(
                hostName,
                null,
                access);

            if (serviceControlManagerHandle.IsInvalid)
            {
                ThrowNewWin32Exception(
                    "Failed to open the Service Control Manager on {0}.",
                    hostName);
            }

            return serviceControlManagerHandle;
        }

        private Win32ServiceControlHandle OpenService(
            Win32ServiceControlHandle serviceControlManagerHandle,
            string name)
        {
            var serviceHandle = Win32ServiceControl.OpenService(
                serviceControlManagerHandle,
                name,
                Win32ServiceControl.SERVICE_ACCESS.SERVICE_ALL_ACCESS);

            if (serviceHandle.IsInvalid)
            {
                ThrowNewWin32Exception(
                    "Failed to get a handle on '{0}' on {1}.",
                    name,
                    this.hostName);
            }

            return serviceHandle;
        }
    }
}
