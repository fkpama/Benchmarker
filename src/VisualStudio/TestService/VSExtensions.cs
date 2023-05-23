using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Benchmarker.VisualStudio.TestsService
{
    internal static class VSExtensions
    {
        internal static IVsHierarchy GetProjectOfGuid(this IVsSolution sln, Guid guid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(
                sln.GetProjectOfGuid(ref guid, out var hier));
            return hier;
        }
        internal static Guid GetProjectId(this IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(
                hierarchy.GetGuidProperty((uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                out var guid));
            return guid;
        }
        internal static void GetRootProperty<T>(this IVsHierarchy hier, int propid, ref T value )
        {
            value = GetRootProperty<T>(hier, propid);
        }
        internal static Guid GetRootGuidProperty(this IVsHierarchy hier, int propid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const uint itemid = (uint)VSConstants.VSITEMID.Root;
            ErrorHandler.ThrowOnFailure(
                hier.GetGuidProperty(itemid, propid, out var guid)
                );
            return guid;
        }
        internal static T GetRootProperty<T>(this IVsHierarchy hier, int propid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const uint itemid = (uint)VSConstants.VSITEMID.Root;
            ErrorHandler.ThrowOnFailure(
                hier.GetProperty(itemid, propid, out var guid)
                );
            return (T)guid;
        }
        //internal static bool TryGetCPSProject(this IVsHierarchy hierarchy, out UnconfiguredProject? cpsProject)
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();
        //    var context = hierarchy as IVsBrowseObjectContext;
        //    if (context is null && hierarchy is IVsHierarchy hier2)
        //    { // VC implements this on their DTE.Project.Object
        //            if (ErrorHandler.Succeeded(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out var extObject)))
        //            {
        //                if (extObject is EnvDTE.Project dteProject)
        //                {
        //                    context = dteProject.Object as IVsBrowseObjectContext;
        //                }
        //            }
        //    }

        //    cpsProject = context?.UnconfiguredProject;

        //    return cpsProject is not null;
        //}
    }
}
