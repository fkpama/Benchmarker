namespace Benchmarker.Validation
{
    public interface IBenchmarkValidator
    {
        string Name { get; }
        void Validate(BenchmarkValidationContext context);
    }
}
