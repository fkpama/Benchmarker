namespace TestAdapter
{
    internal static class Extensions
    {
        public static void AddOutput(this TestResult testResult, string output)
        {
            testResult.Messages.Add(new(TestResultMessage.StandardOutCategory, output));
        }
        public static void AddError(this TestResult testResult, string output)
        {
            testResult.Messages.Add(new(TestResultMessage.StandardErrorCategory, output));
        }
        public static void AddDebugTrace(this TestResult testResult, string output)
        {
            testResult.Messages.Add(new(TestResultMessage.DebugTraceCategory, output));
        }

        public static void Error(this IFrameworkHandle handle, string message)
            => handle.SendMessage(TestMessageLevel.Error, message);

        public static void Warning(this IFrameworkHandle handle, string message)
            => handle.SendMessage(TestMessageLevel.Warning, message);
        public static void Info(this IFrameworkHandle handle, string message)
            => handle.SendMessage(TestMessageLevel.Informational, message);

    }
}
