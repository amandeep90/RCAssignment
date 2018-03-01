using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RC.Assignment
{
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
}
