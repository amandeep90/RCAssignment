using System;
using System.Collections.Generic;
using System.IO;

namespace RC.Assignment
{
    /// <summary>
    /// Generates artificial log data to facilitate spot testing.
    /// </summary>
    class TestDataGen
    {
        private int _targetLines;
        private DateTime _startTime;

        /// <summary>
        /// Number of log entries to generate
        /// </summary>
        /// <param name="linesLmt"></param>
        public TestDataGen(int linesLmt)
        {           
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
