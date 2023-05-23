using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;
namespace Benchmarker.VisualStudio.CodeLens
{
    public class RoslyRequestData
    {
        public Guid ProjectId { get; }
        public Guid VSProjectId { get; }
        public string FilePath { get; }
        public Guid DocumentId { get; }
        public TextSpan Span { get; }
        public string? ManagedType { get; }
        public string? ManagedMethod { get; }

        [JsonConstructor]
        public RoslyRequestData(Guid ProjectId,
                                Guid VSProjectId,
                                Guid DocumentId,
                                TextSpan Span,
                                string FilePath,
                                string? ManagedType,
                                string? ManageMethod)
        {
            this.ProjectId = ProjectId;
            this.VSProjectId = VSProjectId;
            this.DocumentId = DocumentId;
            this.ManagedType = ManagedType;
            this.ManagedMethod = ManagedMethod;
            this.Span = Span;
            this.FilePath = FilePath;
        }

    }
    public interface IVSBenchmarkDataProvider
    {
        Task<BenchmarkData?> GetBenchmarkData2Async(Guid projectGuid,
                                                    string filePath,
                                                    int start,
                                                    int end,
                                                    CancellationToken cancellationToken);
        Task<BenchmarkData?> GetBenchmarkDataAsync(RoslyRequestData requestData,
                                                   CancellationToken cancellationToken);
    }
}