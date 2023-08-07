﻿namespace FrameworkTests
{
    internal class TestDatas
    {
        #region StackTrace1
        public const string SanitizedStackTrace = @" ---> System.NullReferenceException: Object reference not set to an instance of an object.
   at Benchmarks.MonitorBenchmarks.Bounded_Schedule_TaskRun() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\MonitorBenchmarks.cs:line 35";
        public const string StackTrace = @"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.NullReferenceException: Object reference not set to an instance of an object.
   at Benchmarks.MonitorBenchmarks.Bounded_Schedule_TaskRun() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\MonitorBenchmarks.cs:line 35
   at BenchmarkDotNet.Autogenerated.Runnable_1.WorkloadActionUnroll(Int64 invokeCount) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\bin\Debug\net7.0\f6565232-b245-45ee-b5cd-b3a47bf0b259\f6565232-b245-45ee-b5cd-b3a47bf0b259.notcs:line 443
   at BenchmarkDotNet.Engines.Engine.RunIteration(IterationData data)
   at BenchmarkDotNet.Engines.EngineStage.RunIteration(IterationMode mode, IterationStage stage, Int32 index, Int64 invokeCount, Int32 unrollFactor)
   at BenchmarkDotNet.Engines.EngineActualStage.RunSpecific(Int64 invokeCount, IterationMode iterationMode, Int32 iterationCount, Int32 unrollFactor)
   at BenchmarkDotNet.Engines.EngineActualStage.Run(Int64 invokeCount, IterationMode iterationMode, Boolean runAuto, Int32 unrollFactor, Boolean forceSpecific)
   at BenchmarkDotNet.Engines.EngineActualStage.RunWorkload(Int64 invokeCount, Int32 unrollFactor, Boolean forceSpecific)
   at BenchmarkDotNet.Engines.Engine.Run()
   at BenchmarkDotNet.Autogenerated.Runnable_1.Run(IHost host, String benchmarkName) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\bin\Debug\net7.0\f6565232-b245-45ee-b5cd-b3a47bf0b259\f6565232-b245-45ee-b5cd-b3a47bf0b259.notcs:line 350
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
   --- End of inner exception stack trace ---
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)
   at BenchmarkDotNet.Autogenerated.UniqueProgramName.AfterAssemblyLoadingAttached(String[] args) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\bin\Debug\net7.0\f6565232-b245-45ee-b5cd-b3a47bf0b259\f6565232-b245-45ee-b5cd-b3a47bf0b259.notcs:line 57";
        #endregion StackTrace1

        #region StackTrace2

        public const string StackTrace2 = @"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.AggregateException: One or more errors occurred. (Context 'System.Object' already bound to context 4 (Request by 5)) (Context 'System.Object' already bound to context 4 (Request by 6)) (Context 'System.Object' already bound to context 4 (Request by 7)) (Context 'System.Object' already bound to context 4 (Request by 8)) (Context 'System.Object' already bound to context 4 (Request by 9)) (Context 'System.Object' already bound to context 4 (Request by 10))
 ---> System.Exception: Context 'System.Object' already bound to context 4 (Request by 5)
   at Sodiware.Threading.Tasks.LockTaskScheduler.bind(Object transaction, Worker worker) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\LockTaskScheduler.cs:line 753
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.set_Context(Object value) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 149
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.ExecuteTasks(Object parameterObject) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 207
--- End of stack trace from previous location ---
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.<throwException>b__52_0(Object _) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 484
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
--- End of stack trace from previous location ---
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.throwException() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 483
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 475
   at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)
   --- End of inner exception stack trace ---
   at System.Threading.Tasks.Task.ThrowIfExceptional(Boolean includeTaskCanceledExceptions)
   at System.Threading.Tasks.Task.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
   at System.Threading.Tasks.Task.Wait()
   at Benchmarks.RunBenchmarks.Bounded_schedule() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\RunBenchmarks.cs:line 51
   at BenchmarkDotNet.Autogenerated.Runnable_0.WorkloadActionUnroll(Int64 invokeCount) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\bin\Debug\net7.0\21e5cb7d-e632-4af3-a17a-79b643cb4dca\21e5cb7d-e632-4af3-a17a-79b643cb4dca.notcs:line 276
   at BenchmarkDotNet.Engines.Engine.RunIteration(IterationData data)
   at BenchmarkDotNet.Engines.EngineStage.RunIteration(IterationMode mode, IterationStage stage, Int32 index, Int64 invokeCount, Int32 unrollFactor)
   at BenchmarkDotNet.Engines.EngineActualStage.RunSpecific(Int64 invokeCount, IterationMode iterationMode, Int32 iterationCount, Int32 unrollFactor)
   at BenchmarkDotNet.Engines.EngineActualStage.Run(Int64 invokeCount, IterationMode iterationMode, Boolean runAuto, Int32 unrollFactor, Boolean forceSpecific)
   at BenchmarkDotNet.Engines.EngineActualStage.RunWorkload(Int64 invokeCount, Int32 unrollFactor, Boolean forceSpecific)
   at BenchmarkDotNet.Engines.Engine.Run()
   at BenchmarkDotNet.Autogenerated.Runnable_0.Run(IHost host, String benchmarkName) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\bin\Debug\net7.0\21e5cb7d-e632-4af3-a17a-79b643cb4dca\21e5cb7d-e632-4af3-a17a-79b643cb4dca.notcs:line 183
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
 ---> (Inner Exception #1) System.Exception: Context 'System.Object' already bound to context 4 (Request by 6)
   at Sodiware.Threading.Tasks.LockTaskScheduler.bind(Object transaction, Worker worker) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\LockTaskScheduler.cs:line 753
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.set_Context(Object value) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 149
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.ExecuteTasks(Object parameterObject) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 207
--- End of stack trace from previous location ---
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.<throwException>b__52_0(Object _) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 484
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
--- End of stack trace from previous location ---
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.throwException() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 483
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 475
   at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)<---";

        public const string SanitizedStackTrace2 = @" ---> System.AggregateException: One or more errors occurred. (Context 'System.Object' already bound to context 4 (Request by 5)) (Context 'System.Object' already bound to context 4 (Request by 6)) (Context 'System.Object' already bound to context 4 (Request by 7)) (Context 'System.Object' already bound to context 4 (Request by 8)) (Context 'System.Object' already bound to context 4 (Request by 9)) (Context 'System.Object' already bound to context 4 (Request by 10))
 ---> System.Exception: Context 'System.Object' already bound to context 4 (Request by 5)
   at Sodiware.Threading.Tasks.LockTaskScheduler.bind(Object transaction, Worker worker) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\LockTaskScheduler.cs:line 753
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.set_Context(Object value) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 149
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.ExecuteTasks(Object parameterObject) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 207
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.<throwException>b__52_0(Object _) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 484
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.throwException() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 483
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 475
   at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)
   --- End of inner exception stack trace ---
   at System.Threading.Tasks.Task.ThrowIfExceptional(Boolean includeTaskCanceledExceptions)
   at System.Threading.Tasks.Task.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
   at System.Threading.Tasks.Task.Wait()
   at Benchmarks.RunBenchmarks.Bounded_schedule() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Tests\Benchmarks\RunBenchmarks.cs:line 51
 ---> (Inner Exception #1) System.Exception: Context 'System.Object' already bound to context 4 (Request by 6)
   at Sodiware.Threading.Tasks.LockTaskScheduler.bind(Object transaction, Worker worker) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\LockTaskScheduler.cs:line 753
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.set_Context(Object value) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 149
   at Sodiware.Threading.Tasks.LockTaskScheduler.Worker.ExecuteTasks(Object parameterObject) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\Worker.cs:line 207
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.<throwException>b__52_0(Object _) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 484
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.throwException() in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 483
   at Sodiware.Threading.Tasks.LockTaskScheduler.SchedulerTaskCompletionSourceCore`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token) in F:\Sources\Sodiware\Netlogon\src\Framework\src\Core\Sodiware\Threading\Tasks\SchedulerTaskCompletionSource.cs:line 475
   at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)<---";
        #endregion StackTrace2

    }
}
