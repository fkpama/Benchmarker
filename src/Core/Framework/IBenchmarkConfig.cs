using BenchmarkDotNet.Reports;
using Benchmarker.Engine;
using Benchmarker.Framework.Validators;
using Benchmarker.Running;

namespace Benchmarker.Framework
{
    public abstract class BenchmarkerAttribute : Attribute
    { }
    public abstract class BenchmarkValidatorAttribute : BenchmarkerAttribute, IBenchmarkValidator
    {
        public abstract string Name { get; }
        public abstract void Validate(BenchmarkValidationContext context);
    }
    public abstract class BenchmarkBuilderAttribute : Attribute
    {
        protected internal abstract void Build(BenchmarkTestCase testCase, TestCaseCollection collection);
    }
}