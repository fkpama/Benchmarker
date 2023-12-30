using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InteropTests.Lib
{
    public abstract class TsBaseClass
    {
        public required TestContext TestContext { get; init; }
        public CancellationToken cancellationToken => TestContext.CancellationTokenSource.Token;

    }
}
