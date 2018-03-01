using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RC.Assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            // Removes previously generated log files, if any.
            Utilities.CleanUpDir(AppParams.LogFileDir);

            // Spits out one log file per device. File name format is Device_<deviceID>.txt. 
            LogFilesGen testLogFileGen = new LogFilesGen(logFilesLmt: 10, logLinesLmt: 50);
            testLogFileGen.GenerateLogFiles();


            // Queue a separate task for processing each log file found in the log files directory.
            List<Task> logProcessorTasks = new List<Task>();
            DirectoryInfo logDirInfo = new DirectoryInfo(AppParams.LogFileDir);

            foreach (FileInfo fileInfo in logDirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            {
                //ProcessLogFile(fileInfo);
                var task = new Task(() => ProcessLogFile(fileInfo));
                task.Start();
                logProcessorTasks.Add(task);
            }

            // Wait on all tasks to complete.
            Task.WaitAll(logProcessorTasks.ToArray());
            Console.WriteLine("HOLA");

            // Clean up any generated log files.
            //Utilities.CleanUpDir(AppParams.LogFileDir);
        }

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
