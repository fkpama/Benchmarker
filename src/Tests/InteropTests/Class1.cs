using Benchmarker.Storage;
using Benchmarker.Serialization;
using InteropTests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InteropTests
{
    [TestClass]
    public class Class1 : TsBaseClass
    {
        ScriptBenchmarkHistory sut;

        public Class1()
        {
            this.sut = null!;
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            var logger = new JsLogger();
            sut = await TsCompiler.CreateHistoryAsync(logger, cancellationToken);
        }
        [TestMethod]
        public async Task MyTestMethod()
        {
            using var ms = new StringReader("");
            var storage = new JsonStorage();
            var testId = Guid.NewGuid();
            var name = "Test";
            var detail = new BenchmarkDetail
            {
                FullName = "InteropTest.Test1",
                Id = testId,
                MethodTitle = "Test1",
                Name = name
            };

            storage.Add(detail);
            using var stream = new MemoryStream();
            await storage.SaveAsync(stream, cancellationToken);

            stream.Position = 0;
            using var sr = new StreamReader(stream, leaveOpen: true);
            await sut.OpenAsync(sr, cancellationToken).NoAwait();

            Assert.AreEqual(1, sut.NbTests);
            Console.WriteLine(testId.ToString("D"));

            var test = sut.GetTestById(testId);
            Assert.IsNotNull(test);
            Assert.AreEqual(testId, test.Id);

            test = sut.GetTestById(testId);
            Assert.IsNotNull(test);
            Assert.AreEqual(testId, test.Id);
        }
    }
}