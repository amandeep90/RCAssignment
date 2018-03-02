using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RC.Assignment
{
    /// <summary>
    /// Collection of reusable utility functions.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Parses log data, line by line, stored in the log file format. Example "2011-03-07 06:25:32 2"
        /// </summary>
        /// <param name="data">Array of string where each element represents a log line</param>
        /// <returns>Senitized and parsed list of ILogItem ordered by asc log time</returns>
        public static IEnumerable<ILogItem> ParseLogData(string[] data)
        {
            IList<ILogItem> logItems = new List<ILogItem>();

            if (data.Length == 0)
            {
                return logItems;
            }

            foreach (var rawLine in data)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var dataItems = rawLine.Split('\t');

                DateTime logTime;
                int stage;

                if (!DateTime.TryParse(dataItems[0], out logTime))
                {
                    throw new InvalidDataException("Failed to parse log time : " + dataItems[0]);
                }

                if (!int.TryParse(dataItems[1], out stage))
                {
                    throw new InvalidDataException("Failed to parse stage value : " + dataItems[1]);
                }

                logItems.Add(new LogItem { LogTime = logTime, Stage = stage });
            }

            return logItems.Distinct().OrderBy(x => x.LogTime); // Uses IEquatable logic for distinct() defined in LogItems class.
        }

        /// <summary>
        /// Reads the content of the specified file.
        /// </summary>
        /// <param name="fileName"> Full path including file name. </param>
        /// <returns> All the lines in the specified file. </returns>
        public static string[] GetLogLines(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("Couldn't locate log file at " + fileName);
            }

            string[] readText = File.ReadAllLines(fileName);

            return readText;
        }

        /// <summary>
        /// Deletes all the files in a directory.
        /// </summary>
        /// <param name="path"> Absolute path to clean up. </param>
        public static void CleanUpDir(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(path);

            foreach (FileInfo file in dirInfo.GetFiles())
            {
                file.Delete();
            }
        }

        private static readonly Random getrandom = new Random();

        /// <summary>
        /// Generates a random number between the specified range.
        /// </summary>
        /// <param name="min"> Inclusive minimum bound for return value </param>
        /// <param name="max"> Inclusive maximum bound for return value </param>
        /// <returns> A random value within specified range </returns>
        public static int GetRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max + 1);
            }
        }

        /// <summary>
        /// Reads a number from console window
        /// </summary>
        /// <param name="prompt"> Message to show to get the input value </param>
        /// <param name="error"> Error to show when input is incorrect </param>
        /// <param name="lowerLmt"> Lowest accepted value </param>
        /// <param name="upperLmt"> Highest accepted value </param>
        /// <returns> Integer value </returns>
        public static int ReadNumberFromConsole(string prompt, string error, int lowerLmt, int upperLmt)
        {
            int number = 0;
            bool validInput = false;
            do
            {
                Console.Write(prompt);
                string logFilesInput = Console.ReadLine();
                validInput = int.TryParse(logFilesInput, out number);

                if (!validInput || (number < lowerLmt || number > upperLmt))
                {
                    Console.WriteLine(error);
                    validInput = false;
                }

            } while (!validInput);

            return number;
        }
    }
}
