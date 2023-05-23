using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace Benchmarker.VisualStudio.TestsService
{
    internal class BenchmarkProject : IBenchmarkProject,
        IVsUpdateSolutionEvents,
        IVsBuildStatusCallback
    {
        sealed class ProjectBuildContext
        {
            private readonly List<TrackerOperation> operations;
            public IEnumerable<TrackerOperation> Operations
                => this.operations;

            public ProjectBuildContext(List<TrackerOperation> operations)
            {
                this.operations = operations;
            }

            internal void Release()
            {
                s_operationsPool.Return(this.operations);
            }
        }
        public ProjectId Id { get => this.Project.Id; }
        public Project Project { get; }

        private readonly object sync = new();
        private uint buildStatusCookie;
        private uint solutionUpdateCookie;
        private ProjectBuildContext? buildContext;
        private readonly static ObjectPool<List<TrackerOperation>> s_operationsPool
            = ObjectPool.Create<List<TrackerOperation>>();

        public event EventHandler? TestChanged;

        internal bool IsInitialized
        {
            get => this.buildStatusCookie > 0;
        }

        public Project CurrentVersion
        {
            get
            {
                var project = this.Project
                .Solution
                .Workspace
                .CurrentSolution
                .GetProject(this.Id);
                return project is null
                    ? throw new InvalidOperationException("Project disappeared")
                    : project;
            }
        }

        string IBenchmarkProject.OutputFilePath { get => this.Project.OutputFilePath!; }
        public bool IsBuilding
        {
            get => this.buildContext is not null;
        }

        private readonly List<BenchmarkDocument> documents = new();

        internal BenchmarkProject(Project project)
        {
            this.Project = project;
            project
                .Solution
                .Workspace
                .WorkspaceChanged += onWorkspaceChanged;
        }

        private void onWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.ProjectId != this.Id)
                return;

            switch (e.Kind)
            {
                case WorkspaceChangeKind.DocumentChanged:
                    {
                        if (e.DocumentId is null)
                            return;
                        if (this.TryFindTracker(e.DocumentId, out var document))
                        {
                            document.Update(e.NewSolution);
                        }
                        break;
                    }
            }
        }

        internal ValueTask<BenchmarkMetadataInfo?> GetInfoAsync(DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
        {
            var project = this.Project;

            if (this.TryFindTracker(documentId, out var doc))
            {
                return doc.GetInfoAsync(span, cancellationToken);
            }

            var document = project.GetDocument(documentId);
            if (document is null)
            {
                // TODO: Log
                return default;
            }

            return new(Task.Run(async () =>
            {
                if (!this.IsInitialized)
                {
                    await this.getVsProjectAsync(cancellationToken)
                    .ConfigureAwait(false);
                }
                var compilation = await project
                .GetCompilationAsync(cancellationToken)
                .ConfigureAwait(false);
                if (compilation is null)
                {
                    return null;
                }

                var root =await document
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);
                if (root is null)
                {
                    return null;
                }

                BenchmarkDocument bdoc;
                lock (this.documents)
                {
                    if (!this.TryFindTracker(documentId, out bdoc))
                    {
                        bdoc = new(this, document);
                        this.documents.Add(bdoc);
                    }
                }
                return await bdoc.GetInfoAsync(span, cancellationToken);

            }, cancellationToken));
        }

        private ValueTask getVsProjectAsync(CancellationToken cancellationToken)
        {
            if (this.IsInitialized)
                return default;
            var workspace = this.Project
                .Solution
                .Workspace as VisualStudioWorkspace;
            return new(Task.Run(async () =>
            {
                var buildMgr = await ServiceProvider
                .GetGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager>()
                .ConfigureAwait(false);
                await ThreadHelper.JoinableTaskFactory
                    .SwitchToMainThreadAsync(cancellationToken);

                if (this.IsInitialized)
                {
                    await TaskScheduler.Default;
                    return;
                }

                var hier = (workspace?.GetHierarchy(this.Id)) ?? throw new InvalidOperationException();

                var unconfiguredProj = hier.AsUnconfiguredProject();
                if (unconfiguredProj is null)
                {
                    return;
                }
                var activeConfigurationProvider = unconfiguredProj
                .Services
                .ActiveConfiguredProjectProvider;
                if (activeConfigurationProvider is null)
                {
                    return;
                }
                activeConfigurationProvider.Changed += onActiveConfigurationChanged;
                //IVsUpdateSolutionEvents
                ErrorHandler.ThrowOnFailure(
                buildMgr.AdviseUpdateSolutionEvents(this, out this.solutionUpdateCookie)
                    );

                //var projectId = hier.GetRootGuidProperty((int)__VSHPROPID.VSHPROPID_ProjectIDGuid);

                IVsCfgProvider2 cfgProvider = null!;
                hier.GetRootProperty((int)__VSHPROPID.VSHPROPID_ConfigurationProvider,
                    ref cfgProvider);

                var ar = new IVsProjectCfg[1];
                ErrorHandler.ThrowOnFailure(
                buildMgr.FindActiveProjectCfg(IntPtr.Zero,
                    IntPtr.Zero,
                    hier,
                    ar));

                ErrorHandler.ThrowOnFailure(
                    ar[0].get_BuildableProjectCfg(out var output));

                ErrorHandler.ThrowOnFailure(
                output.AdviseBuildStatusCallback(this, out this.buildStatusCookie));

                await TaskScheduler.Default;
                return;
            }));
        }

        private void onActiveConfigurationChanged(object sender, ActiveConfigurationChangedEventArgs e)
        {
            // TODO: Update all cached tests
        }

        private bool TryFindTracker(DocumentId documentId, out BenchmarkDocument doc)
        {
            lock (this.documents)
            {
                doc = this.documents.Find(x => x.Id == documentId);
            }
            return doc is not null;
        }

        IEnumerable<TrackerOperation> IBenchmarkProject.GetOperations()
        {
            lock (this.sync)
            {
                if (this.IsBuilding)
                {
                    return this.buildContext?
                        .Operations
                        .ToArray()
                        ?? Enumerable.Empty<TrackerOperation>();
                }
                else
                {
                    var operations = this.documents
                        .SelectMany(x => x.GetChanges());
                    return operations.ToArray();
                }
            }
        }

        internal void NotifyTestChanged()
        {
            this.TestChanged?.Invoke(this, EventArgs.Empty);
        }


        #region IVsUpdateSolutionEvents implementation
        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
            => VSConstants.S_OK;

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
            => VSConstants.S_OK;

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
            => VSConstants.S_OK;

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
            => VSConstants.S_OK;

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            // TODO: check output path is the same. If not, change test containers
            return VSConstants.S_OK;
        }
        #endregion IVsUpdateSolutionEvents implementation

        #region IVsBuildStatusCallback implementation

        int IVsBuildStatusCallback.BuildBegin(ref int pfContinue)
        {
            List<TrackerOperation> operations = new List<TrackerOperation>();
            lock (this.sync)
            {
                operations.AddRange(documents.SelectMany(x => x.GetChanges()));
                this.buildContext = new(operations);
            }
            return VSConstants.S_OK;
        }

        int IVsBuildStatusCallback.BuildEnd(int fSuccess)
        {
            lock (this.sync)
            {
                if (this.buildContext is not null)
                {
                    this.buildContext.Release();
                    this.buildContext = null;
                }
            }
            if (fSuccess == 0)
            {
                // the build failed. nothing to do
                return VSConstants.S_OK;
            }

            // clear the ignored test list
            return VSConstants.S_OK;
        }
        int IVsBuildStatusCallback.Tick(ref int pfContinue)
            => VSConstants.S_OK;
        #endregion IVsBuildStatusCallback implementation
    }
}
