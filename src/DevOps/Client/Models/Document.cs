using Benchmarker.Storage.DevOps.Serialization;

namespace Benchmarker.Storage.DevOps
{
    public sealed class Document
    {
        public string Name { get; }

        private int? etag;
        private BenchmarkerClient client;
        private readonly string collection;

        internal Document(BenchmarkerClient client,
                          string collection,
                          DocumentModel model)
        {
            if (model.Name.IsMissing())
            {
                throw new NotImplementedException();
            }
            Assumes.NotNullOrWhitespace(model.Name);
            this.Name = model.Name;
            this.etag = model.Etag;
            this.client = client;
            this.collection = collection;
        }

        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            var doc = new DocumentModel
            {
                Name = this.Name,
                Etag = this.etag,
            };
            await this.client
                .DeleteAsync(this.collection, this.Name, cancellationToken)
                .NoAwait();
        }
    }
}
