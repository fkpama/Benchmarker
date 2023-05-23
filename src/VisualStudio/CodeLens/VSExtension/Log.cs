namespace SwBenchmark.CodeLens
{
    internal class Log
    {
        const string s_fileId = @"C:\Temp\Host.log";
        internal static void Info(string v2)
        {
            File.AppendAllText(s_fileId, $"{v2}\n");
        }

        internal static void Warning(string v)
        {
            File.AppendAllText(s_fileId, $"{v}\n");
        }
    }
}