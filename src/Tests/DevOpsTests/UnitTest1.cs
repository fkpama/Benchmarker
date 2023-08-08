
using DevOps.Tests;
using Sodiware;

namespace DevOpsTests
{
    [TestClass]
    public class UnitTest1 : BaseTestClass
    {
        const string projectName = "Tests";
        static readonly Uri uri = new("https://dev.azure.com/kpamafrederic/");

        [TestMethod]
        public async Task TestMethod1()
        {
            using var client = new BenchmarkerClient(uri, Token.ToSecureString());

            //await client
            //    .GetProjectInfoAsync(projectName, cancellationToken)
            //    .ConfigureAwait(false);

            var content = await client.GetDocumentAsync("TestCollection",
                                          "my-document",
                                          CancellationToken.None);
            //await client.GetAuditLogAsync(cancellationToken);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            using var client = new BenchmarkerClient(uri, Token.ToSecureString());

            //await client
            //    .GetProjectInfoAsync(projectName, cancellationToken)
            //    .ConfigureAwait(false);
            //await client.GetAuditLogAsync(cancellationToken);
        }
    }
}