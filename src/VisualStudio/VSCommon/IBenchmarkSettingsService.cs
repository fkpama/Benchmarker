namespace Benchmarker.VisualStudio
{
    public interface IBenchmarkSettingsService
    {
        ValueTask<BenchmarkProjectSettings> GetProjectSettingsAsync(VSProjectId projectGuid, CancellationToken cancellationToken);
    }

    public sealed class BenchmarkProjectSettings
    {
        public string? HistoryFilePath { get; set; }
    }
}