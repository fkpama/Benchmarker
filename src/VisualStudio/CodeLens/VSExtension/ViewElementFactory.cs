using System.ComponentModel.Composition;
using System.Windows;
using Benchmarker.Wpf.Controls;
using CodeLensModels;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Benchmarker.VisualStudio.CodeLens
{
    [Export(typeof(IViewElementFactory))]
    [Name("Performance viewer")]
    [TypeConversion(from: typeof(BenchmarkCodeLensData), to: typeof(FrameworkElement))]
    internal class ViewElementFactory : IViewElementFactory
    {
        public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            // Should never happen if the service's code is correct, but it's good to be paranoid.
            if (typeof(FrameworkElement) != typeof(TView))
            {
                throw new ArgumentException($"Invalid type conversion. Unsupported {nameof(model)} or {nameof(TView)} type");
            }
            UserControl1? control = null;
            if (model is BenchmarkCodeLensData data)
            {
                control = new UserControl1
                {
                    DataContext = data,
                };
                return control as TView;
            }
            return null;
        }
    }
}
