namespace Play
{
    using System;

    public static class Log
    {
        private const string DateFormat = "dd/MM/yy HH:mm:ss.fff";

        public static void Info(string format, params object[] args)
        {
            WriteLine(format, args);          
        }

        public static void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            WriteLine(format, args);

            Console.ResetColor();
        }

        private static void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(
                "{0} - {1}",
                DateTime.Now.ToString(DateFormat),
                string.Format(format, args));            
        }
    }
}
