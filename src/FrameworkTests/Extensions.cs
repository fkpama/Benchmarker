#pragma warning disable IDE0060 // Remove unused parameter
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Sodiware;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace FrameworkTests
{
    class TestFullnameEqualityComparer : IEqualityComparer<TestCase>
    {
        private static WeakReference<TestFullnameEqualityComparer>? s_Instance;
        internal static TestFullnameEqualityComparer Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        public bool Equals(TestCase? x, TestCase? y)
        {
            if (x == null && y == null) return false;
            else if (x == null || y == null) return false;
            return x.FullyQualifiedName.EqualsOrd(y.FullyQualifiedName);
        }

        public int GetHashCode(TestCase obj)
        {
            return obj?.FullyQualifiedName.GetHashCode() ?? 0;
        }
    }
    internal static class Extensions
    {
        internal static void MatchedByName(
            this Assert assert,
            IEnumerable<TestCase> testCases,
            IEnumerable<TestResult> resultss)
            => Matched(assert, testCases, resultss, TestFullnameEqualityComparer.Instance);
        internal static void Matched(
            this Assert assert,
            IEnumerable<TestCase> testCases,
            IEnumerable<TestResult> resultss)
            => Matched(assert, testCases, resultss, EqualityComparer<TestCase>.Default);
        internal static void Matched(
            this Assert assert,
            IEnumerable<TestCase> testCases,
            IEnumerable<TestResult> resultss,
            IEqualityComparer<TestCase> comparer)
        {
            var lst = new List<TestCase>(testCases);
            var results = new List<TestResult>(resultss);
            Assert.AreEqual(lst.Count, results.Count);

            var res2 = new HashSet<TestCase>(comparer);
            results.ForEach(x => res2.Remove(x.TestCase));
            Assert.AreEqual(0, res2.Count);
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
