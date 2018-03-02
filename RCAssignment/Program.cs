using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RC.Assignment
{
    /// <summary>
    /// Entry point into application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main method for this project.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            bool exit = false;
            do
            {
                Console.Clear();

                // Removes previously generated log files, if any.
                Utilities.CleanUpDir(AppParams.LogFileDir);

                Console.WriteLine("Please press Alt+Enter to full screen for improved readability.");
                Console.WriteLine("\nThis app generates sample log files with random data to facilitate organic testing. Please find 'TestData' folder in the same directory where the RCAssignment.exe is located.\n");

                // Generate new log files : spits out one log file per device. File name format is Device_<deviceID>.txt. 
                LogFilesGen testLogFileGen = new LogFilesGen(logFilesLmt: readLogFilesLmt(), logLinesLmt: readLogEntiesLmt());
                testLogFileGen.GenerateLogFiles();

                Console.WriteLine("\n---------------------------Results---------------------------");

                // Queue a separate task for processing each log file found in the log files directory.
                List<Task> logProcessorTasks = new List<Task>();
                DirectoryInfo logDirInfo = new DirectoryInfo(AppParams.LogFileDir);

                foreach (FileInfo fileInfo in logDirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
                {
                    var task = new Task(() => processLogFile(fileInfo));
                    logProcessorTasks.Add(task);
                }

                logProcessorTasks.ForEach(x => x.Start());

                // Wait on all tasks to complete. The following statement blocks execution until all the taks are completed.
                Task.WaitAll(logProcessorTasks.ToArray());

                Console.WriteLine("\nYou can now verify the results on the screen against generated sample input files.");
                Console.WriteLine("In addition, you can also look at the unit tests in EventCounterTests.cs.");
                Console.WriteLine(string.Format("\n\nDeveloped by Amandeep Singh. LinkedIn: {0} | Phone #: {1} | Email: {2}", "https://www.linkedin.com/in/asinghca/", "(778) 345-5356", "me@asingh.io"));

                // Clean up any generated log files.
                //Utilities.CleanUpDir(AppParams.LogFileDir);

                Console.WriteLine("\nPress any key to test again or 'x' to exit...");
                var input = Console.ReadKey();
                exit = (input.KeyChar == 'x' || input.KeyChar == 'X');

            } while (!exit);
        }

        /// <summary>
        /// Logic to handle one file per thread.
        /// </summary>
        /// <param name="fileInfo"> Log file to process </param>
        private static void processLogFile(FileInfo fileInfo)
        {
            string pattern = @"^device_(.+).txt";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(fileInfo.Name);

            if (matches.Count != 1 && matches[0].Groups.Count != 2)
            {
                return;
            }

            string deviceId = matches[0].Groups[1].Value;
            StreamReader eventLog = new StreamReader(fileInfo.FullName);

            IEventCounter eventCounter = EventCounter.Instance;
            eventCounter.ParseEvents(deviceId, eventLog);
            int faultySeqCount = eventCounter.GetEventCount(deviceId);

            Console.WriteLine(string.Format("File : {0} | Device ID : {1} | Fault Event Count : {2}", fileInfo.Name, deviceId, faultySeqCount));
        }

        private static int readLogFilesLmt()
        {
            return Utilities.ReadNumberFromConsole(
               prompt: "Given that each file contains data for a unique device. Please specify how many log files you would like to generate for testing this app: > ",
               error: "Incorrect input. Please enter a value between 1 to 1000!\n",
               lowerLmt: 1,
               upperLmt: 1000);
        }

        private static int readLogEntiesLmt()
        {
            return Utilities.ReadNumberFromConsole(
               prompt: "How many log entries would you like to put in each file: >  ",
               error: "Incorrect input. Please enter a value between 1 to 1000!\n",
               lowerLmt: 1,
               upperLmt: 1000);
        }
    }
}
