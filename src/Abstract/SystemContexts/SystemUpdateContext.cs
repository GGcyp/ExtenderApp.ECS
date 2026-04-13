using ExtenderApp.ECS.Commands;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Systems;
using ExtenderApp.ECS.Systems.Parallels;
using ExtenderApp.ECS.Threading;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统每帧更新时传入的上下文，在 <c>OnUpdate</c> 等回调中用于读取时间、构建查询、写入命令缓冲与访问共享组件等。
    /// </summary>
    public struct SystemUpdateContext
    {
        /// <summary>
        /// 本帧与上一帧之间的时间间隔（秒）。
        /// </summary>
        public readonly float DeltaTime;

        /// <summary>
        /// 自世界开始以来的累计时间（秒）。
        /// </summary>
        public readonly double Time;

        /// <summary>
        /// 当前帧序号，从 0 起递增。
        /// </summary>
        public readonly ulong FrameIndex;

        /// <summary>
        /// 共享组件的注册与查找。
        /// </summary>
        private readonly SharedComponentManager _sharedComponentManager;

        /// <summary>
        /// 实体查询的缓存与构建入口。
        /// </summary>
        private readonly EntityQueryManager _entityQueryManager;

        /// <summary>
        /// 并行系统作业队列与调度。
        /// </summary>
        private readonly ParallelJobManager _parallelJobManager;

        /// <summary>
        /// 本帧延迟执行的实体创建、销毁与组件变更命令。
        /// </summary>
        public EntityCommandBuffer CommandBuffer;

        /// <summary>
        /// 由框架在更新阶段构造，注入共享组件、查询、命令缓冲、并行调度以及时间与帧信息。
        /// </summary>
        internal SystemUpdateContext(SharedComponentManager sharedComponentManager, EntityQueryManager entityQueryManager, EntityCommandBuffer commandBuffer, ParallelJobManager parallelJobManager, float deltaTime, double time, ulong frameIndex)
        {
            _sharedComponentManager = sharedComponentManager;
            _entityQueryManager = entityQueryManager;
            _parallelJobManager = parallelJobManager;

            CommandBuffer = commandBuffer;
            DeltaTime = deltaTime;
            Time = time;
            FrameIndex = frameIndex;
        }

        private void ThrowIfMainThreadAccess()
        {
            if (!MainThreadDetector.IsMainThread())
                throw new InvalidOperationException("SystemUpdateContext can only be accessed from the main thread.");
        }

        #region SharedComponent

        /// <summary>
        /// 获取类型 <typeparamref name="T" /> 的共享组件实例。
        /// </summary>
        public T GetSharedComponent<T>()
            => _sharedComponentManager.Get<T>();

        /// <summary>
        /// 尝试获取类型 <typeparamref name="T" /> 的共享组件，返回是否已注册。
        /// </summary>
        public bool TryGetSharedComponent<T>(out T component)
            => _sharedComponentManager.TryGet(out component);

        #endregion SharedComponent

        #region Query

        /// <summary>
        /// 构建仅要求具备 <typeparamref name="T" /> 的实体查询；等价于 <see cref="With{T}" />(). <see cref="EntityQueryBuilder{T}.Build" />()。
        /// </summary>
        public EntityQuery<T> Query<T>() => With<T>().Build();

        /// <summary>
        /// 构建要求同时具备 <typeparamref name="T1" />、 <typeparamref name="T2" /> 的实体查询。
        /// </summary>
        public EntityQuery<T1, T2> Query<T1, T2>()
            => With<T1, T2>().Build();

        /// <summary>
        /// 构建要求同时具备 T1～T3 的实体查询。
        /// </summary>
        public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
            => With<T1, T2, T3>().Build();

        /// <summary>
        /// 构建要求同时具备 T1～T4 的实体查询。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            => With<T1, T2, T3, T4>().Build();

        /// <summary>
        /// 构建要求同时具备 T1～T5 的实体查询。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
            => With<T1, T2, T3, T4, T5>().Build();

        /// <summary>
        /// 返回无类型参数的 <see cref="EntityQueryBuilder" />，用于 <c>WithAll</c> / <c>WithAny</c> / <c>WithNone</c> / <c>WithRelation</c> 等复杂条件后再构建查询。
        /// </summary>
        public EntityQueryBuilder QueryBuilder()
            => new(_entityQueryManager);

        /// <summary>
        /// 开始链式构建：要求实体具备 <typeparamref name="T" />，可继续链接更多 With 或过滤条件后调用 <see cref="EntityQueryBuilder{T}.Build" />。
        /// </summary>
        public EntityQueryBuilder<T> With<T>()
            => new(_entityQueryManager);

        /// <summary>
        /// 开始链式构建：要求实体同时具备 T1、T2。
        /// </summary>
        public EntityQueryBuilder<T1, T2> With<T1, T2>()
            => new(_entityQueryManager);

        /// <summary>
        /// 开始链式构建：要求实体同时具备 T1～T3。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3> With<T1, T2, T3>()
            => new(_entityQueryManager);

        /// <summary>
        /// 开始链式构建：要求实体同时具备 T1～T4。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3, T4> With<T1, T2, T3, T4>()
            => new(_entityQueryManager);

        /// <summary>
        /// 开始链式构建：要求实体同时具备 T1～T5。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3, T4, T5> With<T1, T2, T3, T4, T5>()
            => new(_entityQueryManager);

        #endregion Query

        #region ParallelJob

        /// <summary>
        /// 注册并行系统：查询由「仅含 T1」的默认构建器生成，系统状态为 <c>default</c>。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>()
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(new EntityQueryBuilder<T1>(_entityQueryManager).BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 注册并行系统：查询由「仅含 T1」的默认构建器生成，并传入 <paramref name="system" /> 作为系统状态。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>(in TSystem system)
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(new EntityQueryBuilder<T1>(_entityQueryManager).BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1、T2，系统状态为 <c>default</c>。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>()
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(new EntityQueryBuilder<T1, T2>(_entityQueryManager).BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1、T2，并传入 <paramref name="system" />。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>(in TSystem system)
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(new EntityQueryBuilder<T1, T2>(_entityQueryManager).BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1～T3，系统状态为 <c>default</c>。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>()
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(new EntityQueryBuilder<T1, T2, T3>(_entityQueryManager).BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1～T3，并传入 <paramref name="system" />。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>(in TSystem system)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(new EntityQueryBuilder<T1, T2, T3>(_entityQueryManager).BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1～T4，系统状态为 <c>default</c>。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>()
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(new EntityQueryBuilder<T1, T2, T3, T4>(_entityQueryManager).BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1～T4，并传入 <paramref name="system" />。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>(in TSystem system)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(new EntityQueryBuilder<T1, T2, T3, T4>(_entityQueryManager).BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 注册并行系统：查询覆盖 T1～T5，系统状态为 <c>default</c>。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>()
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(new EntityQueryBuilder<T1, T2, T3, T4, T5>(_entityQueryManager).BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 注册并行系统：查询覆盖 <typeparamref name="T1" /> 至 <typeparamref name="T5" />，并传入 <paramref name="system" />。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>(in TSystem system)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(new EntityQueryBuilder<T1, T2, T3, T4, T5>(_entityQueryManager).BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件（WithAll / WithAny / WithNone / WithRelation）注册并行系统。须在驱动 World 更新的同一线程上调用（与 <see
        /// cref="ParallelJobManager" /> 单写者语义一致）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>(EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(queryBuilder.BuildEntityQueryCore<T1>(), this, default);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册并行系统（带系统状态）。须在驱动 World 更新的同一线程上调用。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>(in TSystem system, EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(queryBuilder.BuildEntityQueryCore<T1>(), this, system);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册双组件并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>(EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(queryBuilder.BuildEntityQueryCore<T1, T2>(), this, default);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册双组件并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>(in TSystem system, EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(queryBuilder.BuildEntityQueryCore<T1, T2>(), this, system);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册三组件并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>(EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(queryBuilder.BuildEntityQueryCore<T1, T2, T3>(), this, default);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册三组件并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>(in TSystem system, EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(queryBuilder.BuildEntityQueryCore<T1, T2, T3>(), this, system);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册四组件并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>(EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(queryBuilder.BuildEntityQueryCore<T1, T2, T3, T4>(), this, default);

        /// <summary>
        /// 使用与 <see cref="QueryBuilder" /> 相同的流式条件注册四组件并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>(in TSystem system, EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(queryBuilder.BuildEntityQueryCore<T1, T2, T3, T4>(), this, system);

        /// <summary>
        /// 使用与 <see cref="With{T}" /> 链相同的流式条件注册五组件并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>(EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(queryBuilder.BuildEntityQueryCore<T1, T2, T3, T4, T5>(), this, default);

        /// <summary>
        /// 使用与 <see cref="With{T}" /> 链相同的流式条件注册五组件并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>(in TSystem system, EntityQueryBuilder queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(queryBuilder.BuildEntityQueryCore<T1, T2, T3, T4, T5>(), this, system);

        /// <summary>
        /// 使用已配置的 <see cref="EntityQueryBuilder{T}" />（含 WithAll / WithAny / WithNone / WithRelation）注册并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>(EntityQueryBuilder<T1> queryBuilder)
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(queryBuilder.BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 使用已配置的 <see cref="EntityQueryBuilder{T}" /> 注册并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1>(in TSystem system, EntityQueryBuilder<T1> queryBuilder)
            where TSystem : struct, IParallelSystem<T1>
            => _parallelJobManager.AddWorkItem<TSystem, T1>(queryBuilder.BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 使用已配置的双组件查询构建器注册并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>(EntityQueryBuilder<T1, T2> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(queryBuilder.BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 使用已配置的双组件查询构建器注册并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2>(in TSystem system, EntityQueryBuilder<T1, T2> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2>(queryBuilder.BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 使用已配置的三组件查询构建器注册并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>(EntityQueryBuilder<T1, T2, T3> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(queryBuilder.BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 使用已配置的三组件查询构建器注册并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3>(in TSystem system, EntityQueryBuilder<T1, T2, T3> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3>(queryBuilder.BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 使用已配置的四组件查询构建器注册并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>(EntityQueryBuilder<T1, T2, T3, T4> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(queryBuilder.BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 使用已配置的四组件查询构建器注册并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4>(in TSystem system, EntityQueryBuilder<T1, T2, T3, T4> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4>(queryBuilder.BuildEntityQueryCore(), this, system);

        /// <summary>
        /// 使用已配置的五组件查询构建器注册并行系统。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>(EntityQueryBuilder<T1, T2, T3, T4, T5> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(queryBuilder.BuildEntityQueryCore(), this, default);

        /// <summary>
        /// 使用已配置的五组件查询构建器注册并行系统（带系统状态）。
        /// </summary>
        public void AddParallelSystem<TSystem, T1, T2, T3, T4, T5>(in TSystem system, EntityQueryBuilder<T1, T2, T3, T4, T5> queryBuilder)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
            => _parallelJobManager.AddWorkItem<TSystem, T1, T2, T3, T4, T5>(queryBuilder.BuildEntityQueryCore(), this, system);

        #endregion ParallelJob
    }
}