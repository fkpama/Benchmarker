using Sodiware;

namespace FrameworkTests
{
    internal class TestFailureException : Exception
    {
        public TestFailureException(string? displayName)
            : base(displayName.IsPresent() ? $"Test Failed: {displayName}" : null)
        {
            this.DisplayName = displayName;
        }

        public string? DisplayName { get; }
    }
}