using BenchmarkDotNet.Reports;
using Benchmarker.Framework.Validators;
using Benchmarker.Testing;
using Perfolizer.Horology;

namespace Benchmarker.Validation
{
    public sealed class BenchmarkValidationContext
    {
        private List<BenchmarkValidationError>? errors;
        internal BenchmarkValidationContext(string id,
                                            BenchmarkTestCase testCase,
                                            Summary summary,
                                            BenchmarkReport report)
        {
            this.ValidatorId = id;
            this.TestCase = testCase;
            this.Summary = summary;
            this.Report = report;
        }

        internal string ValidatorId { get; }
        public BenchmarkTestCase TestCase { get; }
        public Summary Summary { get; }
        public BenchmarkReport Report { get; }
        internal IReadOnlyList<BenchmarkValidationError>? Errors
        {
            get => this.errors;
        }
        public TimeInterval Mean
        {
            get => new(this.Report.ResultStatistics!.Mean);
        }
        public double Allocated => this.Report.GetAllocated(this.TestCase);

        public BenchmarkValidationError AddError(string message,
            BenchmarkReport? report = null)
            => AddError(message, true, report);
        public BenchmarkValidationError AddError(string message, bool isCritical,
            BenchmarkReport? report)
        {
            var error = new BenchmarkValidationError(
                this.TestCase,
                isCritical,
                message,
                report ?? this.Report);
            (this.errors ??= new()).Add(error);
            return error;

        }
    }
}
