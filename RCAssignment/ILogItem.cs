using System;

namespace RC.Assignment
{
    interface ILogItem
    {
        DateTime LogTime { get; set; }

        int Stage { get; set; }
    }
}
