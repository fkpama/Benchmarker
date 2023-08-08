using DevOps.Tests;

namespace DevOpsTests
{
    public abstract class BaseTestClass
    {
        private protected static string Token;
        public TestContext TestContext { get; set; }
        private protected CancellationToken cancellationToken => this.TestContext.CancellationTokenSource.Token;

        static BaseTestClass()
        {
            Token = null!;
        }
        protected BaseTestClass()
        {
            this.TestContext = null!;
        }
        [TestInitialize]
        public void TestInitialize()
        {
            if (Token.IsMissing())
            {
                Token = TestUtils.FindToken();
            }
        }


    }
}