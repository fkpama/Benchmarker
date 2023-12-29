using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security;
using System.Text;
using Benchmarker.Storage.DevOps.Serialization;
using Newtonsoft.Json;

namespace Benchmarker.Storage.DevOps
{
    public class BenchmarkerClient : IDisposable
    {
        private bool disposed;
        const string extensionDocumentApiFormat = "_apis/ExtensionManagement/InstalledExtensions/{0}/Contributions/{1}?api-version=6.0-preview.1";
        const string extensionInfoApiFormat = "https://extmgmt.dev.azure.com/{0}/_apis/extensionmanagement/installedextensionsbyname/{1}/{2}?api-version=7.0-preview.1";
        const string extensionListApiFormat = "https://extmgmt.dev.azure.com/{0}/_apis/extensionmanagement/installedextensions?api-version=7.0-preview.1";

        const string JsonMediaType = "application/json";

        const string DefaultApiVersion = "7.1-preview.1";
        private readonly SecureString credentials;
        private readonly string organization;

        internal string ApiVersion { get; } = DefaultApiVersion;
        public string OrganizationName => organization;
        public string Publisher { get; }
        public string DocumentCollection { get; }
        public Uri CollectionUri { get; }
        private string Extension { get; }
        internal string ExtensionBaseUrl
        {
            get => $"https://extmgmt.dev.azure.com/{this.OrganizationName}/";
        }

        public BenchmarkerClient(Uri server,
                                 SecureString patToken,
                                 string publisher,
                                 string organization,
                                 string extension,
                                 string documentCollection)
        {
            this.credentials = Guard.NotNull(patToken);
            this.Publisher = Guard.NotNullOrWhitespace(publisher);
            this.organization = Guard.NotNullOrWhitespace(organization);
            this.Extension = Guard.NotNullOrWhitespace(extension);
            this.DocumentCollection = Guard.NotNullOrWhitespace(documentCollection);
            this.CollectionUri = server.AbsoluteUri.EndsWith("/")
                ? new Uri($"{server}/", UriKind.Absolute)
                : server;
        }

