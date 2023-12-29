using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;

namespace Benchmarker.VisualStudio.TestsService
{
    [Export(typeof(IBenchmarkService))]
    internal class BenchmarkerTestService : IBenchmarkService
    {
        private readonly VisualStudioWorkspace workspace;
        private readonly List<BenchmarkProject> projects = new();
        private List<IBenchmarkListener>? listeners;

        IEnumerable<IBenchmarkProject> IBenchmarkService.ActiveProjects
        {
            get
            {
                lock (this.projects)
                {
                    return this.projects.ToArray();
                }
            }
        }

        [ImportingConstructor]
        public BenchmarkerTestService(VisualStudioWorkspace workspace)
        {
            Log.Info("***** Benchmark test service starting *****");
            this.workspace = workspace;
            workspace.WorkspaceChanged += this.Workspace_WorkspaceChanged;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                    break;
                case WorkspaceChangeKind.ProjectRemoved:
                    break;
            }
        }

        public async Task<BenchmarkMetadataInfo?> GetInfoAsync(VSProjectId projectGuid,
                                                        string filePath,
                                                        TextSpan span, CancellationToken cancellationToken)
        {
            log($"Request for project {projectGuid}");
            try
            {
                var ts = TaskScheduler.Current;
                await ThreadHelper
                    .JoinableTaskFactory
                    .SwitchToMainThreadAsync(cancellationToken);
                var project = this.workspace.GetProjectOfGuid(projectGuid);
                log($"Project fetched: {project is not null}");
                await (ts ?? TaskScheduler.Default);
                if (project is null)
                {
                    return null;
                }
                return await IsBenchmarkProjectAsync(project,
                    filePath,
                    span,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log($"Error {ex}");
                return null;
            }

            static void log(string text)
            {
                File.AppendAllText(@"C:\Temp\Host.log", $"{text}\n");
            }
        }
        private Task<BenchmarkMetadataInfo?> IsBenchmarkProjectAsync(Project project,
                                                         string filePath,
                                                         TextSpan span,
                                                         CancellationToken cancellationToken)
        {
            var full = Path.GetFullPath(filePath);
            var document = project
                .Documents
                .FirstOrDefault(x => string.Equals(full, Path.GetFullPath(x.FilePath), StringComparison.OrdinalIgnoreCase));
            if (document is null)
            {
                return Task.FromResult<BenchmarkMetadataInfo?>(null);
            }
            return GetInfoAsync(project.Id, document.Id, span, cancellationToken);

        }
        public Task<BenchmarkMetadataInfo?> GetInfoAsync(ProjectId projectId,
                                                        DocumentId documentId,
                                                        TextSpan span,
                                                        CancellationToken cancellationToken)
        {
            var bproject = this.CreateProject(projectId);
            if (bproject is null)
            {
                return Task.FromResult<BenchmarkMetadataInfo?>(null);
            }

            return bproject
                .GetInfoAsync(documentId, span, cancellationToken)
                .AsTask();

        }

        private BenchmarkProject? CreateProject(ProjectId projectId)
        {
            var project = this.workspace
                .CurrentSolution
                .Projects
                .FirstOrDefault(x => x.Id == projectId);
            if (project is null)
                return null;

            return CreateProject(project);
        }

        private BenchmarkProject? CreateProject(Project project)
        {
            if (!project.TryGetCompilation(out var compilation)
                || !compilation.ReferencesBenchmarkDotNet())
                return null;

            BenchmarkProject? bproj;
            IBenchmarkListener[]? listeners = null;
            lock (this.projects)
            {
                if (!this.TryGetProject(project.Id, out bproj))
                {
                    bproj = new(project);
                    this.projects.Add(bproj);
                    listeners = this.listeners?.ToArray();
                }
            }
            if (listeners is not null && bproj is not null)
            {
                foreach(var listener in listeners)
                {
                    try
                    {
                        listener.OnProjectAdded(this, bproj);
                    }
                    catch
                    {
                        Log.Warn($"Error in listener {listener}");
                    }
                }
            }
            return bproj;
        }

        private bool TryGetProject(ProjectId projectId, out BenchmarkProject? project)
        {
            lock (this.projects)
            {
                project = this.projects.Find(x => x.Id == projectId);
            }
            return project is not null;
        }

        void IBenchmarkService.Advise(IBenchmarkListener listener)
        {
            lock(this.projects)
                (this.listeners ??= new()).Add(listener);
        }

        void IBenchmarkService.Unadvise(IBenchmarkListener listener)
        {
            lock(this.projects)
            {
                this.listeners?.Remove(listener);
            }
        }
    }
}
