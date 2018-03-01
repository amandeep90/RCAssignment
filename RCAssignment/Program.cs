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
    class Program
    {
        /// <summary>
        /// The main method for this project.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Removes previously generated log files, if any.
            Utilities.CleanUpDir(AppParams.LogFileDir);

            // Generate new log files : spits out one log file per device. File name format is Device_<deviceID>.txt. 
            LogFilesGen testLogFileGen = new LogFilesGen(logFilesLmt: 10, logLinesLmt: 50);
            testLogFileGen.GenerateLogFiles();

            // Queue a separate task for processing each log file found in the log files directory.
            List<Task> logProcessorTasks = new List<Task>();
            DirectoryInfo logDirInfo = new DirectoryInfo(AppParams.LogFileDir);

            foreach (FileInfo fileInfo in logDirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            {
                var task = new Task(() => ProcessLogFile(fileInfo));
                task.Start();
                logProcessorTasks.Add(task);
            }

            // Wait on all tasks to complete. The following statement blocks execution until all the taks are completed.
            Task.WaitAll(logProcessorTasks.ToArray());

            // Clean up any generated log files.
            //Utilities.CleanUpDir(AppParams.LogFileDir);
        }

        /// <summary>
        /// Logic to handle one file per thread.
        /// </summary>
        /// <param name="fileInfo"> Log file to process </param>
        static void ProcessLogFile(FileInfo fileInfo)
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

            EventCounter.Instance.ParseEvents(deviceId, eventLog);
            Console.WriteLine(string.Format("Device ID : {0} | Fault Event Count : {1}", deviceId, EventCounter.Instance.GetEventCount(deviceId)));
        }
    }
}
