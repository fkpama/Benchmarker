using System.ComponentModel.Composition;
using Benchmarker.VisualStudio;
using Benchmarker.VisualStudio.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace SwBenchmark.CodeLens
{
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType("CSharp")]
    internal sealed class CodeLensCallbackListener
        : IVSBenchmarkDataProvider,
        ICodeLensCallbackListener
    {
        private readonly IBenchmarkService benchmarkService;
        private readonly IVsBenchmarkStore store;
        //private readonly VisualStudioWorkspace workspace;
        private readonly IServiceProvider services;

        [ImportingConstructor]
        public CodeLensCallbackListener(
            IBenchmarkService benchmarkService,
            IVsBenchmarkStore store,
            //VisualStudioWorkspace workspace,
            [Import(typeof(SVsServiceProvider))]IServiceProvider service)
        {
            Log.Info($"{nameof(CodeLensCallbackListener)} Loaded\n");
            this.benchmarkService = benchmarkService;
            this.store = store;
            //this.workspace = workspace;
            this.services = service;
            this.initImages();
        }

        private void initImages()
        {
            var svc = this.services.GetService<SVsImageService, IVsImageService2>();
            if (svc is null)
            {
                // TODO: Log
                Log.Warning("Could not get the image service to register images");
                return;
            }

            //KnownMonikers.wa
            //var moniker = new ImageMoniker
            //{
            //    Guid = Guid.Empty,
            //    Id = 1
            //};
        }

        public async Task<BenchmarkData?> GetBenchmarkData2Async(
            Guid projectGuid,
            string filePath,
            int start,
            int end,
            CancellationToken cancellationToken)
        {
            var infos = await this.benchmarkService
                .GetInfoAsync(projectGuid,
                              filePath,
                              new(start, end - start),
                              cancellationToken)
                .ConfigureAwait(false);


            if (!infos.HasValue)
                return null;

            var history = await this.store
                .GetHistoryAsync(projectGuid, infos.Value.TestId, cancellationToken)
                .ConfigureAwait(false);
            return new()
            {
                Metadata = infos.Value,
                History = history
            };
        }


        public async Task<BenchmarkData?> GetBenchmarkDataAsync(RoslyRequestData requestData, CancellationToken cancellationToken)
        {
            //var projectId = ProjectId.CreateFromSerialized(requestData.ProjectId);
            //var documentId = DocumentId.CreateFromSerialized(projectId, requestData.DocumentId);
            var metadata = await this.benchmarkService
                .GetInfoAsync(requestData.VSProjectId,
                              requestData.FilePath,
                              requestData.Span,
                              cancellationToken)
                .ConfigureAwait(false);

            if (metadata is null)
                return null;

            return await this.GetBenchmarkDataAsync(requestData.VSProjectId,
                                                    metadata.Value,
                                                    cancellationToken)
                .ConfigureAwait(false);
        }
        private async Task<BenchmarkData?> GetBenchmarkDataAsync(
            VSProjectId projectId,
            BenchmarkMetadataInfo infos,
            CancellationToken cancellationToken)
        {
            var sln = await ServiceProvider
                .GetGlobalServiceAsync<SVsSolution, IVsSolution>()
                .ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory
                .SwitchToMainThreadAsync(cancellationToken);
            var history = await this.store
                .GetHistoryAsync(projectId, infos.TestId, cancellationToken)
                .ConfigureAwait(false);

            return new()
            {
                Metadata = infos,
                History = history
            };
        }

    }
}
