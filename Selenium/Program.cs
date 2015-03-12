namespace Play.Selenium
{
    using System;
    using System.Linq;

    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.Support.UI;

    public class Program : App
    {
        public static void Main(string[] args)
        {
            using (var firefox = new FirefoxDriver())
            {
                firefox.Navigate().GoToUrl(args[0] + "/enterprise/account/Login");

                firefox.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));

                firefox.FindElement(By.Id("login_Email")).SendKeys(args[2]);
                firefox.FindElement(By.Id("login_Password")).SendKeys(args[3]);
                firefox.FindElement(By.Id("login_Password")).SendKeys(Keys.Enter);

                firefox.Navigate().GoToUrl(args[0] + "/enterprise/BookingsCentre/MemberTimetable");

                if (args.Length > 4)
                {
                    var dropdown = new SelectElement(firefox.FindElement(By.ClassName("inputWidthMedium")));
                    dropdown.SelectByText(args[4]);
                }

                var table = firefox.FindElementById("MemberTimetable");
                
                var rows = table.FindElements(By.CssSelector("tbody tr"));

                firefox.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(0));

                var cell =
                    rows.Where(r => r.FindElements(By.PartialLinkText(args[1])).Count != 0)
                        .Select(r => r.FindElement(By.ClassName("col6Item")))
                        .FirstOrDefault();

                if (cell == null) 
                {
                    Log.Info("Could not find 'Book' cell.");
                    return;
                }

                /*
                if (cell.Text != "Book")
                {
                    Log.Info("Not bookable!");
                    return;
                }
                */

                cell.Click();

                firefox.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));

                firefox.Navigate().GoToUrl(args[0] + "/enterprise/Basket/Pay");
            }
        }
    }
}
