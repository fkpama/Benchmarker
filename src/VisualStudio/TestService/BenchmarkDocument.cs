using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.ObjectPool;

// TODO: Register IMaintenanceTask
namespace Benchmarker.VisualStudio.TestsService
{
    internal sealed class TrackerEqualityComparer : IEqualityComparer<BenchmarkTracker>
    {
        private readonly SymbolEqualityComparer comparer;

        public TrackerEqualityComparer(SymbolEqualityComparer comparer)
        {
            this.comparer = comparer;
        }

        public bool Equals(BenchmarkTracker x, BenchmarkTracker y)
            => this.comparer.Equals(x?.Symbol, y?.Symbol);

        public int GetHashCode(BenchmarkTracker obj)
            => this.comparer.GetHashCode(obj?.Symbol);
    }

    internal sealed partial class BenchmarkDocument
    {
        const string AnnotationName = nameof(BenchmarkTracker);
        private readonly HashSet<BenchmarkTracker> trackers;
        private UpdateTask? currentUpdate;
        private static readonly ObjectPool<UpdateTask> s_updatePool
            = ObjectPool.Create<UpdateTask>();
        private static readonly ObjectPool<List<BenchmarkTracker>> s_trackerBuffer
            = ObjectPool.Create<List<BenchmarkTracker>>();
        private string? fname;
        private readonly HashSet<TrackerOperation> removedTests = new();

        private readonly SymbolEqualityComparer SymbolComparer
            = SymbolEqualityComparer.Default;

        public DocumentId Id { get => this.Document.Id; }
        public Document Document { get; private set; }
        private BenchmarkProject Project { get; }
        public string? FilePath { get => this.Document.FilePath; }
        private Document CurrentVersion
        {
            get
            {
                return this.Project
                    .CurrentVersion
                    .GetDocument(this.Id)
                    ?? throw new InvalidOperationException("Document disappeared");
            }
        }

        public BenchmarkTracker? this[IMethodSymbol symbol]
        {
            get
            {
                this.TryFindTracker(symbol, out var tracker);
                return tracker;
            }
        }

        public string Filename
        {
            get => this.fname ??= Path.GetFileName(this.FilePath);
        }

        public BenchmarkDocument(BenchmarkProject benchmarkProject, Document document)
        {
            this.Project = benchmarkProject;
            this.Document = document;
            this.trackers = new(new TrackerEqualityComparer(this.SymbolComparer));
        }

        internal async ValueTask<BenchmarkMetadataInfo?> GetInfoAsync(TextSpan span, CancellationToken cancellationToken)
        {
            var doc = this.Project
                .CurrentVersion
                .GetDocument(this.Id);
            Debug.Assert(doc is not null);
            if (doc is null)
            {
                Log.Warn($"Document disapeared {this.FilePath}");
                return default;
            }
            else
            {
                Log.Debug($"Request for span {span}");
            }

            var version = this.CurrentVersion;
            var current = await version
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);
            if (current is null)
                throw new NotImplementedException();

            SemanticModel semanticModel;

            var node = current.FindNode(span)
                .FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (node is null)
            {
                return default;
            }

            semanticModel = (await version
                .GetSemanticModelAsync(cancellationToken)
                .ConfigureAwait(false))!;

            if (semanticModel is null)
            {
                Log.Warn($"Could not obtain a semantic model");
                return default;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node);
            if (symbol is null)
            {
                Log.Warn($"Could not obtain a declared symbol for method '{node.Identifier.Text}'");
                return default;
            }

            if (!symbol.HasBenchmarkAttribute())
            {
                Log.Debug($"Not a benchmark '{node.Identifier.Text}'");
                return default;
            }

            BenchmarkTracker? tracker = null;
            lock (this.trackers)
            {
                tracker = this.trackers
                    .FirstOrDefault(x => SymbolComparer.Equals(x.Symbol, symbol));
            }
            if (tracker is not null)
            {
                Log.Debug($"Tracker '{tracker.Id}' found for method {node.Identifier.Text}");
                return await tracker
                    .GetInfosAsync(cancellationToken)
                    .ConfigureAwait(false);
            }


            var id = createId();
            tracker = new(this, id, symbol);
            lock (this.trackers)
            {
                if (!TryFindTracker(symbol, out var found))
                {
                    if (this.trackers.Add(tracker))
                    {
                        Log.Debug($"Created benchmark '{tracker.Id}' for request");
                    }
                }
                else
                {
                    tracker = found;
                }
            }

            return await tracker
                .GetInfosAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private bool TryFindTracker(IMethodSymbol symbol, out BenchmarkTracker tracker)
        {
            lock (this.trackers)
            {
                tracker = this.trackers
                    .FirstOrDefault(x => this.SymbolComparer
                    .Equals(x.Symbol, symbol));
            }
            return tracker is not null;
        }

        private static string createId()
            => Guid.NewGuid().ToString("N").Substring(0, 8);

        internal void Update(Solution solution)
        {
            Log.Debug("Document updated");
            UpdateTask task;
            List<BenchmarkTracker> trackers;
            var newDoc = solution.GetDocument(this.Id);
            Debug.Assert(newDoc is not null);
            if (newDoc is null)
            {
                return;
            }
            lock (this.trackers)
            {
                if (this.trackers.Count == 0)
                {
                    Log.Debug($"No tracker to update in document {this.Filename}");
                }
                this.currentUpdate?.Cancel();
                task = s_updatePool.Get();
                trackers = s_trackerBuffer.Get();
                trackers.Clear();
                trackers.AddRange(this.trackers);
                this.currentUpdate = task;
            }
            task.Reset(solution, this.Document, newDoc, trackers, this);
            task.Start();
        }


        void Apply(UpdateTask updateTask,
                           Document appliedDocument,
                           List<TrackerUpdateResult> actions)
        {
            lock (this.trackers)
            {
                if (this.currentUpdate != updateTask)
                {
                    Log.Warn($"Current update is not me!");
                    return;
                }
                appliedDocument.TryGetTextVersion(out var version);
                Log.Verbose($"Applying benchmarks version: {version}");
                foreach (var action in actions)
                {
                    var benchmark = action.Tracker!;
                    switch (action.Update)
                    {
                        case TrackerUpdate.Update:
                            {
                                Log.Verbose($"Updating {benchmark.Id}: {action.Span}");
                                benchmark.Update(action.Span);
                                break;
                            }
                        case TrackerUpdate.Remove:
                            {
                                Log.Verbose($"Removing {benchmark.Id}");
                                if (!this.trackers.Remove(benchmark!))
                                {
                                    Log.Warn($"Tracker {action.Tracker!.Id} not in update source");
                                }
                                else
                                {
                                    this.removedTests.Add(new(benchmark));
                                }
                                break;
                            }
                        default:
                            throw new NotImplementedException();
                    }
                    this.Document = appliedDocument;
                }
                Log.Info("Update done");
                Debug.Assert(this.currentUpdate == updateTask);
                this.currentUpdate = null;
            }
            this.Project.NotifyTestChanged();
        }

        internal IEnumerable<TrackerOperation> GetChanges()
        {
            lock (this.trackers)
            {
                var operations = this.removedTests.ToArray();
                return operations;
            }
        }
    }
}
