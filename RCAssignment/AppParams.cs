namespace RC.Assignment
{
    /// <summary>
    /// Application level constants and properties.
    /// </summary>
    static class AppParams
    {
        /// <summary>
        /// Working directory to be used for this project. Points to the folder where application is being executed.
        /// </summary>
        public static string WorkingDir => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Absolute path to the sample log files directory 
        /// </summary>
        public static string LogFileDir => string.Format("{0}\\TestData", WorkingDir);

        /// <summary>
        /// Denotes unique deviceID per app run.
        /// </summary>
        private static int fileCount = 0;

        /// <summary>
        /// Log file name with absolute path.
        /// </summary>
        public static string LogFileName
        {
            get
            {
                fileCount += 1;

                return string.Format("{0}\\Device_{1}.txt", LogFileDir, fileCount);
            }
        }
    }
}
