using Microsoft.CodeAnalysis;

namespace Benchmarker.VisualStudio
{
    public readonly struct TrackerOperation
    {
        public readonly IBenchmark Tracker;
        public readonly TrackerOperationType Operation;
        public IBenchmark Benchmark => this.Tracker;

        public TrackerOperation(IBenchmark tracker,
                                TrackerOperationType operation = TrackerOperationType.Remove)
        {
            this.Tracker = tracker;
            this.Operation = operation;
        }

    }
    public enum TrackerOperationType
    {
        Remove = 1,
        Add = 2,
    }
    public interface IBenchmarkMethodOpeartion
    {
        IBenchmark Benchmark { get; }
        TrackerOperationType Operation { get; }
    }

    public interface IBenchmark
    {
        IMethodSymbol Symbol { get; }
    }
    public interface IBenchmarkProject
    {
        string OutputFilePath { get; }

        event EventHandler TestChanged;

        IEnumerable<TrackerOperation> GetOperations();
    }
}