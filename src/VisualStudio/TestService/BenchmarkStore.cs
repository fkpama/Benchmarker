using System.ComponentModel.Composition;
using Benchmarker.Serialization;
using Benchmarker.Storage;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Benchmarker.VisualStudio.TestsService
{
    [Export(typeof(IVsBenchmarkStore))]
    internal sealed class BenchmarkStore : IVsBenchmarkStore
    {
        private readonly IBenchmarkSettingsService settingsService;
        private readonly VisualStudioWorkspace workspace;

        [ImportingConstructor]
        public BenchmarkStore(IBenchmarkSettingsService settingsService,
            [ImportMany]IEnumerable<Lazy<IBenchmarkDataProvider>> providers,
            VisualStudioWorkspace workspace)
        {
            this.settingsService = settingsService;
            this.workspace = workspace;
            var x = providers.Select(x => x.Value).ToArray();
        }

        public async ValueTask<BenchmarkHistory> GetHistoryAsync(VSProjectId projectId, TestId testId, CancellationToken cancellationToken)
        {
            var sln = await ServiceProvider
                .GetGlobalServiceAsync<SVsSolution, IVsSolution>()
                .ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var hier = sln.GetProjectOfGuid(projectId.Value);
            var guid = hier.GetProjectId();
            await TaskScheduler.Default;
            var settings = await this.settingsService
                .GetProjectSettingsAsync(guid, cancellationToken)
                .ConfigureAwait(false);
            var path = settings.HistoryFilePath;
            var storage = new JsonStorage(path);
            var runs = await storage
                .GetAllRunAsync(testId, cancellationToken)
                .ConfigureAwait(false);
            return new BenchmarkHistory();
        }
    }
}
