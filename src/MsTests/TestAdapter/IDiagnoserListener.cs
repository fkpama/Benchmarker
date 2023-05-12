using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

namespace TestAdapter
{
    public interface IDiagnoserListener
    {
        void OnTestFinished(BenchmarkCase benchmarkCase);
    }
}