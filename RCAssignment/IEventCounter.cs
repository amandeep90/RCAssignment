using System.IO;

namespace RC.Assignment
{
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
}
