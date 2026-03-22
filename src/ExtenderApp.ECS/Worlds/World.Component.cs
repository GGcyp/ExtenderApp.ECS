using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的组件相关 API 的局部实现。 提供对实体组件的便捷操作接口（获取/设置/添加/移除），这些方法均在主线程执行并内联转发到低层 EntityComponentOperation。
    /// </summary>
    public partial class World
    {
        #region EntityComponentOperation

        /// <summary>
        /// 在指定实体上设置或覆盖某个组件的值并返回一个可用于链式操作的 EntityComponentOperation。 若实体或组件列不存在会抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要写入的组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation SetComponent<T>(Entity entity, T component) where T : struct
            => GetEntityComponentOperation(entity).SetComponent(component);

        /// <summary>
        /// 获取指定实体的组件值。 若对应组件不存在则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>指定组件的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(Entity entity) where T : struct
            => GetEntityComponentOperation(entity).GetComponent<T>();

        /// <summary>
        /// 获取指定实体的组件值（带存在性检查）。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="value">输出参数，用于返回组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation GetComponent<T>(Entity entity, out T value) where T : struct
            => GetEntityComponentOperation(entity).GetComponent<T>(out value);

        /// <summary>
        /// 向指定实体添加一个组件（若已存在则覆盖）。 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要添加的组件值。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddComponent<T>(Entity entity, T component) where T : struct
            => GetEntityComponentOperation(entity).AddComponent(component);

        /// <summary>
        /// 从指定实体移除某个组件（若存在）。 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveComponent<T>(Entity entity) where T : struct
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
        /// 获取一个新的 ArchetypeBuilder 实例，用于构建 ArchetypeChunk。 该 Builder 内部持有对当前 World 的 ArchetypeManager 的引用，允许用户通过链式调用添加或移除组件和关系来定义 ArchetypeChunk 的结构。
        /// 生成的 ArchetypeChunk 可用于批量创建实体或迁移实体到新结构。 该方法在主线程执行并内联以最大化性能。
        /// </summary>
        /// <returns>返回一个 <see cref="ArchetypeBuilder" /> 实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder CreateArchetypeChunkBuilder()
            => new(ArchetypeManager);

        #endregion ArchetypeChunBuilder
    }
}