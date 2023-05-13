using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using Benchmarker.Running;

namespace Benchmarker.Framework.Validators
{
    public class BenchmarkValidationError
    {
        internal BenchmarkValidationError(BenchmarkTestCase testCase, bool isCritical, string message, BenchmarkReport? report)
        {
            this.TestCase = testCase;
            this.IsCritical = isCritical;
            this.Message = message;
            this.Report = report;
        }

        public BenchmarkTestCase TestCase { get; }
        public bool IsCritical { get; }
        public string Message { get; }
        public BenchmarkReport? Report { get; }
        public bool CanMerge { get; set; } = true;

        internal Conclusion CreateError(string validatorId)
                    => this.IsCritical ?
                    Conclusion.CreateError(validatorId,
                    this.Message,
                    this.Report,
                    this.CanMerge)
                    : Conclusion.CreateWarning(validatorId,
                    this.Message,
                    this.Report,
                    true);
    }
}