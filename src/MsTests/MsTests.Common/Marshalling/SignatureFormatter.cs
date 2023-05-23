using System.Reflection;
using System.Text;

namespace MsTests.Common.Marshalling
{
    public interface ITypeInfo
    {
        string FullName { get; }
    }

    public interface IParameterInfo
    {
        ITypeInfo Type { get; }
        string Name { get; }
    }

    public interface IMethodInfo
    {
        public ITypeInfo Type { get; }
        public string Name { get; }
        bool IsGenericMethod { get; }
        IEnumerable<ITypeInfo> TypeParameters { get; }
        IEnumerable<IParameterInfo> Parameters { get; }
    }
    public readonly struct TypeInfoWrapper : ITypeInfo
    {
        public string FullName { get => this.Type.FullName; }
        public Type Type { get; }

        public TypeInfoWrapper(Type type)
        {
            this.Type = type;
        }
    }
    public readonly struct ParameterInfoWrapper : IParameterInfo
    {
        private readonly ParameterInfo parameter;

        public ITypeInfo Type { get => new TypeInfoWrapper(this.parameter.ParameterType); }
        public string Name { get => this.parameter.Name; }
        public ParameterInfoWrapper(ParameterInfo parameter)
        {
            this.parameter = parameter;
        }
    }
    public readonly struct MethodInfoWrapper : IMethodInfo
    {
        private readonly MethodInfo method;
        public string Name { get => this.method.Name; }
        public bool IsGenericMethod
        {
            get => this.method.IsGenericMethod;
        }

        public IEnumerable<IParameterInfo> Parameters
        {
            get => this.method.GetParameters()
                .Select(x => (IParameterInfo)new ParameterInfoWrapper(x));
        }

        public IEnumerable<ITypeInfo> TypeParameters
        {
            get
            {
                var ar = this.method.GetGenericArguments();
                return ar.Select(x => (ITypeInfo)new TypeInfoWrapper(x));
            }
        }

        public ITypeInfo Type { get => new TypeInfoWrapper(this.method.DeclaringType); }

        public MethodInfoWrapper(MethodInfo method)
        {
            this.method = method;
        }
    }
    public class SignatureFormatter
    {
        public SignatureFormatter() { }

        public string Format(IMethodInfo method)
        {
            var name = method.Name;
            var sb = new StringBuilder();
            var type = method.Type.FullName;
            sb.Append(type);
            sb.Append('.');
            sb.Append(name);
            if (method.IsGenericMethod)
            {
                var ar = method.Parameters.ToArray();
                sb.Append('`');
                sb.Append(ar.Length);
            }
            sb.Append('(');
            foreach (var parameter in method.Parameters)
            {
                var ptype = parameter.Type;
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
