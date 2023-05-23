using Microsoft.VisualStudio.Language.CodeLens.Remoting;

namespace Benchmarker.VisualStudio.CodeLens.Interop
{
    internal sealed class HostWrapper : IVSBenchmarkDataProvider
    {
        private readonly Lazy<ICodeLensCallbackService> callbackService;
        private readonly IAsyncCodeLensDataPointProvider dataPointProvider;

        internal ICodeLensCallbackService Service
            => callbackService.Value;

        public HostWrapper(Lazy<ICodeLensCallbackService> callbackService, IAsyncCodeLensDataPointProvider dataPointProvider)
        {
            this.callbackService = callbackService;
            this.dataPointProvider = dataPointProvider;
        }

        public Task<BenchmarkData?> GetBenchmarkData2Async(Guid projectGuid, string filePath, int start, int end, CancellationToken cancellationToken)
        {
            return this.Service
                .InvokeAsync<BenchmarkData?>(this.dataPointProvider,
                nameof(IVSBenchmarkDataProvider.GetBenchmarkData2Async),
                new object[]
                {
                    projectGuid,
                    filePath,
                    start,
                    end
                }, cancellationToken);
        }

        public Task<BenchmarkData?> GetBenchmarkDataAsync(RoslyRequestData request, CancellationToken token)
        {
            return this.Service
                .InvokeAsync<BenchmarkData?>(this.dataPointProvider,
                nameof(IVSBenchmarkDataProvider.GetBenchmarkDataAsync),
                new object[] { request },
                token);
        }
    }
}
