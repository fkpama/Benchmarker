namespace DevOps.Tests
{
    [TestClass]
    public class UnitTest1 : BaseTestClass
    {
        const string projectName = "Tests";
        const string organization = "kpamafrederic";
        const string collection = "TestCollection";
        const string extension = "sw-benchmarker-dev";
        const string publisher = "Frederic-Kpama";
        static readonly Uri uri = new($"https://dev.azure.com/{organization}/");

        BenchmarkerClient client;

        [TestInitialize]
        public void TestInitialize()
        {
            client = new(uri,
                         Token.ToSecureString(),
                         publisher,
                         organization,
                         extension,
                         collection);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            client?.SafeDispose();
        }
        public UnitTest1()
        {
            client = null!;
        }

        [TestMethod]
        public async Task ExtensionStorageClient__throws_FileNotFoundException_if_document_does_not_exists()
        {
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
            {
                _ = await client.GetDocumentAsync(collection, "does_not_exists", cancellationToken).NoAwait();
            });
            //await client.GetAuditLogAsync(cancellationToken);
        }

        [TestMethod]
        public async Task ExtensionStorageClient__can_clear_collection()
        {
            await client.ClearCollectionAsync(collection, cancellationToken).NoAwait();
        }

        [TestMethod]
        public async Task TestMethod1()
        {
            var documentName = Guid.NewGuid().ToString("N").Substring(0, 8);
            var doc = await client.CreateDocumentAsync(documentName, "Hello world", cancellationToken);

            var allDocs = await client.ListDocumentModelsAsync(collection, cancellationToken).NoAwait();

            await doc.DeleteAsync(this.cancellationToken);
            //await client.GetAuditLogAsync(cancellationToken);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
        }
    }
}