namespace Benchmarker.Framework.Exporters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BenchmarkerExporterAttribute : BenchmarkDotNet.Attributes.ExporterConfigBaseAttribute
    {
        public BenchmarkerExporterAttribute()
            : base(Platform.GetExporter())
        {
        }
    }
}