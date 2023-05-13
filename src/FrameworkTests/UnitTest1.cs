using Benchmarker.Engine;
using Benchmarker.MsTests.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestProject;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace FrameworkTests
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }

        Mock<IFrameworkHandle> moq1;
        Mock<IRunContext> moqContext;
        Mock<ITestCaseDiscoverySink> moqSync;
        BenchmarkerExecutor sut;
        readonly List<string> source = new()
        {
            Path.GetFullPath(typeof(Class1).Assembly.Location)
        };

        public UnitTest1()
        {
            this.TestContext = null!;
            this.sut = null!;
            this.moq1 = null!;
            this.moqContext = null!;
            this.moqSync = null!;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            moq1 = new Mock<IFrameworkHandle>();
            moq1.Setup(x => x.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()))
                .Callback((TestMessageLevel lvl, string msg) => Console.WriteLine($"{lvl}: {msg}"));
            moqContext = new Mock<IRunContext>();
            moqSync = new Mock<ITestCaseDiscoverySink>();
            sut = new BenchmarkerExecutor();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var tc = new TestCaseCollection<object>();
            foreach(var item in tc)
            {

            }
        }

        [TestMethod]
        public void TestMethod3()
        {

            var lst = discoverTests();
            var results = getResults();

            Assert.That.Matched(lst, results);
            sut.RunTests(lst, moqContext.Object, moq1.Object);

            Assert.AreEqual(lst.Count, results.Count);
        }

        [TestMethod]
        public void TestMethod2()
        {

            var lst = discoverTests();
            var results = getResults();
            Assert.That.MatchedByName(lst, results);

        }

        private List<TestResult> getResults(bool throwOnFailed = true) => getResults(source, null);
        private List<TestResult> getResults(IEnumerable<string>? sources, bool throwOnFailed = true)
            => getResults(sources, null, throwOnFailed = true);
        private List<TestResult> getResults(IEnumerable<TestCase>? sources, bool throwOnFailed = true)
            => getResults(null, sources, throwOnFailed);
        private List<TestResult> getResults(IEnumerable<string>? sources,
            IEnumerable<TestCase>? tests, bool throwOnFailed = true)
        {
            var results = new List<TestResult>();
            moq1.Setup(x => x.RecordResult(It.IsAny<TestResult>()))
                .Callback((TestResult res) => results.Add(res));

            if ((sources is null && tests is null)
                ||(sources is not null && tests is not null))
            throw new InternalTestFailureException();
            List<TestResult>? failed  = null;
            if (throwOnFailed)
            {
                failed = new();
                this.moq1
                    .Setup(x => x.RecordResult(It.Is<TestResult>(x => x.Outcome == TestOutcome.Failed)))
                    .Callback((TestResult x) => failed.Add(x));
            }
            if (sources is not null)
                sut.RunTests(source, moqContext.Object, moq1.Object);
            else
                sut.RunTests(tests, moqContext.Object, moq1.Object);

            if (failed?.Any() == true)
            {
                Assert.Fail($"Test failed:{Environment.NewLine}{string.Join(Environment.NewLine, failed.Select(x => x.ToString()))}");
            }
            return results;

        }
        private List<TestCase> discoverTests()
        {
            var lst = new List<TestCase>();
            moqSync.Setup(x => x.SendTestCase(It.IsAny<TestCase>()))
                .Callback((TestCase testCase) =>
                {
                    lst.Add(testCase);
                });

            var discoverer = new BenchmarkerDiscoverer();
            discoverer
                .DiscoverTests(source,
                moqContext.Object,
                moq1.Object,
                moqSync.Object);
            return lst;
        }
    }
}