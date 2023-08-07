using System.Collections.Immutable;
using System.Reflection;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Running;
using Benchmarker.Serialization;
using Benchmarker.Validation;
using Sodiware;

namespace Benchmarker.Testing
{
    public class BenchmarkTestCase
    {
        private readonly HashSet<BenchmarkExceptionData> exceptions = new();
        private List<IBenchmarkValidator>? validators;
        public event EventHandler<BenchmarkResultEventArgs>? Failed;
        public event EventHandler<BenchmarkResultEventArgs>? Succeeded;
        public event EventHandler<BenchmarkResultEventArgs>? Result;
        public event EventHandler<BenchmarkConclusionEventArgs>? Conclusion;
        public event EventHandler? ProcessStart
            , ProcessExit
            , Enter
            , Exit
            , RunStart
            , RunEnd;

        public IReadOnlyCollection<BenchmarkExceptionData> ExceptionDatas
        {
            get => this.exceptions;
        }

        public MethodInfo Method { get => this.BenchmarkCase.Descriptor.WorkloadMethod; }

        public BenchmarkRunInfo RunInfo { get; }
        public BenchmarkCase BenchmarkCase { get; }
        //public AssemblyLoadContext? AssemblyLoadContext { get; set; }
        public BenchmarkRecord Record { get; }
        public int? SourceCodeLine { get; private set; }
        public IConfig? Config { get; internal set; }
        public TestId Id { get; }
        public int ResultCount { get; internal set; }
        public double? MeanThreshold
        {
            get => this.Record?.Mean;
        }
        public double? LastRunMean
        {
            get => this.Record?.Mean;
        }
        public bool HasLastRunMean
        {
            get => this.LastRunMean.HasValue
                && this.LastRunMean.Value > 0;
        }
        public long? LastRunAllocated
        {
            get => this.Record?.BytesAllocated;
        }
        public BenchmarkRecord CurrentRecord { get; } = new();
        public string FullyQualifiedName { get; }
        public string? SourceCodeFile { get; private set; }
        public int? SourceCodeLineNumber { get; }
        internal IReadOnlyList<IBenchmarkValidator>? Validators
        {
            get => this.validators;
        }
        public bool HasResult { get; internal set; }

        //internal bool IsAnalyzing;

        public BenchmarkTestCase(BenchmarkRunInfo run,
                                 TestId id,
                                 IConfig? config,
                                 BenchmarkCase benchmarkTestCase,
                                 BenchmarkRecord record,
                                 string fullyQualifiedName,
                                 string? sourceCodeFile,
                                 int? sourceCodeLine)
        {
            RunInfo = run;
            this.Id = id;
            Config = config;
            BenchmarkCase = benchmarkTestCase;
            //TestCase = testCase;
            //AssemblyLoadContext = assemblyLoadContext;
            Record = record;
            this.FullyQualifiedName = fullyQualifiedName;
            this.SourceCodeFile = sourceCodeFile;
            this.SourceCodeLine = sourceCodeLine;
            this.initSourceCode();
        }

        public BenchmarkTestCase(TestId id,
                                 ImmutableConfig config,
                                 BenchmarkCase test,
                                 BenchmarkRecord benchmarkRecord)
        {
            this.Id = id;
            this.Config = config;
            this.Config = config;
            this.BenchmarkCase = test;
            this.Record = benchmarkRecord;
            this.initSourceCode();
        }

        private void initSourceCode()
        {
            var attr = this.Method.GetCustomAttribute<BenchmarkAttribute>(true);
            if (attr is not null)
            {
                if (!this.SourceCodeLine.HasValue)
                    this.SourceCodeLine = attr.SourceCodeLineNumber;
                if (this.SourceCodeFile.IsMissing())
                    this.SourceCodeFile = attr.SourceCodeFile;
            }
        }

        internal long? GetMemDelta(long allocated)
        {
            var old = this.LastRunAllocated;
            if (!old.HasValue)
            {
                return null;
            }

            var result = (long)Math
                .Round((double)(allocated - old), MidpointRounding.AwayFromZero);
            return result;
        }

        internal void AddValidator(IBenchmarkValidator validator)
        {
            (this.validators ??= new()).Add(Guard.NotNull(validator));
        }

        internal double? GetMemDelta(BenchmarkReport report)
        {
            if (!this.LastRunAllocated.HasValue)
                return null;
            var alloc = this.GetAllocated(report);
            var old = this.LastRunAllocated;
            return alloc - old.Value;
        }
        internal double GetAllocated(BenchmarkReport report)
        {
            return report.GetAllocated(this.BenchmarkCase);
        }

        internal void NotifyResult(Summary summary, IEnumerable<Conclusion> conclusions)
        {
            var failed = conclusions.Any(x => x.Kind == ConclusionKind.Error)
                || this.exceptions.Any();
            var args = this.CreateResult(summary,
                                         conclusions,
                                         failed,
                                         this.exceptions);
            if (failed)
                this.Failed?.Invoke(this, args);
            else
                this.Succeeded?.Invoke(this, args);

            this.Result?.Invoke(this, args);
        }

        private bool Match(BenchmarkReport arg)
            => arg?.BenchmarkCase == this.BenchmarkCase;

        private protected virtual BenchmarkResultEventArgs CreateResult(Summary summary, IEnumerable<Conclusion> conclusions, bool failed, IEnumerable<BenchmarkExceptionData> exceptionDatas)
            => new(this, summary, conclusions, failed, exceptions.ToImmutableArray());

        internal void OnProcessStart()
        {
            this.ProcessStart?.Invoke(this);
        }

        internal void OnRunStart()
        {
            this.RunStart?.Invoke(this);
        }

        internal void OnProcessExit()
        {
            this.ProcessExit?.Invoke(this);
        }

        internal void OnRunEnd()
        {
            this.RunEnd?.Invoke(this);
        }

        internal void OnExit()
        {
            this.Exit?.Invoke(this);
        }

        internal void OnStart()
        {
            this.Enter?.Invoke(this);
        }

        internal void OnConclusion(Conclusion conclusion)
        {
            this.Conclusion?.Invoke(this, new(this, conclusion));
        }

        internal void AddException(BenchmarkExceptionData infos)
        {
            this.exceptions.Add(infos);
        }
    }
}
