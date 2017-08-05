using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Configuration;

namespace MailRepeater
{
    class Program
    {
        static int CheckInterval;                            //Number of seconds to wait between checks for new mail
        static string DropFolder;                            //The folder where incoming mail is dropped by the SMTP service
        static string PickupFolder;                          //The folder where the SMTP service picks up mail for sending
        static string Sender;                                //An email address that sends the repeated emails
        static List<string> KeepHeaders;                     //A list of EML headers that are to be retained while repeating
        static Dictionary<string, string> DestinationList;   //Specification of where emails should be sent

        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("MailRepeater 1.2.0.0");
            Console.WriteLine("");

            //Load configuration data
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            AppSettingsSection section1 = (AppSettingsSection)config.GetSection("appSettings");
            AppSettingsSection section2 = (AppSettingsSection)config.GetSection("keepHeaderList");
            AppSettingsSection section3 = (AppSettingsSection)config.GetSection("destinationList");

            int checkInterval = 0;
            int.TryParse(section1.Settings["CheckInterval"].Value, out checkInterval);
            CheckInterval = checkInterval;

            DropFolder = section1.Settings["DropFolder"].Value;
            PickupFolder = section1.Settings["PickupFolder"].Value;
            Sender = section1.Settings["Sender"].Value;

            KeepHeaders = new List<string>();
            foreach (string key in section2.Settings.AllKeys)
                KeepHeaders.Add(key.ToLower());

            DestinationList = new Dictionary<string, string>();
            foreach (string key in section3.Settings.AllKeys)
                if (!DestinationList.ContainsKey(key))
                    DestinationList.Add(key.ToLower().Trim(), section3.Settings[key].Value);

            if (checkInterval <= 0)
            {
                Console.WriteLine("ERROR: The CheckInterval value must be a number greater than or equal to one.");
                Console.WriteLine("Press any key..");
                Console.ReadKey();
                return;
            }

            if (!Directory.Exists(DropFolder))
            {
                Console.WriteLine("ERROR: The DropFolder directory could not be located.");
                Console.WriteLine("Press any key..");
                Console.ReadKey();
                return;
            }

            if (!Directory.Exists(PickupFolder))
            {
                Console.WriteLine("ERROR: The PickupFolder directory could not be located.");
                Console.WriteLine("Press any key..");
                Console.ReadKey();
                return;
            }

            if (!DestinationList.ContainsKey("*"))
            {
                Console.WriteLine("ERROR: The destination list must contain an item with global match (i.e. '*').");
                Console.WriteLine("Press any key..");
                Console.ReadKey();
                return;
            }

            CountDown();
        }

        static void CountDown()
        {
            Process();

            int processCountDown = CheckInterval;

            while (processCountDown > 0)
            {
                Thread.Sleep(1000);
                Console.Title = "MailRepeater (Waiting " + processCountDown.ToString() + ")";
                processCountDown--;
            }

            CountDown();
        }

        static void Process()
        {
            List<string> fileList = Directory.GetFiles(DropFolder).ToList();

            foreach (string inputFile in fileList)
            {
                try
                {
                    List<string> lines = File.ReadAllLines(inputFile).ToList();

                    MailProcessor processor = new MailProcessor(KeepHeaders, DestinationList, Sender);
                    string output = processor.Process(lines);

                    if (processor.HasRecipients)
                    {
                        string outputFile = Path.Combine(PickupFolder, Path.GetFileName(inputFile));
                        File.WriteAllText(outputFile, output);

                        Console.WriteLine("Processed input: " + inputFile);
                        Console.WriteLine("Output file:     " + outputFile);
                        Console.WriteLine(new String('-', Console.WindowWidth));
                    }

                    File.Delete(inputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(new String('-', Console.WindowWidth));
                }
            }
        }

    }
}
