namespace Play
{
    using System;
    using System.Linq;

    public abstract class App
    {
        protected static bool ContainsArg(string[] args, string arg)
        {
            if (args == null || arg == null)
                return false;

            return args
                .Where(a => a != null)
                .Any(a => arg.Equals(a.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        protected static void SetupConsole()
        {
            if (Console.LargestWindowWidth <= 100 || Console.LargestWindowHeight <= 50)
            {
                return;
            }

            Console.SetWindowSize(
                Console.LargestWindowWidth - 20, 
                Console.LargestWindowHeight - 20);

            if (Console.BufferHeight < 10000)
                Console.SetBufferSize(Console.LargestWindowWidth - 20, 10000);
        }
    }
}
