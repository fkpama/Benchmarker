using DevOps.Tests;

namespace DevOps.Tests
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
        public void BaseTestInitialize()
        {
            if (Token.IsMissing())
            {
                Token = TestUtils.FindToken();
            }
        }


    }
}