using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarker.VisualStudio.TestsService
{
    internal enum TrackerUpdate
    {
        None = 0,
        Remove = 1,
        Update = 2
    }
    internal readonly struct TrackerUpdateResult
    {
        //public static TrackerUpdateResult Remove = new(null, TrackerUpdate.Remove, default);
        public static TrackerUpdateResult Nothing = new(null, TrackerUpdate.None, default);
        internal readonly TrackerUpdate Update;
        internal readonly TextSpan Span;
        internal readonly BenchmarkTracker? Tracker;

        public TrackerUpdateResult(BenchmarkTracker? tracker, TrackerUpdate update, TextSpan span = default)
        {
            this.Tracker = tracker;
            this.Update = update;
            this.Span = span;
        }
    }
    internal class BenchmarkTracker : IBenchmark
    {
        public BenchmarkDocument Document { get; }
        public Guid BenchmarkId { get; private set; }
        public string Id { get; }
        public MethodDeclarationSyntax Node { get; private set; }
        public TextSpan Span { get; private set; }
        public string MethodName
        {
            get => this.Node.Identifier.Text;
        }
        public IMethodSymbol Symbol { get; }

        internal BenchmarkTracker(BenchmarkDocument document,
                                  string id,
                                  IMethodSymbol symbol)
        {
            this.Document = document;
            this.Id = id;
            this.Symbol = symbol;
        }

        internal async Task<BenchmarkMetadataInfo?> GetInfosAsync(CancellationToken cancellationToken)
        {
            return new()
            {
                TestId = this.BenchmarkId
            };
        }

        internal ValueTask<TrackerUpdateResult> UpdateAsync(SyntaxTree newSyntaxTree,
                                                 Document newDocument,
                                                 SyntaxNode root,
                                                 CancellationToken cancellationToken)
        {
            Log.Debug($"Updating benchmark {this.Id}");
            return new(Task.Run(async () =>
            {
                //var n = root.FindNode(newSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>(); ;
                var n = await SymbolFinder
                .FindSourceDefinitionAsync(this.Symbol,
                newDocument.Project.Solution,
                cancellationToken)
                .ConfigureAwait(false);
                if (n is null)
                {
                    Log.Info($"benchmark '{this.Id}' Disappeared");
                    return new(this, TrackerUpdate.Remove);
                }
                var sources = n.Locations
                    .Where(x => x.IsInSource
                    && x.SourceTree == newSyntaxTree)
                    .ToArray();

                if (sources.Length > 1)
                {
                    throw new NotImplementedException("TODO");
                }
                else if (sources.Length == 0)
                {
                    throw new NotImplementedException("TODO");
                }
                var span = sources[0].SourceSpan;
                if (span == this.Span)
                {
                    return TrackerUpdateResult.Nothing;
                }
                return new(this, TrackerUpdate.Update, span);
            }));
        }

        internal void Update(TextSpan span)
        {
            this.Span = span;
        }
    }
}
