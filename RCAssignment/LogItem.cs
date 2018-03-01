using System;

namespace RC.Assignment
{
    /// <summary>
    /// Represents log file data members.
    /// </summary>
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

            bool result = DateTime.Equals(this.LogTime, other.LogTime) && this.Stage == other.Stage;

            return result;
        }

        public override int GetHashCode()
        {
            int result = 397 ^ this.LogTime.GetHashCode() ^ this.Stage.GetHashCode();

            return result;
        }

        #endregion IEquatable
    }
}
