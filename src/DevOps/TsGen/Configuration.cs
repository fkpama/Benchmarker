using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Benchmarker;
using Benchmarker.Serialization;
using Reinforced.Typings;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using Sodiware;

namespace TsGen
{
    public static class Configuration
    {
        static Type[] s_ignoreTypes = new[]
        {
            typeof(TestId)
        };
        static IEnumerable<Type> SharedFilter(this IEnumerable<Type> types)
        {
            return types.Except(s_ignoreTypes);
        }
        public static void Configure(ConfigurationBuilder builder)
        {
            const string interopNamespace = "Benchmarker.Interop";
            var assembly = typeof(BenchmarkDetail).Assembly;
            var type2 = assembly
                .SafeGetTypes()
                .Where(x => x.Namespace?.StartsWith(interopNamespace) == true)
                .SharedFilter()
                .ToArray();

            var types = assembly
                .SafeGetExportedTypes()
                .SharedFilter()
                .Except(type2)
                .ToList();

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

            builder.Substitute(typeof(TestId), new RtSimpleTypeName("string"));

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