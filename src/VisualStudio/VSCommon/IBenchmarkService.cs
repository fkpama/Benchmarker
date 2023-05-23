using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarker.VisualStudio
{
    public interface IBenchmarkService
    {
        IEnumerable<IBenchmarkProject> ActiveProjects { get; }

        void Advise(IBenchmarkListener listener);
        void Unadvise(IBenchmarkListener listener);

        Task<BenchmarkMetadataInfo?> GetInfoAsync(VSProjectId projectGuid,
                                                  string filePath,
                                                  TextSpan span,
                                                  CancellationToken cancellationToken);
        Task<BenchmarkMetadataInfo?> GetInfoAsync(ProjectId projectGuid,
                                                  DocumentId docId,
                                                  TextSpan span,
                                                  CancellationToken cancellationToken);
    }
}