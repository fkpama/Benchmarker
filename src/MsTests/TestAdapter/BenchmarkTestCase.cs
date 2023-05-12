using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sodiware.Benchmarker;

namespace TestAdapter
{
    internal class BenchmarkTestCase
    {
        public BenchmarkRunInfo RunInfo { get; }
        public BenchmarkCase BenchmarkCase { get; }
        public TestCase TestCase { get; }
        public AssemblyLoadContext? AssemblyLoadContext { get; set; }
        public IConfig? Config { get; internal set; }

        public BenchmarkTestCase(BenchmarkRunInfo run,
                                 IConfig? config,
                                 BenchmarkCase benchmarkTestCase,
                                 TestCase testCase,
                                 AssemblyLoadContext? assemblyLoadContext)
        {
            this.RunInfo = run;
            this.Config = config;
            this.BenchmarkCase = benchmarkTestCase;
            this.TestCase = testCase;
            this.AssemblyLoadContext = assemblyLoadContext;
        }

    }
}
