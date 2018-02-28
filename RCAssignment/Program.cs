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
            //TestLogFileGen testLogFileGen = new TestLogFileGen(AppConfig.WorkingDir, 10);
            //testLogFileGen.GenerateLogFile();

            TestLogFileReader testLogFileReader = new TestLogFileReader(AppConfig.WorkingDir);
            string[] rawData = testLogFileReader.GetLogLines();

            IEnumerable<ILogItem> parsedData = Utilities.ParseLogData(rawData);

            SequenceValidator sequenceValidator = new SequenceValidator();
            foreach (var logItem in parsedData)
            {
                var result = sequenceValidator.IsValidSequence(logItem.Stage, logItem.LogTime);
                if (!result)
                {
                    Console.WriteLine(result);
                }
            }

        }
    }

    interface IEventCounter
    {
        /// <summary>
        /// Parse and accumulate event information from the given log data.
        /// </summary>
        /// <param name="deviceID">ID of the device that the log is associated with (ex: "HV1")</param>
        /// <param name="eventLog">A stream of lines representing time/value recordings.</param>
        void ParseEvents(string deviceID, StreamReader eventLog);

        /// <summary>
        /// Gets the current count of events detected for the given device
        /// </summary>
        /// <returns>An integer representing the number of detected events</returns>
        int GetEventCount(string deviceID);
    }
    

    interface ILogItem
    {
        DateTime LogTime { get; set; }

        int Stage { get; set; }
    }

    class LogItem : ILogItem, IEquatable<LogItem>
    {
        public DateTime LogTime { get; set; }

        public int Stage { get; set; }

        #region IEquatable

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((LogItem)obj);
        }

        public bool Equals(LogItem other)
        {
            if (other == null)
            {
                return false;
            }

            bool result = DateTime.Equals(this.LogTime, other.LogTime);

            return result;
        }

        public override int GetHashCode()
        {
            int result = 397 ^ this.LogTime.GetHashCode();

            return result;
        }

        #endregion IEquatable
    }

    static class AppConfig
    {
        /// <summary>
        /// Working directory to be used for this project. Points to the folder where application is being executed.
        /// </summary>
        public static string WorkingDir => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string SampleLogFileDir => "TestData";

        public static string SampleLogFileName => "SampleInput.txt";

    }

    static class Utilities
    {
        /// <summary>
        /// Parses log data, line by line, stored in the log file format. Example "2011-03-07 06:25:32 2"
        /// </summary>
        /// <param name="data">Array of string where each element represents a log line</param>
        /// <returns>Senitized and parsed list of ILogItem ordered by asc log time</returns>
        public static IEnumerable<ILogItem> ParseLogData(string[] data)
        {
            IList<ILogItem> logItems = new List<ILogItem>();

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
    }

    class SequenceValidator
    {
        int? _lastStage = null;
        DateTime? _lastTime;

        public bool IsValidSequence(int currentStage, DateTime currentTime)
        {
            if (currentStage < 0 || currentStage > 3)
            {
                throw new InvalidDataException("Unsupported stage value found while parsing log entries");
            }

            if (!_lastStage.HasValue)
            {
                if (currentStage == 3)
                {
                    // First transition from nothing to 3.

                    _lastStage = currentStage;
                    _lastTime = currentTime;
                }

                return true;
            }

            if (_lastStage == currentStage)
            {
                // Duplicated data. Ignore it.

                return true;
            }

            if (_lastStage == 3 && currentStage == 2 && _lastTime.HasValue && (currentTime - _lastTime.Value).TotalMinutes >= 5)
            {
                // Second transition from 3 to 2 after 5 or more mins. 

                _lastStage = 2;
                _lastTime = null; // Once this is null, following 3 to 2 transitions does not need to check duration. 

                return true;
            }

            if (_lastStage.HasValue && !_lastTime.HasValue) // We're passed the second transition. Now we are only concerned with where we go next. 1 = reset and 0 = fault.
            {
                if (currentStage == 0)
                {
                    // Fault sequence reached. Return false. 

                    _lastStage = null;

                    return false;
                }

                if (currentStage == 2 || currentStage == 3)
                {
                    // Simply move to next log item.

                    return true;
                }
            }

            _lastStage = null; // Next fault sequence not found. Reset.
            _lastTime = null;

            return true;
        }
    }

    class TestLogFileReader
    {
        private string _rootPath;

        public TestLogFileReader(string rootPath)
        {
            _rootPath = rootPath;
        }

        /// <summary>
        /// Gets all the log lines contained in the sample input file for testing.
        /// </summary>
        /// <returns></returns>
        public string[] GetLogLines()
        {

            string logFileDir = string.Format("{0}\\{1}", _rootPath, AppConfig.SampleLogFileDir);
            string logFileName = string.Format("{0}\\{1}", logFileDir, AppConfig.SampleLogFileName);

            if (!File.Exists(logFileName))
            {
                throw new FileNotFoundException("Couldn't locate log file at " + logFileName);
            }

            string[] readText = File.ReadAllLines(logFileName);

            return readText;
        }
    }

    class TestLogFileGen
    {
        private string _rootPath;
        private TestDataGen _testDataGen;

        public TestLogFileGen(string rootPath, int logLinesLmt)
        {
            _rootPath = rootPath;
            _testDataGen = new TestDataGen(logLinesLmt);
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

            string logFileDir = string.Format("{0}\\{1}", _rootPath, AppConfig.SampleLogFileDir);
            string logFileName = string.Format("{0}\\{1}", logFileDir, AppConfig.SampleLogFileName);

            if (!Directory.Exists(logFileDir))
            {
                Directory.CreateDirectory(logFileDir);
            }

            IList<string> logLines = _testDataGen.GenerateData();

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
                logLines.Add(getSampleLog());
            }

            return logLines;
        }

        /// <summary>
        /// Generates a sample input data line for artificial log file.
        /// </summary>
        /// <returns> A line for log in this format: "1998-03-07 06:25:32	2" </returns>
        private string getSampleLog()
        {
            _startTime = _startTime.AddMinutes(getRandomNumber(1, 10));
            string line = string.Format("{0}\t{1}", _startTime.ToString("yyyy-MM-dd HH:mm:ss"), getRandomNumber(0, 3));

            return line;
        }

        /// <summary>
        /// Generates a random number between the specified range.
        /// </summary>
        /// <param name="min"> Inclusive minimum bound for return value </param>
        /// <param name="max"> Inclusive maximum bound for return value </param>
        /// <returns> A random value within specified range </returns>
        private int getRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max + 1);
            }
        }
    }
}
