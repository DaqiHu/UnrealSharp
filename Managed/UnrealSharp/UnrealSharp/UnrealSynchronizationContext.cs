using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp
{
    public enum NamedThread
    {
        UnusedAnchor = -1,

        /** The always-present, named threads are listed next **/
        RHIThread,
        GameThread,

        // The render thread is sometimes the game thread and is sometimes the actual rendering thread
        ActualRenderingThread = GameThread + 1,
        // CAUTION ThreadedRenderingThread must be the last named thread, insert new named threads before it

        /** not actually a thread index. Means "Unknown Thread" or "Any Unnamed Thread" **/
        AnyThread = 0xff,

        /** High bits are used for a queue index and priority**/

        MainQueue = 0x000,
        LocalQueue = 0x100,

        NumQueues = 2,
        ThreadIndexMask = 0xff,
        QueueIndexMask = 0x100,
        QueueIndexShift = 8,

        /** High bits are used for a queue index task priority and thread priority**/

        NormalTaskPriority = 0x000,
        HighTaskPriority = 0x200,

        NumTaskPriorities = 2,
        TaskPriorityMask = 0x200,
        TaskPriorityShift = 9,

        NormalThreadPriority = 0x000,
        HighThreadPriority = 0x400,
        BackgroundThreadPriority = 0x800,

        NumThreadPriorities = 3,
        ThreadPriorityMask = 0xC00,
        ThreadPriorityShift = 10,

        /** Combinations **/
        GameThread_Local = GameThread | LocalQueue,
        ActualRenderingThread_Local = ActualRenderingThread | LocalQueue,

        AnyHiPriThreadNormalTask = AnyThread | HighThreadPriority | NormalTaskPriority,
        AnyHiPriThreadHiPriTask = AnyThread | HighThreadPriority | HighTaskPriority,

        AnyNormalThreadNormalTask = AnyThread | NormalThreadPriority | NormalTaskPriority,
        AnyNormalThreadHiPriTask = AnyThread | NormalThreadPriority | HighTaskPriority,

        AnyBackgroundThreadNormalTask = AnyThread | BackgroundThreadPriority | NormalTaskPriority,
        AnyBackgroundHiPriTask = AnyThread | BackgroundThreadPriority | HighTaskPriority,
    };

    public static class UnrealContextTaskExtension
    {
        public static Task ConfigureWithUnrealContext(this Task task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            var unrealContext = new UnrealSynchronizationContext(thread);
            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.ContinueWith((Task t, object? state) =>
                {
                    SynchronizationContext newPreviousContext = (SynchronizationContext) state!;
                    try
                    {
                        if (t.IsCanceled && throwOnCancel)
                        {
                            throw new TaskCanceledException();
                        }
                        
                        t.GetAwaiter().GetResult();
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(newPreviousContext);
                    }
                },
                previousContext,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        public static Task ConfigureWithUnrealContext(this ValueTask task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false) 
            => task.AsTask().ConfigureWithUnrealContext(thread, throwOnCancel);

        public static Task<T> ConfigureWithUnrealContext<T>(this Task<T> task, NamedThread thread = NamedThread.GameThread, bool throwOnCancel = false)
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            UnrealSynchronizationContext unrealContext = new UnrealSynchronizationContext(thread);
            SynchronizationContext.SetSynchronizationContext(unrealContext);

            return task.ContinueWith((t, state) =>
                {
                    SynchronizationContext newPreviousContext = (SynchronizationContext) state!;
                    try
                    {
                        if (t.IsCanceled && throwOnCancel)
                        {
                            throw new TaskCanceledException();
                        }
                        
                        return t.GetAwaiter().GetResult();
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(newPreviousContext);
                    }
                },
                previousContext,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        public static Task<T> ConfigureWithUnrealContext<T>(this ValueTask<T> task, NamedThread thread = NamedThread.GameThread,
            bool throwOnCancel = false)
            => task.AsTask().ConfigureWithUnrealContext(thread, throwOnCancel);
    }
    
    public sealed class UnrealSynchronizationContext : SynchronizationContext
    {
        public static NamedThread CurrentThread => (NamedThread)AsyncExporter.CallGetCurrentNamedThread();

        private readonly NamedThread _thread;
        private readonly nint _worldContext;

        public UnrealSynchronizationContext(NamedThread thread)
        {
            _thread = thread;
            _worldContext = FCSManagerExporter.CallGetCurrentWorldContext();
        }

        public override void Post(SendOrPostCallback d, object? state) => RunOnThread(_worldContext, _thread, () => d(state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (CurrentThread == _thread)
            {
                d(state);
                return;
            }

            using ManualResetEventSlim manualResetEventInstance = new ManualResetEventSlim(false);
            
            RunOnThread(_worldContext, _thread, () =>
            {
                try
                {
                    d(state);
                }
                finally
                {
                    manualResetEventInstance.Set();
                }
            });
            manualResetEventInstance.Wait();
        }

        internal static void RunOnThread(nint worldContextObject, NamedThread thread, Action callback)
        {
            GCHandle callbackHandle = GCHandle.Alloc(callback);
            AsyncExporter.CallRunOnThread(worldContextObject, (int) thread, GCHandle.ToIntPtr(callbackHandle));
        }

        public static void RunOnThread(UObject worldContextObject, NamedThread thread, Action callback)
            => RunOnThread(worldContextObject.NativeObject, thread, callback);
    }
}
