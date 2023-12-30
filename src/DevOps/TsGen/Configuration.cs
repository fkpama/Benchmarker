using System.Reflection;
using System.Text.Json;
using Benchmarker.Serialization;
using Reinforced.Typings.Fluent;
using Sodiware;

namespace TsGen
{
    public static class Configuration
    {
        public static void Configure(ConfigurationBuilder builder)
        {
            const string interopNamespace = "Benchmarker.Interop";
            var assembly = typeof(BenchmarkDetail).Assembly;
            var types = assembly.GetExportedTypes();

            var type2 = assembly
                .SafeGetTypes()
                .Where(x => x.Namespace?.StartsWith(interopNamespace) == true)
                .ToArray();

            types = assembly
                .SafeGetExportedTypes()
                .Except(type2)
                .ToArray();

            Console.WriteLine($"INTEROP TYPES: {string.Join(", ", types.Select(x => x.Name))}");
            File.WriteAllText(@"F:\Temp\interopTypes.txt", string.Join(", ", type2.Select(x => x.Name)));
            var interfaces = types.Where(x => x.IsInterface);
            var interopInterfaces = type2.Where(x => x.IsInterface);
            var classes = types.Where(x => x.IsClass).Concat(types.Where(x => x.IsStruct()));
            var enums = types.Where(x => x.IsEnum);

            builder.Global(c =>
            {
                c
                .CamelCaseForMethods()
                .CamelCaseForProperties()
                .UseModules(true);
            });

            builder.ExportAsInterfaces(interopInterfaces, c =>
            {
                c
                .WithAllMethods(m => m.OverrideName(m.Member.Name))
                .AutoI(false);
                if (c.Type.Name.StartsWith("I") && char.IsUpper(c.Type.Name[1]))
                    c.OverrideName(c.Type.Name.Substring(1));
            });
            builder.ExportAsInterfaces(interfaces, c =>
            {
                c
                .WithAllMethods()
                .WithAllProperties()
                .WithAllMethods();
            });

            builder.ExportAsInterfaces(classes, c =>
            {
                c
                .WithAllProperties()
                .AutoI(false);
            });
        }

    }
}