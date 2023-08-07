using BenchmarkDotNet.Reports;
using Benchmarker.Testing;
using Benchmarker.Validation;

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