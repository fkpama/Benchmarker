namespace Benchmarker
{
    public class BenchmarkDetail
    {
        public Guid Id { get; set; }
        public required string Name { get; init; }
        //public string RefId { get; set; }
        public required string FullName { get; init; }
        public string? MethodTitle { get; set; }
    }
}
