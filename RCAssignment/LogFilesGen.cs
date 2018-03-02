using System;
using System.Collections.Generic;
using System.IO;

namespace RC.Assignment
{
    /// <summary>
    /// The aim of this class it to facilitate organic testing by generating random test data.
    /// This is an artificial log files generator to build random sample input data for spot testing.
    /// Spits out one log file per device. File name format is Device_<deviceID>.txt.
    /// </summary>
    class LogFilesGen
    {
        private TestDataGen _testDataGen;
        private int _logFilesLmt;

        /// <summary>
        /// Sets up the log files generator instance with required number of log files and log lines per file.
        /// </summary>
        /// <param name="logFilesLmt"> Number of log files to generate. Note: each log file denotes log data for a unique device. </param>
        /// <param name="logLinesLmt"> Number of log entries to generate in a file </param>
        public LogFilesGen(int logFilesLmt, int logLinesLmt)
        {
            if (logFilesLmt > 1000 && logFilesLmt < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(logFilesLmt));
            }

            if (logLinesLmt > 1000 && logLinesLmt < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(logLinesLmt));
            }

            _logFilesLmt = logFilesLmt;
            _testDataGen = new TestDataGen(logLinesLmt);
        }

        /// <summary>
        /// Generates a sample input log files for testing. Files are stored under the "TestData" folder where this app executes.
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
}
