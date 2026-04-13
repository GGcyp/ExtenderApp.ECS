using System.Collections.Concurrent;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Systems.Parallels
{
    /// <summary>
    /// 并行系统工作项提供者基类。 提供者负责维护工作项对象池以及工作项的租用与归还上限配置。
    /// </summary>
    internal abstract class ParallelSystemProvider
    {
        /// <summary>
        /// 工作项池的默认最大容量。
        /// </summary>
        protected const int DefaultMaxWorkItemCount = 100;
        
        /// <summary>
        /// 工作项池的最大容量，超过该数量的新归还项不会被入队。
        /// </summary>
        protected int MaxWorkItemCount { get; set; }

        /// <summary>
        /// 按系统类型缓存的提供者实例字典。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ParallelSystemProvider> dict = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        protected ParallelSystemProvider()
        {
            MaxWorkItemCount = DefaultMaxWorkItemCount;
        }

        /// <summary>
        /// 获取指定系统类型对应的提供者实例。
        /// </summary>
        /// <typeparam name="TSystem">系统类型。</typeparam>
        /// <returns>对应的 <see cref="ParallelSystemProvider{T}" /> 实例。</returns>
        public static ParallelSystemProvider<TSystem> Get<TSystem>() where TSystem : struct, IParallelSystem
        {
            return (ParallelSystemProvider<TSystem>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem>());
        }

        /// <summary>
        /// 获取指定泛型系统类型对应的提供者实例（1 个组件泛型）。
        /// </summary>
        public static ParallelSystemProvider<TSystem, T1> Get<TSystem, T1>() where TSystem : struct, IParallelSystem<T1>
        {
            return (ParallelSystemProvider<TSystem, T1>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem, T1>());
        }

        /// <summary>
        /// 获取指定泛型系统类型对应的提供者实例（2 个组件泛型）。
        /// </summary>
        public static ParallelSystemProvider<TSystem, T1, T2> Get<TSystem, T1, T2>() where TSystem : struct, IParallelSystem<T1, T2>
        {
            return (ParallelSystemProvider<TSystem, T1, T2>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem, T1, T2>());
        }

        /// <summary>
        /// 获取指定泛型系统类型对应的提供者实例（3 个组件泛型）。
        /// </summary>
        public static ParallelSystemProvider<TSystem, T1, T2, T3> Get<TSystem, T1, T2, T3>() where TSystem : struct, IParallelSystem<T1, T2, T3>
        {
            return (ParallelSystemProvider<TSystem, T1, T2, T3>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem, T1, T2, T3>());
        }

        /// <summary>
        /// 获取指定泛型系统类型对应的提供者实例（4 个组件泛型）。
        /// </summary>
        public static ParallelSystemProvider<TSystem, T1, T2, T3, T4> Get<TSystem, T1, T2, T3, T4>() where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
        {
            return (ParallelSystemProvider<TSystem, T1, T2, T3, T4>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem, T1, T2, T3, T4>());
        }

        /// <summary>
        /// 获取指定泛型系统类型对应的提供者实例（5 个组件泛型）。
        /// </summary>
        public static ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5> Get<TSystem, T1, T2, T3, T4, T5>() where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
        {
            return (ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5>)dict.GetOrAdd(typeof(TSystem), _ => new ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5>());
        }
    }

    /// <summary>
    /// 并行系统提供者（无泛型参数）。 负责维护对应系统类型的工作项对象池，并提供租用与归还接口。
    /// </summary>
    /// <typeparam name="TSystem"></typeparam>
    internal sealed class ParallelSystemProvider<TSystem> : ParallelSystemProvider where TSystem : struct, IParallelSystem
    {
        /// <summary>
        /// 可复用的工作项队列。
        /// </summary>
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem>> _workQueue;

        /// <summary>
        /// 构造函数，创建内部工作项队列。
        /// </summary>
        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        /// <summary>
        /// 租用一个工作项并用指定查询进行初始化。 若队列中存在已回收的工作项则直接复用，否则创建新的工作项。
        /// </summary>
        /// <param name="query">要处理的实体查询结果。</param>
        /// <param name="updateContext">系统更新上下文。</param>
        /// <returns>初始化好的工作项实例。</returns>
        public ParallelSystemWorkItem<TSystem> Rent(JobEntityQuery query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        /// <summary>
        /// 将工作项返回到队列中以便后续复用。 若队列已达最大容量则忽略该归还操作。
        /// </summary>
        /// <param name="parallelSystemWorkItem">要归还的工作项。</param>
        internal void Return(ParallelSystemWorkItem<TSystem> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
            {
                _workQueue.Enqueue(parallelSystemWorkItem);
            }
        }
    }

    /// <summary>
    /// 并行系统提供者（1 个泛型参数）。 负责维护对应系统类型的工作项对象池，并提供租用与归还接口。
    /// </summary>
    internal sealed class ParallelSystemProvider<TSystem, T1> : ParallelSystemProvider where TSystem : struct, IParallelSystem<T1>
    {
        /// <summary>
        /// 可复用的工作项队列。
        /// </summary>
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem, T1>> _workQueue;

        /// <summary>
        /// 构造函数，创建内部工作项队列。
        /// </summary>
        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        /// <summary>
        /// 租用一个工作项并用指定泛型查询进行初始化。 若队列中存在已回收的工作项则直接复用，否则创建新的工作项。
        /// </summary>
        /// <param name="query">要处理的泛型实体查询结果。</param>
        /// <param name="updateContext">系统更新上下文。</param>
        /// <returns>初始化好的工作项实例。</returns>
        public ParallelSystemWorkItem<TSystem, T1> Rent(JobEntityQuery<T1> query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        /// <summary>
        /// 将工作项返回到队列中以便后续复用。 若队列已达最大容量则忽略该归还操作。
        /// </summary>
        /// <param name="parallelSystemWorkItem">要归还的工作项。</param>
        internal void Return(ParallelSystemWorkItem<TSystem, T1> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
            {
                _workQueue.Enqueue(parallelSystemWorkItem);
            }
        }
    }

    /// <summary>
    /// 并行系统提供者（2 个组件类型泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemProvider<TSystem, T1, T2> : ParallelSystemProvider where TSystem : struct, IParallelSystem<T1, T2>
    {
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem, T1, T2>> _workQueue;

        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        public ParallelSystemWorkItem<TSystem, T1, T2> Rent(JobEntityQuery<T1, T2> query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        internal void Return(ParallelSystemWorkItem<TSystem, T1, T2> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
                _workQueue.Enqueue(parallelSystemWorkItem);
        }
    }

    /// <summary>
    /// 并行系统提供者（3 个组件类型泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemProvider<TSystem, T1, T2, T3> : ParallelSystemProvider where TSystem : struct, IParallelSystem<T1, T2, T3>
    {
        /// <summary>
        /// 可复用的工作项队列
        /// </summary>
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem, T1, T2, T3>> _workQueue;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        /// <summary>
        /// 获取一个租用的工作项并初始化查询
        /// </summary>
        /// <param name="query">要处理的泛型实体查询结果。</param>
        /// <param name="updateContext">系统更新上下文。</param>
        /// <returns>初始化好的工作项实例。</returns>
        public ParallelSystemWorkItem<TSystem, T1, T2, T3> Rent(JobEntityQuery<T1, T2, T3> query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        /// <summary>
        /// 将工作项返回池中
        /// </summary>
        internal void Return(ParallelSystemWorkItem<TSystem, T1, T2, T3> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
            {
                _workQueue.Enqueue(parallelSystemWorkItem);
            }
        }
    }

    /// <summary>
    /// 并行系统提供者（4 个泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemProvider<TSystem, T1, T2, T3, T4> : ParallelSystemProvider where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
    {
        /// <summary>
        /// 可复用的工作项队列
        /// </summary>
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem, T1, T2, T3, T4>> _workQueue;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        /// <summary>
        /// 获取一个租用的工作项并初始化查询
        /// </summary>
        /// <param name="query">要处理的泛型实体查询结果。</param>
        /// <param name="updateContext">系统更新上下文。</param>
        /// <returns>初始化好的工作项实例。</returns>
        public ParallelSystemWorkItem<TSystem, T1, T2, T3, T4> Rent(JobEntityQuery<T1, T2, T3, T4> query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        /// <summary>
        /// 将工作项返回池中
        /// </summary>
        internal void Return(ParallelSystemWorkItem<TSystem, T1, T2, T3, T4> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
            {
                _workQueue.Enqueue(parallelSystemWorkItem);
            }
        }
    }

    /// <summary>
    /// 并行系统提供者（5 个泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5> : ParallelSystemProvider where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
    {
        /// <summary>
        /// 可复用的工作项队列
        /// </summary>
        private readonly ConcurrentQueue<ParallelSystemWorkItem<TSystem, T1, T2, T3, T4, T5>> _workQueue;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemProvider()
        {
            _workQueue = new();
        }

        /// <summary>
        /// 获取一个租用的工作项并初始化查询
        /// </summary>
        /// <param name="query">要处理的泛型实体查询结果。</param>
        /// <param name="updateContext">系统更新上下文。</param>
        /// <returns>初始化好的工作项实例。</returns>
        public ParallelSystemWorkItem<TSystem, T1, T2, T3, T4, T5> Rent(JobEntityQuery<T1, T2, T3, T4, T5> query, SystemUpdateContext updateContext, TSystem system = default)
        {
            if (_workQueue.TryDequeue(out var workItem))
            {
                workItem.Init(query, updateContext, system);
                return workItem;
            }

            workItem = new(this);
            workItem.Init(query, updateContext, system);
            return workItem;
        }

        /// <summary>
        /// 将工作项返回池中
        /// </summary>
        internal void Return(ParallelSystemWorkItem<TSystem, T1, T2, T3, T4, T5> parallelSystemWorkItem)
        {
            if (_workQueue.Count < MaxWorkItemCount)
            {
                _workQueue.Enqueue(parallelSystemWorkItem);
            }
        }
    }
}