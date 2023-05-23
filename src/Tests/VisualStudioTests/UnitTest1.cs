using Benchmarker.VisualStudio.TestsService;
using Microsoft.CodeAnalysis.Text;

namespace VisualStudioTests
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }
        [TestMethod]
        public void TestMethod1()
        {
            this.TestContext.WriteLine("Hello World");
            Console.Error.WriteLine("Error ");
            //var span = TextSpan.FromBounds(5, 50);
            //var changeSpan = TextSpan.FromBounds(2, 25);
            //var change = new TextChange(changeSpan, string.Empty);

            //var expected = TextSpan.FromBounds(25, 50);

            //var result = span.MapTo(new[] { change });

            //Assert.AreEqual(expected, result);
        }

        private int getIndex(string text, string v1, int v2)
        {
            int count = 0;
            int start = -1;
            for (var i = 0; i < v2; i++, count++)
            {
                start = text.IndexOf(v1, i);
            }
            return start;
        }
    }
}