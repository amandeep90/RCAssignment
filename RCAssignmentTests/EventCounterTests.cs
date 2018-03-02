using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RC.Assignment;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace RCAssignmentTests
{
    [TestClass]

    public class EventCounterTests
    {
        private static EventCounter eventCtr = EventCounter.Instance;

        [TestCleanup()]
        public void Cleanup()
        {
            eventCtr.ResetCounter();
        }

        #region Log Ingest Logic

        [TestMethod]
        public void ParseEvents_LogWithEmptyLines_EmptyLinesIgnored()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0),
                    string.Empty,
                    string.Empty,
                    getLogLine("2001-01-01 19:50:00", 1)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_LogWithWhitespaceLines_WhitespacedLinesIgnored()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0),
                    "       ",
                    "       ",
                    getLogLine("2001-01-01 19:50:00", 1)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void ParseEvents_LogWithIncorrectDate_ThrowsException()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("Garbage Date Value", 0)
                }));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void ParseEvents_LogWithBadStageValue_ThrowsException()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:50:00", "Not a number")
                }));
        }

        [TestMethod]
        public void ParseEvents_LogWithOneLine_LogEntryParsed()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_LogWithMultipleLine_LogEntriesParsed()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0),
                    getLogLine("2001-01-01 19:47:00", 1),
                    getLogLine("2001-01-01 19:48:00", 2),
                    getLogLine("2001-01-01 19:49:00", 3),
                    getLogLine("2001-01-01 19:50:00", 0),
                    getLogLine("2001-01-01 19:52:00", 1)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_MultipleDevices_AllDevicesParsed()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0),
                    getLogLine("2001-01-01 19:47:00", 1),
                    getLogLine("2001-01-01 19:48:00", 2)
                }));

            eventCtr.ParseEvents("Device 2", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 0),
                    getLogLine("2001-01-01 19:47:00", 1),
                    getLogLine("2001-01-01 19:48:00", 2)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
            Assert.AreEqual(0, eventCtr.GetEventCount("Device 2"));
        }

        #endregion Log Ingest Logic

        #region Log Data Validation

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseEvents_DeviceIdNull_ThrowsException()
        {
            eventCtr.ParseEvents(null, getEmptyStreamReader());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseEvents_DeviceIdEmpty_ThrowsException()
        {
            eventCtr.ParseEvents("", getEmptyStreamReader());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseEvents_StreamReaderNull_ThrowsException()
        {
            eventCtr.ParseEvents("Device 1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]

        public void ParseEvents_LogWithUnexpectedLowerLmtStage_ThrowsException()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", -1)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]

        public void ParseEvents_LogWithUnexpectedUpperLmtStage_ThrowsException()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:46:00", 4)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_LogWithRepeatedStages_RepeatedStagesIgnored()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 2), // Repeated Stage - ignored
                    getLogLine("2001-01-01 19:52:00", 2), // Repeated Stage - ignored
                    getLogLine("2001-01-01 19:57:00", 3),
                    getLogLine("2001-01-01 19:58:00", 3), // Repeated Stage - ignored
                    getLogLine("2001-01-01 19:59:00", 0)
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_LogWithDuplicateTimes_DuplicateLogTimesIgnored()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:50:00", 3), // Not a duplicate
                    getLogLine("2001-01-01 19:50:00", 3), // Duplicate
                    getLogLine("2001-01-01 19:53:00", 3),
                    getLogLine("2001-01-01 19:54:00", 3),
                    getLogLine("2001-01-01 19:59:00", 0)
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        #endregion Log Data Validation

        #region Faulty Sequence Cases

        [TestMethod]
        public void ParseEvents_FaultWithinThreeTransitions_OneFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_FaultWithinFourTransitions_OneFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 3),
                    getLogLine("2001-01-01 19:52:00", 0)
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_FaultWithinFiveTransitions_OneFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 3),
                    getLogLine("2001-01-01 19:52:00", 2),
                    getLogLine("2001-01-01 19:53:00", 0)
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_FaultWithinOverFiveTransitions_OneFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 3),
                    getLogLine("2001-01-01 19:52:00", 2),
                    getLogLine("2001-01-01 19:53:00", 3),
                    getLogLine("2001-01-01 19:54:00", 2),
                    getLogLine("2001-01-01 19:55:00", 3),
                    getLogLine("2001-01-01 19:56:00", 0),
                }));

            Assert.AreEqual(1, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_LogWithTwoFaults_TwoFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    // First faulty sequence below
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0),
                    // Second faulty sequence below
                    getLogLine("2001-01-01 19:52:00", 3),
                    getLogLine("2001-01-01 19:57:00", 2),
                    getLogLine("2001-01-01 19:58:00", 3),
                    getLogLine("2001-01-01 19:59:00", 0)
                }));

            Assert.AreEqual(2, eventCtr.GetEventCount("Device 1"));
        }

        #endregion Faulty Sequence Cases

        #region Not Faulty Sequence Cases

        [TestMethod]
        public void ParseEvents_NotFaultyFirstTransitionStage_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 1),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultyFirstTransitionDuration_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:49:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultySecondTransitionStage1_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 1),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultySecondTransitionStage3_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 3),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultySecondTransitionStage0_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 0),
                    getLogLine("2001-01-01 19:51:00", 0)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultyThirdTransitionStage1_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 1)
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        [TestMethod]
        public void ParseEvents_NotFaultyFinalTransitionStage1_ZeroFaultSequenceFound()
        {
            eventCtr.ParseEvents("Device 1", GenerateStreamReader(
                new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 2), // Duplicate stage will be ignored.
                    getLogLine("2001-01-01 19:52:00", 3), // Cycle 2 to 3
                    getLogLine("2001-01-01 19:53:00", 2), // Cycle 3 to 2
                    getLogLine("2001-01-01 19:54:00", 3), // Cycle 2 to 3
                    getLogLine("2001-01-01 19:51:00", 1)  // Final stage not 0 - so not faulty
                }));

            Assert.AreEqual(0, eventCtr.GetEventCount("Device 1"));
        }

        #endregion Fault Sequence Detection Logic

        #region Multi Threaded Invokes and Performance

        [TestMethod]
        public void ParseEvents_InvokeFromMultiThreads_IsThreadSafe()
        {
            var faultyLogData = new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0)
                };

            List<Task> logProcessorTasks = new List<Task>();

            for (int deviceCount = 1; deviceCount < 11; deviceCount++)
            {
                var id = string.Format("Device {0}", deviceCount);
                var task = new Task(() => eventCtr.ParseEvents(id, GenerateStreamReader(faultyLogData)));
                logProcessorTasks.Add(task);
            }

            logProcessorTasks.ForEach(x => x.Start());
            Task.WaitAll(logProcessorTasks.ToArray());

            List<Task<int>> lookUpCountTasks = new List<Task<int>>();

            for (int countTask = 0; countTask < 1000; countTask++)
            {
                var id = string.Format("Device {0}", Utilities.GetRandomNumber(1, 10));
                var task = new Task<int>(() => eventCtr.GetEventCount(id));
                lookUpCountTasks.Add(task);
            }

            lookUpCountTasks.ForEach(x => x.Start());
            Task.WaitAll(lookUpCountTasks.ToArray());

            var tasksWith1faultyCount = lookUpCountTasks.Where(t => t.Result == 1).Count();

            Assert.AreEqual(1000, tasksWith1faultyCount);
        }

        [TestMethod]
        [Timeout(1000)]
        public void ParseEvents_Parse1000Logs_PerformsInOneSec()
        {
            var faultyLogData = new string[]
                {
                    getLogLine("2001-01-01 19:45:00", 3),
                    getLogLine("2001-01-01 19:50:00", 2),
                    getLogLine("2001-01-01 19:51:00", 0)
                };

            List<Task> logProcessorTasks = new List<Task>();

            for (int deviceCount = 1; deviceCount < 1001; deviceCount++)
            {
                var id = string.Format("Device {0}", deviceCount);
                var task = new Task(() => eventCtr.ParseEvents(id, GenerateStreamReader(faultyLogData)));
                logProcessorTasks.Add(task);
            }

            logProcessorTasks.ForEach(x => x.Start());

            Task.WaitAll(logProcessorTasks.ToArray());
        }

        #endregion Multi Threaded Invokes and Performance

        #region Helper Methods 

        private string getLogLine(string logTime, int stage)
        {
            return string.Format("{0}\t{1}", logTime, stage);
        }

        private string getLogLine(string logTime, string stage)
        {
            return string.Format("{0}\t{1}", logTime, stage);
        }

        private StreamReader getEmptyStreamReader()
        {
            return GenerateStreamReader(new List<string>().ToArray());
        }

        public StreamReader GenerateStreamReader(string[] lines)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
            writer.Flush();
            stream.Position = 0;

            return new StreamReader(stream);
        }

        #endregion Helper Methods
    }
}
