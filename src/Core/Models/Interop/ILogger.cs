namespace Benchmarker.Interop
{
    internal interface ILogger
    {
        void debug(string message, params object[] args);
        void info(string message, params object[] args);
        void warn(string message, params object[] args);
        void verbose(string message, params object[] args);
        void error(string message, params object[] args);
        void trace(string message, params object[] args);
        void command(string message, params object[] args);
    }
}
