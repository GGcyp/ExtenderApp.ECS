using ExtenderApp.ECS.Queries;
using System;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统创建阶段（OnCreate）传入的上下文，用于在系统创建时对当前 World 进行只读访问和必要的初始化操作。
    ///
    /// 提供便捷的方法：创建实体、批量创建实体、获取或构建实体查询（EntityQuery/EntityQueryBuilder）、以及访问共享组件。
    /// 注意：OnCreate 通常在主线程执行。本上下文为轻量包装，不建议长期保存；若需长期访问 World，建议在 OnCreate 中缓存必要的查询或句柄。
    /// </summary>
    public readonly struct SystemCreateContext
    {
        private readonly World _world;

        /// <summary>
        /// 创建 SystemCreateContext 的实例。
        /// </summary>
        /// <param name="world">当前 World 实例（不得为 null）。</param>
        internal SystemCreateContext(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        #region Entity
        /// <summary>        /// 在当前 World 中创建一个空实体并返回实体句柄。等同于调用 World.CreateEntity()。        /// </summary>        public Entity CreateEntity()            => _world.CreateEntity();        /// <summary>        /// 使用指定组件掩码创建实体并返回实体句柄。等同于调用 World.CreateEntity(ComponentMask)。        /// </summary>        public Entity CreateEntity(ComponentMask mask)            => _world.CreateEntity(mask);        /// <summary>        /// 创建一个仅包含单个组件并写入初始值的实体。等同于调用 World.CreateEntity(T1 component)。        /// </summary>        public Entity CreateEntity<T>(T component)            => _world.CreateEntity(component);        /// <summary>        /// 批量创建实体并将结果写入指定跨度（用于高效批量创建）。等同于调用 World.CreateEntity(Span<Entity>)。        /// </summary>        public void CreateEntities(Span<Entity> entities)            => _world.CreateEntity(entities);        #endregion Entity        #region Query        /// <summary>        /// 获取或创建单组件实体查询（等同于 World.Query&lt;T1&gt;()）。        /// </summary>        public EntityQuery<T> Query<T>()            => _world.Query<T>();        /// <summary>        /// 获取或创建两个组件组合的实体查询（等同于 World.Query&lt;T1,T2&gt;()）。        /// </summary>        public EntityQuery<T1, T2> Query<T1, T2>()            => _world.Query<T1, T2>();        /// <summary>        /// 获取或创建三个组件组合的实体查询（等同于 World.Query&lt;T1,T2,T3&gt;()）。        /// </summary>        public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()            => _world.Query<T1, T2, T3>();        /// <summary>        /// 获取或创建四个组件组合的实体查询（等同于 World.Query&lt;T1,T2,T3,T4&gt;()）。        /// </summary>        public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()            => _world.Query<T1, T2, T3, T4>();        /// <summary>        /// 获取或创建五个组件组合的实体查询（等同于 World.Query&lt;T1,T2,T3,T4,T5&gt;()）。        /// </summary>        public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()            => _world.Query<T1, T2, T3, T4, T5>();        /// <summary>        /// 返回一个 EntityQueryBuilder，用于在创建阶段构建复杂查询（With/WithAll/WithNone 等）。        /// </summary>        public EntityQueryBuilder QueryBuilder()            => _world.QueryBuilder();        /// <summary>        /// 在 OnCreate 中通过泛型参数直接构建单组件的查询构造器（等同于 World.With&lt;T1&gt;()）。        /// </summary>        public EntityQueryBuilder<T> With<T>()            => _world.With<T>();        /// <summary>        /// 构建包含两个组件类型的查询构造器（等同于 World.With&lt;T1,T2&gt;()）。        /// </summary>        public EntityQueryBuilder<T1, T2> With<T1, T2>()            => _world.With<T1, T2>();        /// <summary>        /// 构建包含三个组件类型的查询构造器（等同于 World.With&lt;T1,T2,T3&gt;()）。        /// </summary>        public EntityQueryBuilder<T1, T2, T3> With<T1, T2, T3>()            => _world.With<T1, T2, T3>();        /// <summary>        /// 构建包含四个组件类型的查询构造器（等同于 World.With&lt;T1,T2,T3,T4&gt;()）。        /// </summary>        public EntityQueryBuilder<T1, T2, T3, T4> With<T1, T2, T3, T4>()            => _world.With<T1, T2, T3, T4>();        /// <summary>        /// 构建包含五个组件类型的查询构造器（等同于 World.With&lt;T1,T2,T3,T4,T5&gt;()）。        /// </summary>        public EntityQueryBuilder<T1, T2, T3, T4, T5> With<T1, T2, T3, T4, T5>()            => _world.With<T1, T2, T3, T4, T5>();        #endregion Query        #region SharedComponent        /// <summary>        /// 获取当前 World 中的共享组件实例（若存在）。共享组件常用于全局状态或配置数据的跨系统共享。        /// </summary>        public T GetSharedComponent<T>()            => _world.GetSharedComponent<T>();        /// <summary>        /// 尝试获取当前 World 中的共享组件实例；存在时返回 true 并通过 out 参数输出组件值，否则返回 false。        /// </summary>        public bool TryGetSharedComponent<T>(out T component)            => _world.TryGetSharedComponent(out component);        #endregion SharedComponent    }}