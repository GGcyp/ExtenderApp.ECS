using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的实体相关 API 的局部实现。
    /// 提供实体的创建与销毁操作，并在主线程上执行安全检查。
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// 使用指定组件掩码创建一个实体，并返回实体句柄。
        /// </summary>
        /// <param name="mask">组件掩码，表示实体要包含的组件集合。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(ComponentMask mask)
        {
            ThrowIfNotMainThread();
            if (mask.IsEmpty)
                throw new ArgumentNullException(nameof(mask));

            Archetype archetype = ArchetypeManager.GetOrCreateArchetype(mask);
            return EntityManager.CreateEntity(archetype);
        }

        /// <summary>
        /// 使用指定 Archetype 创建一个实体。
        /// </summary>
        /// <param name="archetype">目标 Archetype。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(Archetype archetype)
        {
            ThrowIfNotMainThread();
            ArgumentNullException.ThrowIfNull(archetype, nameof(archetype));

            return EntityManager.CreateEntity(archetype);
        }

        /// <summary>
        /// 创建一个不带组件的空实体，并返回其句柄。
        /// </summary>
        /// <returns>新创建的空实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity()
        {
            ThrowIfNotMainThread();
            return EntityManager.CreateEntity();
        }

        /// <summary>
        /// 销毁指定实体，回收其资源并从所属 Archetype 中移除对应槽位。
        /// </summary>
        /// <param name="entity">要销毁的实体句柄。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(Entity entity)
        {
            ThrowIfNotMainThread();
            EntityManager.DestroyEntity(entity);
        }
    }
}