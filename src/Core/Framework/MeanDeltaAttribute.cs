using System.Configuration;
using BenchmarkDotNet.Configs;
using Benchmarker.Columns;
using Benchmarker.Diagnosers;
using Benchmarker.Framework.Engine;
using Benchmarker.Framework.Validators;

namespace Benchmarker
{
    public abstract class HistoryAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }
        protected HistoryAttribute()
        {
            var conf = new ManualConfig();
            conf
                .AddExporter(Platform.GetExporter())
                .AddValidator(ExporterValidator.Instance);
            this.Config = this.Configure(conf);
        }

        protected abstract IConfig Configure(ManualConfig conf);
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class MeanDeltaAttribute : HistoryAttribute, IConfigSource
    {
        public MeanDeltaAttribute()
        {
        }
        protected override IConfig Configure(ManualConfig conf)
        {
            conf
                .AddDiagnoser(DeltaDiagnoser.Instance)
                .AddColumnProvider(new DeltaColumnProvider());
            return conf;
        }
    }
}
