using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RC.Assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            //Utilities.CleanUpDir(AppParams.LogFileDir);

            //LogFilesGen testLogFileGen = new LogFilesGen(logFilesLmt: 10, logLinesLmt: 50);
            //testLogFileGen.GenerateLogFiles();

            List<Task> logProcessorTasks = new List<Task>();

            DirectoryInfo logDirInfo = new DirectoryInfo(AppParams.LogFileDir);

            foreach (FileInfo fileInfo in logDirInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            {
                //ProcessLogFile(fileInfo);
                var task = new Task(() => ProcessLogFile(fileInfo));
                task.Start();
                logProcessorTasks.Add(task);
            }

            Task.WaitAll(logProcessorTasks.ToArray());

            //Utilities.CleanUpDir(AppParams.LogFileDir);
        }

        static void ProcessLogFile(FileInfo fileInfo)
        {
            string pattern = @"^device_(\d+).txt";
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

    class EventCounter : IEventCounter
    {
        // Make class singleton since it doesn't preserve any instance state. Having a single instance class is easier to unit test if mocking is neeeded.
        private static readonly EventCounter _instance = new EventCounter();
        private static ConcurrentDictionary<string, int> _faultyEventsByDeviceId = new ConcurrentDictionary<string, int>();

        // Hide this constructor to enforce singleton pattern.
        private EventCounter()
        {

        }

        public static EventCounter Instance => _instance;

        public int GetEventCount(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
            {
                throw new ArgumentNullException(nameof(deviceID));
            }

            int eventCount = 0;

            if (_faultyEventsByDeviceId.TryGetValue(deviceID, out eventCount))
            {
                return eventCount;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(deviceID));

                //return eventCount;
            }
        }

        public void ParseEvents(string deviceID, StreamReader eventLog)
        {
            if (string.IsNullOrEmpty(deviceID))
            {
                throw new ArgumentNullException(nameof(deviceID));
            }

            if (eventLog == null)
            {
                throw new ArgumentNullException(nameof(eventLog));
            }

            string line;
            ISet<string> rawData = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while ((line = eventLog.ReadLine()) != null)
            {
                rawData.Add(line);
            }

            eventLog.Close();
            eventLog.Dispose();

            IEnumerable<ILogItem> parsedData = Utilities.ParseLogData(rawData.ToArray());

            _faultyEventsByDeviceId.TryAdd(deviceID, 0);

            SequenceValidator sequenceValidator = new SequenceValidator();

            foreach (var logItem in parsedData)
            {
                var result = sequenceValidator.IsValidSequence(logItem.Stage, logItem.LogTime);
                if (!result)
                {
                    // Faulty sequence detected

                    _faultyEventsByDeviceId.AddOrUpdate(deviceID, 1, (k, v) => v + 1);
                }
            }
        }

        class SequenceValidator
        {
            int? _lastStage = null;
            DateTime? _lastTime;

            /// <summary>
            /// Computes if transition from one stage to another is valid. 
            /// </summary>
            /// <param name="currentStage">Stage value between 0 to 3</param>
            /// <param name="currentTime">Log time in ISO-8601 format</param>
            /// <returns>True if not a faulty sequence. False if faulty sequence is detected.</returns>
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
                    // Requirement : any number of cycles between stage 2 and 3 for any duration. It doesn't explicitly say if any means at least 1 or more cycles.
                    // Based on this statement, if n = "any number of cycles" then assuming n >=0. That means we may have zero cycles or we may have up to n cycles between 2 and 3.
                    
                    // It means the following sequence should be deemed faulty:
                    //2001 - 01 - 01 22:24:00 3
                    //2001 - 01 - 01 22:29:00 2
                    //2001 - 01 - 01 22:37:00 0

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

    static class AppParams
    {
        /// <summary>
        /// Working directory to be used for this project. Points to the folder where application is being executed.
        /// </summary>
        public static string WorkingDir => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string LogFileDir => string.Format("{0}\\TestData", WorkingDir);

        private static int fileCount = 0;

        public static string LogFileName
        {
            get
            {
                fileCount += 1;

                return string.Format("{0}\\Device_{1}.txt", LogFileDir, fileCount);
            }
        }
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

        /// <returns></returns>
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

            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
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
    }

    class LogFilesGen
    {
        private TestDataGen _testDataGen;
        private int _logFilesLmt;

        public LogFilesGen(int logFilesLmt, int logLinesLmt)
        {
            if (logFilesLmt > 1000 && logFilesLmt < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(logFilesLmt));
            }

            if (logLinesLmt > 100 && logLinesLmt < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(logLinesLmt));
            }

            _logFilesLmt = logFilesLmt;
            _testDataGen = new TestDataGen(logLinesLmt);
        }

        /// <summary>
        /// Generates a sample input log file for testing. File is stored under the "TestData" folder where app executes.
        /// </summary>
        public void GenerateLogFiles()
        {
            if (!Directory.Exists(AppParams.LogFileDir))
            {
                Directory.CreateDirectory(AppParams.LogFileDir);
            }

            for (int i = 0; i < _logFilesLmt; i++)
            {
                IList<string> logLines = _testDataGen.GenerateData();

                using (StreamWriter file = new StreamWriter(AppParams.LogFileName, false))
                {
                    foreach (var line in logLines)
                    {
                        file.WriteLine(line);
                    }
                }
            }
        }
    }

    class TestDataGen
    {
        private int _targetLines;
        private DateTime _startTime;

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
            _startTime = _startTime.AddMinutes(Utilities.GetRandomNumber(1, 10));
            string line = string.Format("{0}\t{1}", _startTime.ToString("yyyy-MM-dd HH:mm:ss"), Utilities.GetRandomNumber(0, 3));

            return line;
        }


    }
}
