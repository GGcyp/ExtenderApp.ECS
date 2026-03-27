using ExtenderApp.ECS.Commands;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Queries;
using System;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统运行阶段（OnUpdate）传入的上下文（SystemUpdateContext）。
    ///
    /// 提供对运行时所需资源的访问：共享组件管理器、实体查询管理器以及用于记录结构性修改的实体命令缓冲区。
    /// 系统在 OnUpdate 中应通过本上下文读取共享数据、构建或使用实体查询，并通过 CommandBuffer 延迟提交结构性变更以保证线程安全。
    /// </summary>
    public struct SystemUpdateContext
    {
        private readonly SharedComponentManager _sharedComponentManager;
        private readonly EntityQueryManager _entityQueryManager;

        /// <summary>
        /// 实体命令缓冲区（EntityCommandBuffer）。
        /// 用于在系统执行期间记录对实体/组件的结构性更改（新增/移除组件、创建/销毁实体等），
        /// 由调度器在适当时机（通常为帧末或同步点）统一回放以保证线程安全。
        /// </summary>
        public EntityCommandBuffer CommandBuffer;

        /// <summary>
        /// 创建一个 SystemUpdateContext 实例（内部使用）。
        /// </summary>
        /// <param name="sharedComponentManager">共享组件管理器，用于访问或查询共享组件。</param>
        /// <param name="entityQueryManager">实体查询管理器，用于构建与获取 EntityQuery。</param>
        /// <param name="commandBuffer">实体命令缓冲区，用于记录结构性变更。</param>
        internal SystemUpdateContext(SharedComponentManager sharedComponentManager, EntityQueryManager entityQueryManager, EntityCommandBuffer commandBuffer)
        {
            _sharedComponentManager = sharedComponentManager;
            _entityQueryManager = entityQueryManager;
            CommandBuffer = commandBuffer;
        }

        #region SharedComponent

        /// <summary>
        /// 获取当前 World 中的共享组件实例（用于运行时读取全局/共享状态）。
        /// </summary>
        public T GetSharedComponent<T>()
            => _sharedComponentManager.Get<T>();

        /// <summary>
        /// 尝试获取当前 World 中的共享组件实例；存在时返回 true 并通过 out 参数输出组件值，否则返回 false。
        /// </summary>
        public bool TryGetSharedComponent<T>(out T component)
            => _sharedComponentManager.TryGet(out component);

        #endregion SharedComponent

        #region Query

        /// <summary>
        /// 获取或创建单组件实体查询（等同于 World.Query&lt;T&gt;()）。
        /// </summary>
        public EntityQuery<T> Query<T>()
            => With<T>().Build();

        /// <summary>
        /// 获取或创建两个组件组合的实体查询（等同于 World.Query&lt;T,T2&gt;()）。
        /// </summary>
        public EntityQuery<T1, T2> Query<T1, T2>()
            => With<T1, T2>().Build();

        /// <summary>
        /// 获取或创建三个组件组合的实体查询（等同于 World.Query&lt;T,T2,T3&gt;()）。
        /// </summary>
        public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
            => With<T1, T2, T3>().Build();

        /// <summary>
        /// 获取或创建四个组件组合的实体查询（等同于 World.Query&lt;T,T2,T3,T4&gt;()）。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            => With<T1, T2, T3, T4>().Build();

        /// <summary>
        /// 获取或创建五个组件组合的实体查询（等同于 World.Query&lt;T,T2,T3,T4,T5&gt;()）。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
            => With<T1, T2, T3, T4, T5>().Build();

        /// <summary>
        /// 返回一个 EntityQueryBuilder，用于在系统运行阶段（OnUpdate）构建复杂查询（如 With/WithAll/WithNone 等）。
        /// </summary>
        public EntityQueryBuilder QueryBuilder()
            => new(_entityQueryManager);

        /// <summary>
        /// 在 OnUpdate 中通过泛型参数构建单组件的查询构造器（等同于 World.With&lt;T&gt;()）。
        /// </summary>
        public EntityQueryBuilder<T> With<T>()
            => new(_entityQueryManager);

        /// <summary>
        /// 在 OnUpdate 中构建包含两个组件类型的查询构造器（等同于 World.With&lt;T,T2&gt;()）。
        /// </summary>
        public EntityQueryBuilder<T1, T2> With<T1, T2>()
            => new(_entityQueryManager);

        /// <summary>
        /// 在 OnUpdate 中构建包含三个组件类型的查询构造器（等同于 World.With&lt;T,T2,T3&gt;()）。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3> With<T1, T2, T3>()
            => new(_entityQueryManager);

        /// <summary>
        /// 在 OnUpdate 中构建包含四个组件类型的查询构造器（等同于 World.With&lt;T,T2,T3,T4&gt;()）。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3, T4> With<T1, T2, T3, T4>()
            => new(_entityQueryManager);

        /// <summary>
        /// 在 OnUpdate 中构建包含五个组件类型的查询构造器（等同于 World.With&lt;T,T2,T3,T4,T5&gt;()）。
        /// </summary>
        public EntityQueryBuilder<T1, T2, T3, T4, T5> With<T1, T2, T3, T4, T5>()
            => new(_entityQueryManager);

        #endregion Query
    }
}