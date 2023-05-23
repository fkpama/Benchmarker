using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace Benchmarker.VisualStudio.TestsService.TestManager
{
    internal sealed class TestContainer : IBuildableTestContainer
    {
        private readonly IBenchmarkProject project;
        private readonly TestContainerDiscoverer discoverer;
        private bool hasChanged;

        public TestContainer(TestContainerDiscoverer discoverer, IBenchmarkProject project)
        {
            this.discoverer = discoverer;
            this.project = project;
            project.TestChanged += onProjectTestChanged;
        }

        ITestContainerDiscoverer ITestContainer.Discoverer { get => this.discoverer; }
        string ITestContainer. Source { get => this.project.OutputFilePath; }
        IEnumerable<Guid>? ITestContainer. DebugEngines { get; }
        FrameworkVersion ITestContainer. TargetFramework { get; }
        Architecture ITestContainer.TargetPlatform { get; }
        bool ITestContainer.IsAppContainerTestContainer { get; }
        public IBenchmarkProject Project { get => this.project; }

        public Task<bool> BuildAsync(CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(ITestContainer other)
        {
            return other == this && !this.hasChanged ? 0 : 1;
        }

        IDeploymentData? ITestContainer.DeployAppContainer() { return null; }

        public Task<bool> GetIsUpToDateAsync(CancellationToken cancelToken)
        {
            return Task.FromResult(true);
        }

        public ITestContainer Snapshot()
        {
            return this;
        }
        private void onProjectTestChanged(object sender, EventArgs e)
        {
            this.hasChanged = true;
            this.discoverer.NotifyChanged();
        }

    }
}