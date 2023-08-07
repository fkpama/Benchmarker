using Benchmarker;
using Benchmarker.Engine;
namespace FrameworkTests
{
    [TestClass]
    public class EngineUtilsTests
    {
        [TestMethod]
        [DataRow(nameof(TestDatas.StackTrace2), nameof(TestDatas.SanitizedStackTrace2))]
        [DataRow(nameof(TestDatas.StackTrace), nameof(TestDatas.SanitizedStackTrace))]
        public void EngineUtils__can_sanitize_stack_trace(string stackTrace, string expected)
        {
            var s1 = (string)typeof(TestDatas).GetField(stackTrace)!.GetRawConstantValue()!;
            var s2 = (string)typeof(TestDatas).GetField(expected)!.GetRawConstantValue()!;
            var str = EngineUtils.SanitizeStackTrace(s1);
            if (!string.Equals(s2, str))
                throw new AssertFailedException("Lines are not equal");
        }
    }
}
