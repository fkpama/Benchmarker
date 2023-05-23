using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapter
{
    internal class FilterLayer
    {
        internal static Dictionary<string, TestProperty> SupportedPropertiesCache;
        static FilterLayer()
        {
            // Initialize the property cache
            SupportedPropertiesCache = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase)
            {
                ["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName,
                ["Name"] = TestCaseProperties.DisplayName,
                //["TestCategory"] = CategoryList.NUnitTestCategoryProperty,
                //["Category"] = CategoryList.NUnitTestCategoryProperty
            };

        }

        internal static TestProperty? PropertyProvider(string arg)
        {
            throw new NotImplementedException();
        }

        internal static object? PropertyProvider(TestCase testCase, string propertyName)
        {
            if (SupportedPropertiesCache
                .TryGetValue(propertyName, out var testProperty)
                && testCase.Properties.Contains(testProperty))
            {
                return testCase.GetPropertyValue(testProperty);
            }

            return null;
        }
    }
}
