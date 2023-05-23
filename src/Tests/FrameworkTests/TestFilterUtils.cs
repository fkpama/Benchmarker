using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace FrameworkTests
{
    internal class TestFilterUtils
    {
        public static ITestCaseFilterExpression CreateVSTestFilterExpression(string filter)
        {
            var filterExpressionWrapperType = Type.GetType("Microsoft.VisualStudio.TestPlatform.Common.Filtering.FilterExpressionWrapper, Microsoft.VisualStudio.TestPlatform.Common", throwOnError: true)!;

            var filterExpressionWrapper =
                filterExpressionWrapperType.GetTypeInfo()
                .GetConstructor(new[] { typeof(string) })!
                .Invoke(new object[] { filter })!;

            return (ITestCaseFilterExpression)Type.GetType("Microsoft.VisualStudio.TestPlatform.Common.Filtering.TestCaseFilterExpression, Microsoft.VisualStudio.TestPlatform.Common", throwOnError: true)!.GetTypeInfo()
                .GetConstructor(new[] { filterExpressionWrapperType })!
                .Invoke(new object[] { filterExpressionWrapper });
        }

    }
}
