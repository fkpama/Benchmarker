using System.Reflection;

namespace Benchmarker.Framework
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<T> GetAllCustomAttributes<T>(this MethodInfo provider, bool inherit = false)
            where T: Attribute
        {
            foreach(var attr in provider.GetCustomAttributes<T>(inherit))
                yield return attr;

            if (provider.DeclaringType is not null)
            {
                foreach(var attr in provider
                    .DeclaringType
                    .GetCustomAttributes<T>(inherit))
                    yield return attr;

                foreach(var attr in provider
                    .DeclaringType
                    .Assembly
                    .GetCustomAttributes<T>())
                {
                    yield return attr;
                }
            }
        }
    }
}
