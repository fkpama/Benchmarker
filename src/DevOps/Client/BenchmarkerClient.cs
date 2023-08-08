using System.Net.Http;
using System.Security;
using System.Text;
using Benchmarker.Storage.DevOps.Serialization;
using Newtonsoft.Json;

namespace Benchmarker.Storage.DevOps
{
    public class BenchmarkerClient : IDisposable
    {
        private bool disposed;
        const string extensionDocumentApiFormat = "_apis/ExtensionManagement/InstalledExtensions/{0}/Contributions/{1}?api-version=6.0-preview.1"; const string extensionInfoApiFormat = "https://extmgmt.dev.azure.com/{0}/_apis/extensionmanagement/installedextensionsbyname/{1}/{2}?api-version=7.0-preview.1";
        const string extensionListApiFormat = "https://extmgmt.dev.azure.com/{0}/_apis/extensionmanagement/installedextensions?api-version=7.0-preview.1";
        const string publisherName = "frederic-kpama",
            extensionName = "sw-benchmarker-storage";

        internal string ApiVersion { get; } = "7.1-preview.1";
        public string OrganizationName { get; } = "kpamafrederic";

        public BenchmarkerClient(Uri server, SecureString patToken)
        {
            this.CollectionUri = server;
            this.credentials = patToken;
            this.CollectionUri = !server.AbsoluteUri.EndsWith("/")
                ? new Uri($"{server}/", UriKind.Absolute)
                : server;
        }

        public Uri CollectionUri { get; }
        internal string ExtensionBaseUrl
        {
            get => $"https://extmgmt.dev.azure.com/{this.OrganizationName}/";
        }

        private readonly SecureString credentials;

        async Task GetExtensionInfoAsync(HttpClient client, CancellationToken cancellationToken)
        {
            var uri = string.Format(extensionInfoApiFormat, this.OrganizationName, publisherName, extensionName);
            var response = await client
                .GetAsync(uri, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        }

        public async Task GetProjectInfoAsync(string projectName, CancellationToken cancellationToken)
        {
            var url =$"_apis/projects/{projectName}?api-version=7.0";
            using var client = CreateHttpClient();
            await this.GetExtensionInfoAsync(client, cancellationToken);
            var response = await client
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task UploadDocumentAsync(
            string projectName,
            string extensionId,
            string contributionId,
            string documentId,
            string text,
            CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient(false);

            var scope = "Current";
            var scopeType = "Default";
            var path =  $"{this.ExtensionBaseUrl}_apis/ExtensionManagement/InstalledExtensions/{publisherName}/{extensionId}/Data/Scopes/{scopeType}/{scope}/Collections/{contributionId}/Documents/{documentId}?api-version=7.1-preview.1";
            using var stream = new MemoryStream();
            var data = new DocumentModel
            {
                //Name = "Hello",
                Value = "World"
            };
            var mem = JsonConvert.SerializeObject(data);
            using var content = new StringContent(mem, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(path, content, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
        }

        public async Task GetAuditLogAsync(CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();
            var template = $"{this.ExtensionBaseUrl}_apis/ExtensionManagement/AuditLog/{publisherName}/{extensionName}?{this.ApiVersion}";
            var response = await client
                .GetAsync(template, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();


            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            File.WriteAllText(@"C:\Temp\result.json", str);

        }
        private string getCollectionDocumentUri(string docCollection, string documentId)
            => getDocumentUri(this.OrganizationName,
                              ScopeType.Collection,
                              publisherName,
                              extensionName,
                              docCollection,
                              documentId);
        public async Task<byte[]> GetDocumentAsync(string documentCollection,
                                           string documentId,
                                           CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();

            var path = this.getCollectionDocumentUri(documentCollection, documentId);
            using var stream = new MemoryStream();
            var response = await client
                .GetAsync(path, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return null!;
        }

        private HttpClient CreateHttpClient(bool useBaseAddress = true)
        {
            // Set the PAT token as the authorization header
            var bts = Encoding.ASCII.GetBytes($":{this.credentials.ToManagedString()}");
            var client = new HttpClient();
            if (useBaseAddress)
            {
                client.BaseAddress = this.CollectionUri;
            }
            client.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(bts));
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            return client;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            this.disposed = true;
            if (disposing)
            {
                //this.client?.Dispose();
            }
        }
        private static string getDocumentUri(string orgName,
                                      ScopeType scope,
                                      string publisherName,
                                      string extensionName,
                                      string documentCollection,
                                      string documentName,
                                      string? apiVersion = null)
        {
            var scopePart = scope switch
            {
                ScopeType.Collection => "Default/Current",
                ScopeType.User => "User/Me",
                _ => throw new NotImplementedException()
            };
            var uri = $"https://extmgmt.dev.azure.com/{orgName}" +
                $"/_apis/ExtensionManagement/InstalledExtensions" +
                $"/{publisherName}/{extensionName}/Data/Scopes/{scopePart}" +
                $"/Collections/{documentCollection}/Documents" +
                $"/{documentName}?api-version={apiVersion}";
            return uri;
        }

        public Task<object> GetCommitAsync(string projectName,
                                           string buildName,
                                           CancellationToken none)
        {
            using var client = CreateHttpClient();
            return null;
        }

        enum ScopeType
        {
            Collection = 1,
            User = 2
        }

    }
}