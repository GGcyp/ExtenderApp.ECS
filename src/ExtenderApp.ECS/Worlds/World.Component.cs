using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// CurrentWorld 的组件相关 API 的局部实现。 提供对实体组件的便捷操作接口（获取/设置/添加/移除），这些方法均在主线程执行并内联转发到低层 EntityComponentOperation。
    /// </summary>
    public partial class World
    {
        #region EntityComponentOperation

        /// <summary>
        /// 在指定实体上设置或覆盖某个组件的值并返回一个可用于链式操作的 EntityComponentOperation。 若实体或组件列不存在会抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要写入的组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation SetComponent<T>(Entity entity, T component)
            => GetEntityComponentOperation(entity).SetComponent(component);

        /// <summary>
        /// 获取指定实体的组件值。 若对应组件不存在则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>指定组件的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(Entity entity)
            => GetEntityComponentOperation(entity).GetComponent<T>();

        /// <summary>
        /// 获取指定实体的组件值（带存在性检查）。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="value">输出参数，用于返回组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation GetComponent<T>(Entity entity, out T value)
            => GetEntityComponentOperation(entity).GetComponent<T>(out value);

        /// <summary>
        /// 向指定实体添加一个组件（若已存在则覆盖）。 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要添加的组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddComponent<T>(Entity entity, T component)
            => GetEntityComponentOperation(entity).AddComponent(component);

        /// <summary>
        /// 向指定实体添加一个组件。 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddComponent<T>(Entity entity)
            => GetEntityComponentOperation(entity).AddComponent<T>();

        /// <summary>
        /// 从指定实体移除某个组件（若存在）。 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveComponent<T>(Entity entity)
            => GetEntityComponentOperation(entity).RemoveComponent<T>();

        /// <summary>
        /// 创建一个指向指定实体的轻量 EntityComponentOperation 实例（内部使用）。
        /// </summary>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>初始化的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EntityComponentOperation GetEntityComponentOperation(Entity entity)
            => new EntityComponentOperation(this, entity);

        #endregion EntityComponentOperation

        #region ArchetypeChunBuilder

        /// <summary>
        /// 获取一个新的 ArchetypeBuilder 实例，用于构建 ArchetypeChunk。 该 Builder 内部持有对当前 CurrentWorld 的 ArchetypeManager 的引用，允许用户通过链式调用添加或移除组件和关系来定义 ArchetypeChunk 的结构。
        /// 生成的 ArchetypeChunk 可用于批量创建实体或迁移实体到新结构。 该方法在主线程执行并内联以最大化性能。
        /// </summary>
        /// <returns>返回一个 <see cref="ArchetypeBuilder" /> 实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder CreateArchetypeChunkBuilder()
            => new(ArchetypeManager);

        #endregion ArchetypeChunBuilder

        #region SharedComponent

        /// <summary>
        /// 将指定类型的共享组件写入世界（创建或覆盖）。
        /// 写入操作仅允许在主线程执行。
        /// </summary>
        /// <typeparam name="T">共享组件的值类型（struct）。</typeparam>
        /// <param name="value">要写入的组件值（按 in 传递以避免不必要拷贝）。</param>
        /// <remarks>
        /// 建议在主线程的确定性同步点（例如每帧开始、系统组边界或帧末）进行更新，
        /// 以便并行系统在整个阶段内读取到一致的快照数据。
        /// </remarks>
        public void SetSharedComponent<T>(in T value)
        {
            ThrowIfNotMainThread();
            SharedComponentManager.Set(value);
        }

        /// <summary>
        /// 尝试读取指定类型的共享组件值。
        /// </summary>
        /// <typeparam name="T">共享组件的值类型（struct）。</typeparam>
        /// <param name="value">当返回 true 时输出对应的组件值；若返回 false 则为默认值。</param>
        /// <returns>若存在则返回 true；否则返回 false。</returns>
        /// <remarks>读取为线程安全的只读操作，可在任意线程调用（前提是写入仅在主线程）。</remarks>
        public bool TryGetSharedComponent<T>(out T value)
        => SharedComponentManager.TryGet(out value);

        /// <summary>
        /// 获取指定类型的共享组件值（若不存在则抛出异常）。
        /// </summary>
        /// <typeparam name="T">共享组件的值类型（struct）。</typeparam>
        /// <returns>存在的共享组件值。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的共享组件不存在时抛出。</exception>
        public T GetSharedComponent<T>()
            => SharedComponentManager.Get<T>();

        /// <summary>
        /// 判断世界中是否存在指定类型的共享组件。
        /// </summary>
        /// <typeparam name="T">共享组件的值类型（struct）。</typeparam>
        /// <returns>存在则返回 true；否则返回 false。</returns>
        public bool HasSharedComponent<T>()
            => SharedComponentManager.Has<T>();

        /// <summary>
        /// 从世界中移除指定类型的共享组件（仅主线程允许）。
        /// </summary>
        /// <typeparam name="T">共享组件的值类型（struct）。</typeparam>
        /// <returns>若组件存在并成功移除则返回 true；否则返回 false。</returns>
        /// <remarks>移除操作会回收底层非托管存储，应在主线程的同步点执行。</remarks>
        public bool RemoveSharedComponent<T>()
        {
            ThrowIfNotMainThread();
            return SharedComponentManager.Remove<T>();
        }

        /// <summary>
        /// 尝试新增一个共享组件（若已存在则返回 false）。
        /// </summary>
        /// <param name="value">要添加的共享组件值（按 in 传递以避免不必要拷贝）。</param>
        public bool TryAddSharedComponent<T>(in T value)
        {
            ThrowIfNotMainThread();
            return SharedComponentManager.TryAddComponent(value);
        }

        /// <summary>
        /// 新增一个共享组件；若已存在则抛出异常。
        /// </summary>
        /// <param name="value">要添加的共享组件值（按 in 传递以避免不必要拷贝）。</param>
        public void AddSharedComponent<T>(in T value)
        {
            ThrowIfNotMainThread();
            if (!SharedComponentManager.TryAddComponent(value))
                throw new InvalidOperationException($"Shared component of type {typeof(T)} already exists.");
        }

        /// <summary>
        /// 尝试更新已存在的共享组件（若不存在则返回 false）。
        /// </summary>
        /// <param name="value">要更新的共享组件值（按 in 传递以避免不必要拷贝）。</param>
        public bool TryUpdateSharedComponent<T>(in T value)
        {
            ThrowIfNotMainThread();
            return SharedComponentManager.TrySetComponent(value);
        }

        /// <summary>
        /// 更新已存在的共享组件；若不存在则抛出异常。
        /// </summary>
        /// <param name="value">要更新的共享组件值（按 in 传递以避免不必要拷贝）。</param>
        public void UpdateSharedComponent<T>(in T value)
        {
            ThrowIfNotMainThread();
            if (!SharedComponentManager.TrySetComponent(value))
                throw new KeyNotFoundException($"Shared component of type {typeof(T)} does not exist.");
        }

        #endregion SharedComponent
    }
}