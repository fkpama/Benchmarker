using Benchmarker.Framework.Utils;
using Benchmarker.Testing;
using Benchmarker.Validation;
using Perfolizer.Horology;

namespace Benchmarker.Framework
{

    public sealed class MaxTimeAttribute : BenchmarkValidatorAttribute
    {
        public TimeInterval Time { get; }
        public override string Name { get => $"Max time ({this.Time})"; }

        public MaxTimeAttribute(string time)
        {
            this.Time = BenchHelp.ParseTime(time);
        }

        public override void Validate(BenchmarkValidationContext context)
        {
            var time = context.Mean;
            if(time > this.Time)
            {
                context.AddError($"Method took too long ({time} > {this.Time}) ");
            }
        }
    }

    public sealed class MaxMemAttribute : BenchmarkBuilderAttribute
    {
        public double Max { get; }
        public MaxMemAttribute(string mem)
        {
            this.Max = BenchHelp.ParseSize(mem);
        }
        protected internal override void Build(BenchmarkTestCase testCase, TestCaseCollection collection)
        {
            testCase.AddValidator(new MaxMemValidator(this.Max));
        }
    }

    public class MemThresholdAttribute : BenchmarkBuilderAttribute
    {
        public double Threshold { get; }

        public MemThresholdAttribute(string threshold)
        {
            this.Threshold = BenchHelp.ParseSize(threshold);
        }

        protected internal override void Build(BenchmarkTestCase testCase, TestCaseCollection collection)
        {
            testCase.AddValidator(new MemoryThresholdValidator(this.Threshold));
        }
    }
}
