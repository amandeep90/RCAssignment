using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RC.Assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            TestLogFileGen testLogFileGen = new TestLogFileGen(AppConfig.WorkingDir, 10);
            testLogFileGen.GenerateLogFile();

        }
    }

    static class AppConfig
    {
        /// <summary>
        /// Working directory to be used for this project. Points to the folder where application is being executed.
        /// </summary>
        public static string WorkingDir => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    }

    class TestLogFileGen
    {
        private string _rootPath;
        TestDataGen testDataGen;

        public TestLogFileGen(string rootPath, int logLinesLmt)
        {
            _rootPath = rootPath;
            testDataGen = new TestDataGen(logLinesLmt);
        }

        /// <summary>
        /// Generates a sample input log file for testing. File is stored under the "TestData" folder where app executes.
        /// </summary>
        public void GenerateLogFile()
        {
            if (!Directory.Exists(_rootPath))
            {
                throw new DirectoryNotFoundException("Invalid root directory provided to the log file generator");
            }

            string logFileDir = string.Format("{0}\\TestData", _rootPath);
            string logFileName = string.Format("{0}\\SampleInput.txt", logFileDir);

            if (!Directory.Exists(logFileDir))
            {
                Directory.CreateDirectory(logFileDir);
            }

            IList<string> logLines = testDataGen.GenerateData();

            using (StreamWriter file = new StreamWriter(logFileName, false))
            {
                foreach (var line in logLines)
                {
                    file.WriteLine(line);
                }
            }
        }
    }

    class TestDataGen
    {
        private int _targetLines;
        private DateTime _startTime;
        private readonly Random getrandom = new Random();

        public TestDataGen(int linesLmt)
        {
            if (linesLmt < 2)
            {
                throw new InvalidDataException("Please generate at least two lines of data");
            }

            _targetLines = linesLmt;
            _startTime = new DateTime(2001, 1, 1); // Should be enough to generate ample amount of test data.
        }

        /// <summary>
        /// Generates artificial log data for testing.
        /// </summary>
        /// <returns> IList of string - each entry is a log line. Ordered by date with oldest record first in the list. </returns>
        public IList<string> GenerateData()
        {
            IList<string> logLines = new List<string>();

            for (int i = 0; i < _targetLines; i++)
            {
                logLines.Add(GetSampleLog());
            }

            return logLines;
        }

        /// <summary>
        /// Generates a sample input data line for artificial log file.
        /// </summary>
        /// <returns> A line for log in this format: "1998-03-07 06:25:32	2" </returns>
        private string GetSampleLog()
        {
            _startTime = _startTime.AddMinutes(GetRandomNumber(1, 10));
            string line = string.Format("{0}\t{1}", _startTime.ToString("yyyy-MM-dd HH:mm:ss"), GetRandomNumber(0, 3));

            return line;
        }

        /// <summary>
        /// Generates a random number between the specified range.
        /// </summary>
        /// <param name="min"> Inclusive minimum bound for return value </param>
        /// <param name="max"> Inclusive maximum bound for return value </param>
        /// <returns> A random value within specified range </returns>
        private int GetRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max + 1);
            }
        }
    }
}
