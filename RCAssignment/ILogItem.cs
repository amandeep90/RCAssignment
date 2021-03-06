﻿using System;

namespace RC.Assignment
{
    /// <summary>
    /// Represents log file data members.
    /// </summary>
    public interface ILogItem
    {
        DateTime LogTime { get; set; }

        int Stage { get; set; }
    }
}
