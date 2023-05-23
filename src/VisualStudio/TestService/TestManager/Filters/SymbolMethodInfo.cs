using Microsoft.CodeAnalysis;
using MsTests.Common.Marshalling;

namespace Benchmarker.VisualStudio.TestsService.TestManager.Filters
{
    internal readonly struct ParameterSymbolInfo : IParameterInfo
    {
        public ITypeInfo Type { get=> new TypeSymbolInfo(this.Symbol.Type); }
        public string Name => this.Symbol.Name;
        public IParameterSymbol Symbol { get; }
        public ParameterSymbolInfo(IParameterSymbol symbol) : this()
        {
            this.Symbol = symbol;
        }
    }
    internal readonly struct TypeSymbolInfo : ITypeInfo
    {
        public string FullName { get => this.Symbol.GetFullName(); }
        public ITypeSymbol Symbol { get; }

        internal TypeSymbolInfo(ITypeSymbol symbol)
        {
            this.Symbol = symbol;
        }
    }

    internal readonly struct SymbolMethodInfo : IMethodInfo
    {
        public ITypeInfo Type { get => new TypeSymbolInfo(this.Symbol.ContainingType); }
        public string Name => this.Symbol.Name;
        public bool IsGenericMethod { get => this.Symbol.IsGenericMethod; }
        public IEnumerable<ITypeInfo> TypeParameters
            => this.Symbol.TypeParameters
            .Select(x => (ITypeInfo)new TypeSymbolInfo(x));
        public IEnumerable<IParameterInfo> Parameters
            => this.Symbol.Parameters
            .Select(x => (IParameterInfo)new ParameterSymbolInfo(symbol: x));
        public IMethodSymbol Symbol { get; }
        public SymbolMethodInfo(IMethodSymbol symbol)
        {
            this.Symbol = symbol;
        }

    }
}
