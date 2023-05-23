using BenchmarkDotNet.Configs;
using TestProject;

[assembly: Debug]

namespace TestProject
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DebugAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }
        public DebugAttribute()
        {
            this.Config = new ManualConfig()
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
        }
    }
}