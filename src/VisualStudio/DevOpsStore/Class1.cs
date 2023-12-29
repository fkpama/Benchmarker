using System.ComponentModel.Composition;
using Benchmarker.VisualStudio;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace DevOpsStore
{
    [Export(typeof(IBenchmarkDataProvider))]
    public class Class1 : IBenchmarkDataProvider
    {
        private readonly ITeamFoundationContextManager contextManager;
        private readonly IServiceProvider services;

        internal ITeamFoundationContext CurrentContext
        {
            get => this.contextManager.CurrentContext;
        }

        [ImportingConstructor]
        public Class1([Import(typeof(SVsServiceProvider))]IServiceProvider services)
        {
            this.services = services;
            this.contextManager = (ITeamFoundationContextManager)services.GetService(typeof(ITeamFoundationContextManager))!;
            this.contextManager.ContextChanged += onDevOpsContextChanged;
        }

        private void onDevOpsContextChanged(object sender, ContextChangedEventArgs e)
        {
            var ctx = e.NewContext;
            var collection = this.contextManager.CurrentContext
                .TeamProjectCollection;
            if (collection is not null)
            {
                var credentials = collection.ClientCredentials;
            }
        }

        private T? getService<T>()
            where T : class
        {
            try
            {
                var comp = this.services.GetService<SComponentModel, IComponentModel>();
                return comp.GetService<T>();
            }
            catch
            {
                var svc = this.services.GetService(typeof(T));
                return (T?)svc;
            }
        }
    }
}