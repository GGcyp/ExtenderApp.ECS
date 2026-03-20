using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的组件相关 API 的局部实现。
    /// 提供对实体组件的便捷操作接口（获取/设置/添加/移除），这些方法均在主线程执行并内联转发到低层 ComponentBuffer。
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// 在指定实体上设置或覆盖某个组件的值并返回一个可用于链式操作的 ComponentBuffer。
        /// 若实体或组件列不存在会抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要写入的组件值。</param>
        /// <returns>用于链式调用的 <see cref="ComponentBuffer"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentBuffer SetComponent<T>(Entity entity, T component) where T : struct, IComponent
            => GetComponentBuffer(entity).SetComponent(component);

        /// <summary>
        /// 获取指定实体的组件值。
        /// 若对应组件不存在则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>指定组件的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(Entity entity) where T : struct, IComponent
            => GetComponentBuffer(entity).GetComponent<T>();

        /// <summary>
        /// 获取指定实体的组件值（带存在性检查）。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="value">输出参数，用于返回组件值。</param>
        /// <returns>用于链式调用的 <see cref="ComponentBuffer"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentBuffer GetComponent<T>(Entity entity, out T value) where T : struct, IComponent
            => GetComponentBuffer(entity).GetComponent<T>(out value);

        /// <summary>
        /// 向指定实体添加一个组件（若已存在则覆盖）。
        /// 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <param name="component">要添加的组件值。</param>
        /// <returns>用于链式调用的 <see cref="ComponentBuffer"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentBuffer AddComponent<T>(Entity entity, T component) where T : struct, IComponent
            => GetComponentBuffer(entity).AddComponent(component);

        /// <summary>
        /// 从指定实体移除某个组件（若存在）。
        /// 可能会触发实体从一个 Archetype 迁移到另一个 Archetype。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>用于链式调用的 <see cref="ComponentBuffer"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentBuffer RemoveComponent<T>(Entity entity) where T : struct, IComponent
            => GetComponentBuffer(entity).RemoveComponent<T>();

        /// <summary>
        /// 创建一个指向指定实体的轻量 ComponentBuffer 实例（内部使用）。
        /// </summary>
        /// <param name="entity">目标实体句柄。</param>
        /// <returns>初始化的 <see cref="ComponentBuffer"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ComponentBuffer GetComponentBuffer(Entity entity)
            => new ComponentBuffer(this, entity);
    }
}