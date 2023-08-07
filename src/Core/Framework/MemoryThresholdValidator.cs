using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using Benchmarker.Validation;

namespace Benchmarker.Framework
{
    internal sealed class MaxMemValidator : IBenchmarkValidator
    {
        public MaxMemValidator(double max)
        {
            this.Max = max;
        }

        public double Max { get; }
        public string Name { get => $"Max mem ({this.Max.ToSizeString()})"; }

        public void Validate(BenchmarkValidationContext context)
        {
            var testCase = context.TestCase;
            var value = context.Allocated;
            if (value > this.Max)
            {
                var str = value.ToSizeString();
                var allowed = this.Max.ToSizeString();
                context.AddError($"Allocated memory exceeds max allowed: {str} > {allowed}");
            }
        }
    }
    internal class MemoryThresholdValidator : IBenchmarkValidator
    {
        public MemoryThresholdValidator(double threshold)
        {
            this.Threshold = threshold;
        }

        public double Threshold { get; }
        public string Name { get => $"Memory Threshold ({this.Threshold.ToSizeString()})"; }

        public void Validate(BenchmarkValidationContext context)
        {
            var testCase = context.TestCase;
            var delta = testCase.GetMemDelta(context.Report);
            if (!delta.HasValue)
                return;
            if (delta > this.Threshold)
            {
                context.AddError("Mem too high", true, context.Report);
            }
        }
    }
}