        async Task GetExtensionInfoAsync(HttpClient client, CancellationToken cancellationToken)
        {
            var uri = string.Format(extensionInfoApiFormat,
                                    this.OrganizationName,
                                    this.Publisher,
                                    this.ExtensionBaseUrl);
            var response = await client
                .GetAsync(uri, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        }

        internal async Task ClearCollectionAsync(string collection, CancellationToken cancellationToken = default)
        {
            var docs = await ListDocumentModelsAsync(collection, cancellationToken).NoAwait();
            var lst = new List<Task>();
            foreach(var doc in docs)
            {
                var name = doc.Name;
                Assumes.NotNullOrWhitespace(name);
                lst.Add(Task.Run(async () => await this.DeleteAsync(collection, name, cancellationToken).NoAwait(), cancellationToken));
            }
            await Task.WhenAll(lst).WithCancellation(cancellationToken).NoAwait();
        }
        internal async Task<List<DocumentModel>> ListDocumentModelsAsync(string collection, CancellationToken cancellationToken = default)
        {
            var uri = getCollectionUri(collection);
            var items = await this.getListAsync<DocumentModel>(uri, cancellationToken).NoAwait();
            return items.ToList();
        }
        public async Task<Document[]> ListDocumentsAsync(string collection, CancellationToken cancellationToken)
        {
            var items = await this.ListDocumentModelsAsync(collection, cancellationToken).NoAwait();
            return items.Select(x => new Document(this, collection, x)).ToArray();
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

        //public async Task UploadDocumentAsync(string documentId,
        //                                      string text,
        //                                      CancellationToken cancellationToken)
        //{
        //    using var client = CreateHttpClient(false);

            //var scope = "Current";
            //var scopeType = "Default";
            //var path =  $"{this.ExtensionBaseUrl}_apis/ExtensionManagement/InstalledExtensions/" +
            //    $"{this.Publisher}/{extensionId}/Data/Scopes/{scopeType}/" +
            //    $"{scope}/Collections/{contributionId}/" +
            //    $"Documents/{documentId}" +
            //    $"?api-version=7.1-preview.1";
            //var path = this.getDocumentUri(documentId);
            //using var stream = new MemoryStream();
            //var data = new DocumentModel
            //{
            //    //Name = "Hello",
            //    Value = "World"
            //};
            //var mem = JsonConvert.SerializeObject(data);
            //using var content = new StringContent(mem, Encoding.UTF8, JsonMediaType);
            //var response = await client.PostAsync(path, content, cancellationToken)
            //    .ConfigureAwait(false);

            //response.EnsureSuccessStatusCode();
        //}

        public async Task GetAuditLogAsync(CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();
            var template = $"{this.ExtensionBaseUrl}_apis/ExtensionManagement/" +
                $"AuditLog/{this.Publisher}/{this.ExtensionBaseUrl}" +
                $"?{this.ApiVersion}";
            var response = await client
                .GetAsync(template, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();


            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            File.WriteAllText(@"C:\Temp\result.json", str);

        }
        private string getCollectionDocumentUri(string docCollection, string documentId)
            => getDocumentUri(this.OrganizationName,
                              ScopeType.Collection,
                              this.Publisher,
                              this.Extension,
                              docCollection,
                              documentId);

        public Task<Document> GetDocumentAsync(string documentId,
                                           CancellationToken cancellationToken)
            => GetDocumentAsync(this.DocumentCollection, documentId, cancellationToken);
        public async Task<Document> GetDocumentAsync(string documentCollection,
                                           string documentId,
                                           CancellationToken cancellationToken)
        {
            using var client = CreateHttpClient();

            var path = this.getCollectionDocumentUri(documentCollection, documentId);
            using var stream = new MemoryStream();
            var response = await client
                .GetAsync(path, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    var fname = $"{this.Publisher}/{this.Extension}/" +
                        $"{documentCollection}/{documentId}";
                    throw new FileNotFoundException($"Server File missing: {fname}");
                }
                response.EnsureSuccessStatusCode();
            }

            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var document = JsonConvert.DeserializeObject<DocumentModel>(str);
            Assumes.NotNull(document);
            return new(this, documentCollection, document);
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
        private static string getCollectionUri(string orgName,
                                               ScopeType scope,
                                               string publisherName,
                                               string extensionName,
                                               string documentCollection,
                                               string? apiVersion = null)
        {
            Guard.Debug.NotNullOrWhitespace(orgName);
            Guard.Debug.NotNullOrWhitespace(publisherName);
            Guard.Debug.NotNullOrWhitespace(extensionName);
            Guard.Debug.NotNullOrWhitespace(documentCollection);
            var scopePart = scope switch
            {
                ScopeType.Collection => "Default/Current",
                ScopeType.User => "User/Me",
                _ => throw new NotImplementedException()
            };
            var uri = $"https://extmgmt.dev.azure.com/{orgName}" +
                $"/_apis/ExtensionManagement/InstalledExtensions" +
                $"/{publisherName}/{extensionName}/Data/Scopes/{scopePart}" +
                $"/Collections/{documentCollection}/Documents";
            if (apiVersion.IsPresent())
            {
                uri += $"?api-version={apiVersion}";
            }
            return uri;
        }
        private string getCollectionUri(string documentCollection)
        {
            var collUri = getCollectionUri(this.OrganizationName,
                                           ScopeType.Collection,
                                           this.Publisher,
                                           this.Extension,
                                           documentCollection,
                                           ApiVersion);
            return collUri;
        }
        private static string getDocumentUri(string orgName,
                                             ScopeType scope,
                                             string publisherName,
                                             string extensionName,
                                             string documentCollection,
                                             string documentName,
                                             string? apiVersion = null)
        {
            Guard.Debug.NotNullOrWhitespace(documentName);
            var collUri = getCollectionUri(orgName,
                                           scope,
                                           publisherName,
                                           extensionName,
                                           documentCollection);
            var uri = $"{collUri}/{documentName}?api-version={apiVersion.IfMissing(DefaultApiVersion)}";
            //var uri = $"https://extmgmt.dev.azure.com/{orgName}" +
            //    $"/_apis/ExtensionManagement/InstalledExtensions" +
            //    $"/{publisherName}/{extensionName}/Data/Scopes/{scopePart}" +
            //    $"/Collections/{documentCollection}/Documents" +
            //    $"/{documentName}?api-version={apiVersion.IfMissing(DefaultApiVersion)}";
            return uri;
        }
        private string getDocumentUri(string collection, string documentName)
        {
            return getDocumentUri(this.organization,
                                  ScopeType.Collection,
                                  this.Publisher,
                                  this.Extension,
                                  collection,
                                  documentName,
                                  ApiVersion);
        }

        private async Task<TOutput> createAsync<TData, TOutput>(string uri, TData data, CancellationToken cancellationToken)
            where TOutput : class
        {
            Assumes.NotNull(data);
            var response = await this.postAsync(uri, data, cancellationToken).NoAwait();
            Assumes.True(response.StatusCode == HttpStatusCode.Created);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new NotImplementedException();
            }
            var output = await response.ReadResponseAs<TOutput>(cancellationToken).NoAwait();
            if (output is null)
            {
                throw new NotImplementedException();
            }
            return output;

        }
        private async Task<HttpResponseMessage> deleteAsync(string uri, CancellationToken cancellationToken)
        {
            using var client = this.CreateHttpClient();
            var response = await client.DeleteAsync(uri, cancellationToken).NoAwait();
            response.EnsureSuccessStatusCode();
            return response;
        }
        private async Task<HttpResponseMessage> postAsync<T>(string uri, T data, CancellationToken cancellationToken)
        {
            Assumes.NotNull(data);
            var str = JsonConvert.SerializeObject(data);
            using var client = this.CreateHttpClient();
            using var content = new StringContent(str, Encoding.UTF8, JsonMediaType);
            var response = await client.PostAsync(uri, content, cancellationToken).NoAwait();
            response.EnsureSuccessStatusCode();
            return response;
        }
        private async Task<T[]> getListAsync<T>(string uri, CancellationToken cancellationToken)
            where T : class
        {
            using var client = this.CreateHttpClient();
            var result = await this.getAsync<ArrayModel<T>>(uri, cancellationToken).NoAwait();
            Assumes.NotNull(result);
            return result.Value;
        }
        private async Task<T> getAsync<T>(string uri, CancellationToken cancellationToken)
        {
            using var client = this.CreateHttpClient();
            var response = await client.GetAsync(uri, cancellationToken).NoAwait();
            response.EnsureSuccessStatusCode();
            var str = await response.Content.ReadAsStringAsync().WithCancellation(cancellationToken).NoAwait();
            var result = JsonConvert.DeserializeObject<T>(str);
            Assumes.NotNull(result);
            return result;
        }
        private string getDocumentUri(string documentName)
            => getDocumentUri(this.DocumentCollection, documentName);

        public async Task<Document> CreateDocumentAsync(string documentName,
                                                        string content,
                                                        CancellationToken cancellationToken)
        {
            using var tr = new StringReader(content);
            return await CreateDocumentAsync(documentName, tr, cancellationToken).NoAwait();

        }
        public Task<Document> CreateDocumentAsync(string documentName,
                                                        TextReader tr,
                                                        CancellationToken cancellationToken)
            => CreateDocumentAsync(this.DocumentCollection, documentName, tr, cancellationToken);
        public async Task<Document> CreateDocumentAsync(string collection,
                                                        string documentName,
                                                        TextReader tr,
                                                        CancellationToken cancellationToken)
        {
            var uri = this.getDocumentUri(documentName);
            var str = await tr.ReadToEndAsync().WithCancellation(cancellationToken).NoAwait();
            var model = new DocumentModel
            {
                Name = documentName,
                Value = str.EmptyIfMissing()
            };
            var doc = await this.createAsync<DocumentModel, DocumentModel>(uri, model, cancellationToken).NoAwait();
            return new(this, collection, doc);
        }

        internal async Task DeleteAsync(string collection,
                                  string name,
                                  CancellationToken cancellationToken)
        {
            var uri = this.getDocumentUri(collection, name);
            var response = await this.deleteAsync(uri, cancellationToken).NoAwait();
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new NotImplementedException();
            }
        }

        enum ScopeType
        {
            Collection = 1,
            User = 2
        }

    }
}