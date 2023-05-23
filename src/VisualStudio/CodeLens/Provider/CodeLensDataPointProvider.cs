using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Benchmarker.VisualStudio.CodeLens.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace Benchmarker.VisualStudio.CodeLens.Provider
{
    sealed class BenchmarkMethodTracker
    {
        public BenchmarkData Infos { get; internal set; }

        public BenchmarkMethodTracker(BenchmarkData infos)
        {
            this.Infos = infos;
        }
    }
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name(Id)]
    [ContentType("code")]
    [LocalizedName(typeof(Resources), "LocalizedProviderName")]
    [Priority(210)]
    public sealed class BenchmarkerCodeLensDataPointProvider : IAsyncCodeLensDataPointProvider
    {
        internal const string Id = nameof(BenchmarkerCodeLensDataPointProvider);
        private readonly HostWrapper service;
        private readonly ConditionalWeakTable<DescriptorCacheItem, BenchmarkMethodTracker> trackers = new();

        [ImportingConstructor]
        public BenchmarkerCodeLensDataPointProvider(Lazy<ICodeLensCallbackService> callbackService)
        {
            this.service = new HostWrapper(callbackService, this);
        }
        public async Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor,
                                                  CodeLensDescriptorContext descriptorContext,
                                                  CancellationToken token)
        {
            Log.WriteLine($"Request for '{descriptor.ProjectGuid}/{descriptor.ElementDescription}/{descriptor.Kind}' ({descriptor.FilePath}:{descriptorContext.ApplicableSpan})");
            if (!descriptorContext.ApplicableSpan.HasValue)
            {
                return false;
            }
            if (descriptor.Kind != CodeElementKinds.Method)
            {
                Log.WriteLine($"Not a method '{descriptor.ElementDescription}'");
                return false;
            }
            try
            {
                var isBenchmark = await this
                    .RequestDataAsync(descriptor, descriptorContext, token)
                    .ConfigureAwait(false);
                if (!isBenchmark.HasValue)
                    return false;
                var element = new DescriptorCacheItem(descriptor, descriptorContext);
                lock (this.trackers)
                {
                    if (!this.trackers.TryGetValue(element, out var current))
                    {
                        current = new(isBenchmark.Value);
                        this.trackers.Add(element, current);
                    }
                    current.Infos = isBenchmark.Value;
                }
                Log.WriteLine($"Found becnhmark {descriptor.ElementDescription}");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Exception {ex}");
                return false;
            }
        }

        private async Task<BenchmarkData?> RequestDataAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken token)
        {
            if (!descriptorContext.ApplicableSpan.HasValue)
            {
                var msg = $"{nameof(CodeLensDescriptorContext)} without span";
                Log.LogError(msg);
                throw new ArgumentException(msg);
            }
            BenchmarkData? isBenchmark;
            if (descriptorContext.TryGetRoslyInfos(descriptor, out var request))
            {
                Log.WriteLine($"Found roslyn request {ProjectId.CreateFromSerialized(request!.ProjectId, request.ManagedMethod)}");
                isBenchmark = await this.service
                    .GetBenchmarkDataAsync(request, token)
                    .ConfigureAwait(false);

            }
            else
            {
                Log.WriteLine($"Roslyn request not found for project {descriptor.ProjectGuid}");
                isBenchmark = await this.service
                    .GetBenchmarkData2Async(descriptor.ProjectGuid,
                    descriptor.FilePath,
                    descriptorContext.ApplicableSpan.Value.Start,
                    descriptorContext.ApplicableSpan.Value.End,
                    token)
                    .ConfigureAwait(false);
            }
            return isBenchmark;
        }

        public async Task<IAsyncCodeLensDataPoint?> CreateDataPointAsync(CodeLensDescriptor descriptor,
                                                                  CodeLensDescriptorContext descriptorContext,
                                                                  CancellationToken token)
        {
            try
            {
                BenchmarkMethodTracker tracker;
                var element = new DescriptorCacheItem(descriptor, descriptorContext);
                lock (this.trackers)
                {
                    trackers.TryGetValue(element, out tracker);
                }
                if (tracker is not null)
                {
                    Log.WriteLine($"Successfully retrieved benchmark tracker {descriptor.ElementDescription}");
                }
                else
                {
                    Log.WriteLine($"Creating new datapoint for '{descriptor.ProjectGuid}/{descriptor.ElementDescription}/{descriptor.Kind}' ({descriptor.FilePath}:{descriptorContext.ApplicableSpan})");
                    if (descriptorContext.ApplicableSpan is null)
                    {
                        Log.LogError("Call with invalid span");
                        return null!;
                    }

                    var infos = await this
                        .service
                        .GetBenchmarkData2Async(
                        descriptor.ProjectGuid,
                        descriptor.FilePath,
                        descriptorContext.ApplicableSpan.Value.Start,
                        descriptorContext.ApplicableSpan.Value.End,
                        token)
                    .ConfigureAwait(false);
                    if (!infos.HasValue)
                    {
                        Log.LogError("Call to non benchmark method");
                        return null;
                    }

                    tracker = new(infos.Value);
                }
                var dataPoint = new CodeLensDataPoint(descriptor,
                tracker);

                lock (this.trackers)
                    this.trackers.Remove(element);
                return dataPoint;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error while creating data point: {ex}");
                return null;
            }
        }
    }
}