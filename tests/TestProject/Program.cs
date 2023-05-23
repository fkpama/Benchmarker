using BenchmarkDotNet.Running;

namespace TestProject
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var lst = new[]
            {
                typeof(Class1),
                typeof(Class2),
            };
            BenchmarkRunner.Run(lst, args: args);
        }
    }
}
