using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;
using Sodiware;

namespace Benchmarker.VisualStudio.TestsService
{
    [Export(typeof(IBenchmarkSettingsService))]
    internal class BenchmarkSettingsService : IBenchmarkSettingsService
    {
        [ImportingConstructor]
        public BenchmarkSettingsService()
        {
            Log.Info("Settings service created");
        }
        public async ValueTask<BenchmarkProjectSettings?> GetProjectSettingsAsync(VSProjectId projectGuid, CancellationToken cancellationToken)
        {
            var solution = await ServiceProvider
                .GetGlobalServiceAsync<SVsSolution, IVsSolution>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var hier = solution.GetProjectOfGuid(projectGuid);
            var cps = hier.AsUnconfiguredProject();
            if (cps is null)
            {
                Log.Warn("Could not get CPS project from hierarchy");
                return null;
            }

            ErrorHandler.ThrowOnFailure(
                hier.GetProperty((uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ProjectDir,
                out var pvar));
            var projectDir = pvar as string;

            await TaskScheduler.Default;

            var properties = cps.Services
                .ActiveConfiguredProjectProvider?
                .ActiveConfiguredProject?
                .Services
                .UserPropertiesProvider?
                .GetCommonProperties();
            if (properties is null)
            {
                // TODO
                throw new NotImplementedException();
            }

            var intermediateOutputPath = await properties
                .GetEvaluatedPropertyValueAsync("IntermediateOutputPath")
                .ConfigureAwait(false);
            //var names = await properties.GetPropertyNamesAsync()
            //    .ConfigureAwait(false);

            var defaultHistoryPath = Path.Combine(projectDir,
                                                  intermediateOutputPath,
                                                  "benchmarks.hist.json");
            return new()
            {
                HistoryFilePath = defaultHistoryPath
            };
        }
    }
}
