namespace Benchmarker.VisualStudio.TestsService
{
    internal static class Log
    {
        const string s_filePath = @"C:\Temp\TestService.log";
        internal static void Error(string v, Exception? ex = null)
        {
            lock (s_filePath)
            {
                write("   ERROR", v);
                if (ex is not null)
                    write(ex.ToString());
            }
        }

        internal static void Info(string v)
        {
            write("    INFO", v);
        }

        internal static void Debug(string v)
        {
            write("   DEBUG", v);
        }

        internal static void Warn(string v)
        {
            write("    WARN", v);
        }

        internal static void Verbose(string v)
        {
            write("VERBOSE", v);
        }

        private static void write(string msg)
        {
            lock (s_filePath)
                File.AppendAllText(s_filePath, msg);
        }

        private static void write(string level, string msg)
        {
            write($"[{level}] {msg}\n");
        }
    }
}