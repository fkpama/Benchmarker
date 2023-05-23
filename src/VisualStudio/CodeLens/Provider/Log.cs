namespace Benchmarker.VisualStudio.CodeLens
{
    internal class Log
    {
        internal static void LogError(string v)
        {
            File.AppendAllText($"C:\\Temp\\Error.log", $"{v}\n");
        }

        internal static void WriteLine(string v)
        {
            File.AppendAllText($"C:\\Temp\\Info.log", $"{v}\n");
        }
    }
}