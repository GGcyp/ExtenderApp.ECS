using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ExtenderApp.ECS;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 多线程并行任务调度器。
    /// </summary>
    internal sealed class ParallelJobManager : DisposableObject
    {
        private readonly bool _workersEnabled;
        private readonly Channel<ParallelJobWorkItem>? _workItemChannel;
        private readonly ConcurrentBag<Task>? _workerTasks;
        private readonly CancellationTokenSource? _cts;

        /// <summary>
        /// 未完成作业数归零时置为有信号，由零变正时复位；供同步等待阻塞而非纯自旋。
        /// </summary>
        private readonly ManualResetEventSlim? _allJobsDrained;

        private int _outstandingJobs;

        /// <summary>
        /// 为 true 时 <see cref="AddWorkItem" /> 会将作业交给后台 worker；为 false 时调用会抛出异常。
        /// </summary>
        public bool WorkersEnabled => _workersEnabled;

        /// <summary>
        /// 当前未完成的作业数。
        /// </summary>
        public int OutstandingJobs => _outstandingJobs;

        public ParallelJobManager(bool enableBackgroundWorkers = true)
        {
            _workersEnabled = enableBackgroundWorkers;
            if (!_workersEnabled)
                return;

            _cts = new();
            _workerTasks = new();
            _allJobsDrained = new ManualResetEventSlim(initialState: true);
            _workItemChannel = Channel.CreateUnbounded<ParallelJobWorkItem>(new()
            {
                SingleWriter = true,
                SingleReader = false,
                AllowSynchronousContinuations = false,
            });

            int workerCount = Math.Max(1, Settings.MaxParallelProcessorCount);
            for (int i = 0; i < workerCount; i++)
                CreateNewWorker();
        }

        private void CreateNewWorker()
        {
            var workerTask = Task.Factory.StartNew(
                WorkerLoop,
                _cts!.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            _workerTasks!.Add(workerTask);
        }

        private async Task WorkerLoop()
        {
            var reader = _workItemChannel!.Reader;
            var ct = _cts!.Token;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                        break;

                    while (reader.TryRead(out var task))
                        ExecuteAndCompleteWorkItem(task);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ExecuteAndCompleteWorkItem(ParallelJobWorkItem task)
        {
            try
            {
                task.Execute();
            }
            finally
            {
                task.Retrun();
                if (Interlocked.Decrement(ref _outstandingJobs) == 0)
                    _allJobsDrained!.Set();
            }
        }

        public void AddWorkItem(ParallelJobWorkItem workItem)
        {
            if (!_workersEnabled)
            {
                throw new InvalidOperationException(
                    "当前 World 未启用并行作业调度。请使用 WorldOptions 配置 WorldParallelJobsMode.PerWorldWorkers，或不要调度 IParallelSystem 工作。");
            }

            int newOutstanding = Interlocked.Increment(ref _outstandingJobs);
            if (newOutstanding == 1)
                _allJobsDrained!.Reset();

            if (!_workItemChannel!.Writer.TryWrite(workItem))
            {
                if (Interlocked.Decrement(ref _outstandingJobs) == 0)
                    _allJobsDrained!.Set();
                throw new InvalidOperationException("未能将作业入队。");
            }
        }

        /// <summary>
        /// 阻塞直至已入队作业全部执行完毕。等待期间主线程会 TryRead 并执行作业以协助排空；无就绪作业时在事件上短时阻塞。
        /// </summary>
        internal void WaitUntilJobsCompleted()
        {
            if (!_workersEnabled)
                return;

            var reader = _workItemChannel!.Reader;
            var sw = Stopwatch.StartNew();
            const int timeoutMs = 60_000;

            while (Volatile.Read(ref _outstandingJobs) != 0)
            {
                while (reader.TryRead(out var task))
                    ExecuteAndCompleteWorkItem(task);

                if (Volatile.Read(ref _outstandingJobs) == 0)
                    return;

                int elapsed = (int)sw.ElapsedMilliseconds;
                if (elapsed >= timeoutMs)
                {
                    throw new TimeoutException(
                        $"并行作业在 {timeoutMs} ms 内未完成（未完成数={Volatile.Read(ref _outstandingJobs)}）。");
                }

                int waitMs = Math.Min(250, timeoutMs - elapsed);
                _allJobsDrained!.Wait(waitMs);
            }
        }

        protected override void DisposeManagedResources()
        {
            if (!_workersEnabled)
                return;

            _workItemChannel!.Writer.Complete();
            _cts!.Cancel();
            try
            {
                Task.WhenAll(_workerTasks!).Wait();
            }
            catch
            {
            }

            _allJobsDrained?.Dispose();
        }
    }
}