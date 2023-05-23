using System.Diagnostics;
using Benchmarker.Engine;
using Benchmarker.MsTests.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;
using MsTests.Common.Marshalling;
using MsTests.Common.Serialization;
using Sodiware;
using TestProject;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace FrameworkTests
{
    [TestClass]
    public class UnitTest1
    {
        static string TestProjectPath
        {
            get
            {
                return Path.GetFullPath(typeof(Class1).Assembly.Location);
            }
        }

        public TestContext TestContext { get; set; }

        Mock<IFrameworkHandle> moq1;
        Mock<IRunContext> moqContext;
        Mock<IRunSettings> moqSettings;
        Mock<ITestCaseDiscoverySink> moqSync;
        BenchmarkerExecutor sut;
        readonly List<string> source = new() { TestProjectPath };

        public UnitTest1()
        {
            this.TestContext = null!;
            this.sut = null!;
            this.moq1 = null!;
            this.moqContext = null!;
            this.moqSync = null!;
            this.moqSettings = null!;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            moq1 = new Mock<IFrameworkHandle>();
            moq1.Setup(x => x.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()))
                .Callback((TestMessageLevel lvl, string msg) => Console.WriteLine($"{lvl}: {msg}"));
            moqContext = new Mock<IRunContext>();
            moqSync = new Mock<ITestCaseDiscoverySink>();
            moqSettings = new();
            moqContext.Setup(x => x.RunSettings).Returns(moqSettings.Object);
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
        public void TestMethod4()
        {
            var settings = new AdapterSettings();
            var method = typeof(Class1).GetInstanceMethod(nameof(Class1.Benchmark__1), System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(method);
            var str = new SignatureFormatter();
            var mstr = str.Format(new MethodInfoWrapper(method));
            settings.IgnoredBenchmarks = new() {
                new()
                {
                    Source = TestProjectPath,
                    Methods = mstr
                }
            };
            //var filter = TestFilterUtils.CreateVSTestFilterExpression("FullyQualifiedName=Hello");
            //moqContext.Setup(x => x.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(),
            //    It.IsAny<Func<string, TestProperty>>()))
            //    .Returns(filter);
            var serializer = AdapterSettings.Serialize(settings);
            var xml = @$"<RunSettings>{serializer}</RunSettings>";
            moqSettings.Setup(x => x.SettingsXml).Returns(xml);
            var lst = discoverTests();
            //var results = getResults();
            Assert.IsTrue(lst.Count > 0);


            var fname = $"{method.DeclaringType!.Namespace}.{method.DeclaringType!.Name}.{method.Name}"; ;
            var found = lst.FirstOrDefault(x => x.FullyQualifiedName.Contains(fname));
            Assert.IsNull(found);
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