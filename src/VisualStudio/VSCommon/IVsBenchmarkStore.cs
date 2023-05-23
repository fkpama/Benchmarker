using Benchmarker.Serialization;

namespace Benchmarker.VisualStudio
{
    public interface IVsBenchmarkStore
    {
        ValueTask<BenchmarkHistory> GetHistoryAsync(
            VSProjectId projectId,
            TestId testId,
            CancellationToken cancellationToken);
    }
}