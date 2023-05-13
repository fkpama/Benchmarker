using Sodiware;
using StronglyTypedIds;

namespace Benchmarker
{
    [StronglyTypedId(StronglyTypedIdBackingType.Guid,
        converters: StronglyTypedIdConverter.SystemTextJson,
        implementations: StronglyTypedIdImplementations.IEquatable
        | StronglyTypedIdImplementations.IComparable
        | StronglyTypedIdImplementations.Default)]
    public partial struct TestId
    {
        public bool IsMissing => Value.IsMissing();
        public TestId(string str)
            : this(Guid.Parse(str)) { }

        public static implicit operator TestId(Guid str)
            => new(str);
        public static implicit operator Guid(TestId str)
            => str.Value;

        public static bool operator==(TestId a, Guid b)
            => a.Value == b;
        public static bool operator !=(TestId a, Guid b)
            => !(a == b);
    }
}