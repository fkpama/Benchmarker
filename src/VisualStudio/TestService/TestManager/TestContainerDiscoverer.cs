using System.ComponentModel.Composition;
using Benchmarker.MsTests;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Benchmarker.VisualStudio.TestsService.TestManager
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class TestContainerDiscoverer
        : ITestContainerDiscoverer,
        IBenchmarkListener
    {
        readonly HashSet<TestContainer> containers = new();
        private readonly IBenchmarkService benchmarkService;
        private readonly IOperationState operationState;

        public Uri ExecutorUri { get; } = new(BenchmarkerConstants.ExecutorUri);
        public IEnumerable<ITestContainer> TestContainers { get => containers; }
        public IEnumerable<string> FileTypes { get; }

        public event EventHandler? TestContainersUpdated;

        [ImportingConstructor]
        public TestContainerDiscoverer(IBenchmarkService benchmarkService,
            IOperationState operationState,
            [Import(typeof(SVsServiceProvider))] IServiceProvider sp)
        {
            this.benchmarkService = benchmarkService;
            this.operationState = operationState;
            operationState.StateChanged += OperationState_StateChanged;
            var model = sp.GetService<SComponentModel, IComponentModel>();
            //var values = settings.Select(x => x.Value).ToArray();
            Log.Info($"TestContainerDiscoverer created");

            //var container = new TestContainer(this, "Hello.cs");
            benchmarkService.Advise(this);
            foreach(var project in benchmarkService.ActiveProjects)
            {
                containers.Add(new(this, project));
            }
        }

        private T? test<T>(IComponentModel model)
            where T : class
        {
            try
            {
                return model.GetService<T>();
            }
            catch
            {
                return null;
            }
        }

        private void OperationState_StateChanged(object sender, OperationStateChangedEventArgs e)
        {
        }

        public string GetCurrentTest(string filePath, int line, int lineCharOffset)
        {
            File.AppendAllText(@"C:\Temp\Hello.log", "RESOLVING\n");
            return null;
        }

        void IBenchmarkListener.OnProjectAdded(IBenchmarkService service, IBenchmarkProject project)
        {
            lock (this.containers)
            {
                var found = this.containers.FirstOrDefault(x => x.Project == project);
                if (found is null)
                {
                    this.containers.Add(new(this, project));
                    this.TestContainersUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        internal void NotifyChanged()
        {
            this.TestContainersUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}