namespace Play.TimeZone
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    public class Program
    {
        private const string GreenwichStandardTime = "Greenwich Standard Time";
        private const string ChinaStandardTime = "China Standard Time";

        public static void Main(string[] args)
        {
            SetupConsole();

            IsDst(ChinaStandardTime, new DateTime(1991, 9, 10, 0, 0, 0, DateTimeKind.Utc));
            IsDst(GreenwichStandardTime, new DateTime(2014, 7, 16, 0, 0, 0, DateTimeKind.Utc));

            InvestigateOffsetDiffs();

            if (ContainsArg(args, "generate-sql"))
                ConvertDstFileToSql();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static bool ContainsArg(string[] args, string arg)
        {
            if (args == null || arg == null) 
                return false;

            return args
                .Where(a => a != null)
                .Any(a => arg.Equals(a.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static void SetupConsole()
        {
            if (Console.LargestWindowWidth > 100 && Console.LargestWindowHeight > 50)
            {
                Console.SetWindowSize(Console.LargestWindowWidth - 20, Console.LargestWindowHeight - 20);

                if (Console.BufferHeight < 10000)
                    Console.SetBufferSize(Console.LargestWindowWidth - 20, 10000);
            }
        }

        private static void InvestigateOffsetDiffs()
        {
            // China last DST clock change was during 1991
            var dateTime = new DateTime(1991, 1, 1, 0, 0, 0);

            for (int i = 0; i < 366; i++)
                GetOffsetDiff(dateTime.AddDays(i), ChinaStandardTime);

            // UK DST start date 
            dateTime = new DateTime(2014, 3, 30, 0, 0, 0);

            for (int i = 0; i < 1440; i++)
                GetOffsetDiff(dateTime.AddMinutes(i), ChinaStandardTime);

            // UK DST end date
            dateTime = new DateTime(2014, 10, 26, 0, 0, 0);

            for (int i = 0; i < 1440; i++)
                GetOffsetDiff(dateTime.AddMinutes(i), ChinaStandardTime);            
        }

        private static void GetOffsetDiff(DateTime dateTime, string timezoneId)
        {
            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(GreenwichStandardTime);
            var otherTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

            var instant = new DateTimeOffset(dateTime);

            TimeSpan localOffset = localTimeZone.GetUtcOffset(instant);
            TimeSpan otherOffset = otherTimeZone.GetUtcOffset(instant);

            Console.WriteLine("{0} - {1}", instant, otherOffset - localOffset);           
        }

        private static void IsDst(string timezoneId, DateTime dateTime)
        {
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

            dateTime = TimeZoneInfo.ConvertTime(dateTime, tzi);

            bool isDaylightSavings = tzi.IsDaylightSavingTime(dateTime);

            Console.WriteLine(
                "'{0}' was {1} in DST on {2}",
                timezoneId,
                isDaylightSavings ? string.Empty : "NOT",
                dateTime);
        }

        private static void ConvertDstFileToSql()
        {
            var xml = XDocument.Load(@"..\..\uk-dst.xml");

            var rows = xml.Root.Elements("tr");

            var fileText = new StringBuilder();

            foreach (var row in rows)
            {
                var header = row.Element("th");

                var anchor = header.Element("a");

                var year = anchor != null && anchor.Value != "*"
                    ? anchor.Value
                    : header.Value.Substring(header.Value.Length - 4);

                var cells = row.Elements("td").ToList();

                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.MinValue;

                if (cells.Count > 1 && cells[1].Value != "No DST End")
                {
                    startDate = ParseDate(cells[0].Value, year);
                    endDate = ParseDate(cells[1].Value, year);
                }

                string sql = string.Format(
                    "INSERT CalendarDstUk ([Year], StartDateTimeLocal, EndDateTimeLocal) " +
                    "VALUES (CONVERT(datetime, '{0}', 120), CONVERT(datetime, '{1}', 120), CONVERT(datetime, '{2}', 120))",
                    new DateTime(startDate.Year, 1, 1).ToString("yyyy/MM/dd HH:mm:ss"),
                    startDate.ToString("yyyy/MM/dd HH:mm:ss"),
                    endDate.ToString("yyyy/MM/dd HH:mm:ss"));

                if (startDate.Year >= 1972)
                    fileText.AppendLine(sql);

                Console.WriteLine(sql);
            }

            File.WriteAllText(
                @"..\..\uk-dst.sql",
                fileText.ToString());
        }

        private static DateTime ParseDate(string date, string year)
        {
            return DateTime.ParseExact(
                string.Format("{0}, {1}", date, year),
                "dddd, d MMMM, HH:mm, yyyy",
                null);
        }
    }
}
