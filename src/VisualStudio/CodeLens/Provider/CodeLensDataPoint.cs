using CodeLensModels;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Threading;

namespace Benchmarker.VisualStudio.CodeLens.Provider
{
    internal class CodeLensDataPoint : IAsyncCodeLensDataPoint
    {
        public CodeLensDescriptor Descriptor { get; }
        public BenchmarkMethodTracker Tracker { get; }

        public BenchmarkData Infos
        {
            get => this.Tracker.Infos;
        }

        public event AsyncEventHandler? InvalidatedAsync;

        public CodeLensDataPoint(CodeLensDescriptor descriptor,
                                 BenchmarkMethodTracker infos)
        {
            this.Descriptor = descriptor;
            this.Tracker = infos;
        }

        public Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext descriptorContext, CancellationToken token)
        {
            var dataDesc = new CodeLensDataPointDescriptor
            {
                Description = "Shows up inline",
                TooltipText = "Show Up On Hover",
                
            };
            return Task.FromResult(dataDesc);
        }

        public Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext descriptorContext, CancellationToken token)
        {
            var cls = new BenchmarkCodeLensData
            {
                MetadataInfo = this.Tracker.Infos
            };
            var entry = new CodeLensDetailEntryDescriptor
            {
                Tooltip = "Entry",
            };
            var cmd = new CodeLensDetailPaneCommand
            {
                CommandDisplayName = "Go to method",
                RequiredSelectionCount = 1,
            };
            var detail = new CodeLensDetailsDescriptor
            {
                CustomData = new[] { cls },
                //SelectionMode = CodeLensDetailEntriesSelectionMode.Single,
                Entries = new[]{ entry },
                PaneNavigationCommands = new[] { cmd }
            };

            return Task.FromResult(detail);
        }
    }
}