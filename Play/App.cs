namespace Play
{
    using System;
    using System.Linq;

    public abstract class App
    {
        private static string[] arguments;

        protected static void Initialise(string[] args)
        {
            arguments = args;

            SetupConsole();

            Log.Info("Started {0}...", Environment.CommandLine);
        }

        protected static void Finalise()
        {
            Log.Info("Finished {0}...", Environment.CommandLine);
            Log.Info("Press any key to exit.");
            Console.ReadKey();            
        }

        protected static bool WasArgPassed(string arg)
        {
            if (arguments == null || arg == null)
            {
                return false;
            }

            return arguments
                .Where(a => a != null)
                .Any(a => arg.Equals(a.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static void SetupConsole()
        {
            if (Console.LargestWindowWidth <= 100 || Console.LargestWindowHeight <= 50)
            {
                return;
            }

            Console.SetWindowSize(
                width: Console.LargestWindowWidth - 20, 
                height: Console.LargestWindowHeight - 20);

            if (Console.BufferHeight >= 10000)
            {
                return;
            }

            Console.SetBufferSize(
                width: Console.LargestWindowWidth - 20,
                height: 10000);
        }
    }
}
