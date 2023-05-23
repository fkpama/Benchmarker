using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;

namespace Benchmarker.VisualStudio.TestsService
{
    internal sealed partial class BenchmarkDocument
    {
        private sealed class UpdateTask
        {
            private BenchmarkDocument? DocTracker;
            public List<BenchmarkTracker>? Trackers;
            public CancellationTokenSource Cancellation = new();
            readonly static ObjectPool<List<TrackerUpdateResult>> s_resultPool
                = ObjectPool.Create<List<TrackerUpdateResult>>();
            public Solution Solution;
            private Document From;
            private Document To;

            public bool IsCurrentVersion
            {
                get
                {
                    if (this.Solution is null)
                        return false;

                    return this.Solution.Version
                        == this.Solution.Workspace.CurrentSolution.Version;
                }
            }

            public UpdateTask()
            {
                this.Solution = null!;
                this.From = null!;
                this.To = null!;
            }

            internal void Reset()
            {
                lock (this)
                {
                    if (this.Trackers is not null)
                    {
                        s_trackerBuffer.Return(this.Trackers);
                        this.Trackers = null!;
                    }
                    this.DocTracker = null;
                    this.Trackers = null;
                    this.Solution = null!;
                    this.From = null!;
                    this.To = null!;
                    this.Cancellation = null!;
                    s_updatePool.Return(this);
                }
            }
            internal void Reset(Solution solution,
                                Document from,
                                Document to,
                                List<BenchmarkTracker> trackers,
                                BenchmarkDocument docTracker)
            {
                lock (this)
                {
                    if (this.Trackers is not null)
                    {
                        s_trackerBuffer.Return(this.Trackers);
                        this.Trackers = null!;
                    }
                    this.DocTracker = docTracker;
                    this.Trackers = trackers;
                    this.Solution = solution;
                    this.From = from;
                    this.To = to;
                    this.Cancellation = new();
                }
            }

            internal void Cancel()
            {
                this.Cancellation?.Cancel();
            }

            internal void Start()
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.doStartAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        this.Reset();
                        s_updatePool.Return(this);
                    }
                });
            }

            private async Task doStartAsync()
            {
                if (!this.IsCurrentVersion)
                {
                    Log.Debug("Outdated");
                    return;
                }

                if (this.Trackers is null || this.Trackers.Count == 0)
                {
                    // ??
                    this.release();
                    return;
                }

                var startTime = Stopwatch.GetTimestamp();

                var to = this.To;
                var cancellationToken = this.Cancellation.Token;
                var stree = await to
                    .GetSyntaxTreeAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (stree is null)
                {
                    Log.Error("Could not get a syntax tree?");
                    return;
                }
                var root = await stree
                .GetRootAsync(cancellationToken)
                .ConfigureAwait(false);
                List<TrackerUpdateResult>? actions = null;
                try
                {
                    foreach (var benchmark in this.Trackers)
                    {
                        var result = await benchmark
                    .UpdateAsync(stree,
                                 to,
                                 root,
                                 cancellationToken)
                    .ConfigureAwait(false);
                        if (result.Update != TrackerUpdate.None)
                        {
                            if (actions is null)
                            {
                                actions = s_resultPool.Get();
                                actions.Clear();
                            }
                            actions.Add(result);
                        }
                    }

                    Cancellation.Token.ThrowIfCancellationRequested();
                    if (actions is not null)
                    {
                        apply(to, actions);
                    }
                    var end = Stopwatch.GetTimestamp();
                    var t = TimeSpan.FromTicks(end - startTime);
                    Log.Verbose($"Update done ({t})");
                }
                finally
                {
                    if (actions is not null)
                        s_resultPool.Return(actions);
                }
            }

            private void apply(Document document, List<TrackerUpdateResult> toRemove)
            {
                var tracker = this.DocTracker;
                if (tracker is null)
                {
                    Log.Verbose("Cancel update. No doc tracker");
                    return;
                }
                tracker.Apply(this, document, toRemove);
            }

            private void release()
            {
                lock (this)
                {
                    this.Cancel();
                    if (this.Trackers is not null)
                    {
                        s_trackerBuffer.Return(this.Trackers);
                    }
                }
            }
        }
    }
}
