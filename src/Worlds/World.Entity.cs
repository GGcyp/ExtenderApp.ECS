using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// CurrentWorld 的实体相关 API 的局部实现。 提供实体的创建与销毁操作，并在主线程上执行安全检查。
    /// </summary>
    public partial class World
    {
        #region Create

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

            Archetype archetype = AManager.GetOrCreateArchetype(mask);
            return EManager.CreateEntity(archetype);
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

            return CreateEntityPrivate(archetype);
        }

        /// <summary>
        /// 内部实体创建入口：在已解析到目标 Archetype 后创建实体。
        /// </summary>
        /// <param name="archetype">目标 Archetype。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreateEntityPrivate(Archetype archetype)
        {
            return EManager.CreateEntity(archetype);
        }

        /// <summary>
        /// 创建一个不带组件的空实体，并返回其句柄。
        /// </summary>
        /// <returns>新创建的空实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity()
        {
            ThrowIfNotMainThread();
            return EManager.CreateEntity();
        }

        /// <summary>
        /// 批量创建实体并写入到指定跨度。
        /// </summary>
        /// <param name="entities">用于接收创建结果的实体跨度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateEntity(Span<Entity> entities)
        {
            ThrowIfNotMainThread();
            if (entities.IsEmpty)
                return;
            EManager.CreateEntity(entities);
        }

        /// <summary>
        /// 创建一个仅包含单个组件的实体，并写入该组件初始值。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="component">组件初始值。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T>(T component)
        {
            ThrowIfNotMainThread();

            Archetype archetype = AManager.GetOrCreateArchetype(ComponentType.Create<T>());
            var entity = CreateEntityPrivate(archetype);
            SetComponent(entity, component);
            return entity;
        }

        /// <summary>
        /// 创建一个包含两个组件的实体，并写入组件初始值。
        /// </summary>
        /// <typeparam name="T1">组件类型 1。</typeparam>
        /// <typeparam name="T2">组件类型 2。</typeparam>
        /// <param name="component1">组件 1 初始值。</param>
        /// <param name="component2">组件 2 初始值。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T1, T2>(T1 component1, T2 component2)
        {
            ThrowIfNotMainThread();
            ComponentMask mask = new()
            {
                ComponentType.Create<T1>(),
                ComponentType.Create<T2>()
            };

            Archetype archetype = AManager.GetOrCreateArchetype(mask);
            var entity = CreateEntityPrivate(archetype);
            SetComponent(entity, component1)
                .SetComponent(component2);
            return entity;
        }

        /// <summary>
        /// 创建一个包含三个组件的实体，并写入组件初始值。
        /// </summary>
        /// <typeparam name="T1">组件类型 1。</typeparam>
        /// <typeparam name="T2">组件类型 2。</typeparam>
        /// <typeparam name="T3">组件类型 3。</typeparam>
        /// <param name="component1">组件 1 初始值。</param>
        /// <param name="component2">组件 2 初始值。</param>
        /// <param name="component3">组件 3 初始值。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T1, T2, T3>(T1 component1, T2 component2, T3 component3)
        {
            ThrowIfNotMainThread();
            ComponentMask mask = new()
            {
                ComponentType.Create<T1>(),
                ComponentType.Create<T2>(),
                ComponentType.Create<T3>(),
            };

            Archetype archetype = AManager.GetOrCreateArchetype(mask);
            var entity = CreateEntityPrivate(archetype);
            SetComponent(entity, component1)
                .SetComponent(component2)
                .SetComponent(component3);
            return entity;
        }

        /// <summary>
        /// 创建一个包含四个组件的实体，并写入组件初始值。
        /// </summary>
        /// <typeparam name="T1">组件类型 1。</typeparam>
        /// <typeparam name="T2">组件类型 2。</typeparam>
        /// <typeparam name="T3">组件类型 3。</typeparam>
        /// <typeparam name="T4">组件类型 4。</typeparam>
        /// <param name="component1">组件 1 初始值。</param>
        /// <param name="component2">组件 2 初始值。</param>
        /// <param name="component3">组件 3 初始值。</param>
        /// <param name="component4">组件 4 初始值。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4)
        {
            ThrowIfNotMainThread();
            ComponentMask mask = new()
            {
                ComponentType.Create<T1>(),
                ComponentType.Create<T2>(),
                ComponentType.Create<T3>(),
                ComponentType.Create<T4>(),
            };

            Archetype archetype = AManager.GetOrCreateArchetype(mask);
            var entity = CreateEntityPrivate(archetype);
            SetComponent(entity, component1)
                .SetComponent(component2)
                .SetComponent(component3)
                .SetComponent(component4);
            return entity;
        }

        /// <summary>
        /// 创建一个包含五个组件的实体，并写入组件初始值。
        /// </summary>
        /// <typeparam name="T1">组件类型 1。</typeparam>
        /// <typeparam name="T2">组件类型 2。</typeparam>
        /// <typeparam name="T3">组件类型 3。</typeparam>
        /// <typeparam name="T4">组件类型 4。</typeparam>
        /// <typeparam name="T5">组件类型 5。</typeparam>
        /// <param name="component1">组件 1 初始值。</param>
        /// <param name="component2">组件 2 初始值。</param>
        /// <param name="component3">组件 3 初始值。</param>
        /// <param name="component4">组件 4 初始值。</param>
        /// <param name="component5">组件 5 初始值。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5)
        {
            ThrowIfNotMainThread();
            ComponentMask mask = new()
            {
                ComponentType.Create<T1>(),
                ComponentType.Create<T2>(),
                ComponentType.Create<T3>(),
                ComponentType.Create<T4>(),
                ComponentType.Create<T5>(),
            };

            Archetype archetype = AManager.GetOrCreateArchetype(mask);
            var entity = CreateEntityPrivate(archetype);
            SetComponent(entity, component1)
                .SetComponent(component2)
                .SetComponent(component3)
                .SetComponent(component4)
                .SetComponent(component5);
            return entity;
        }

        #endregion Create

        #region Destroy

        /// <summary>
        /// 销毁指定实体，回收其资源并从所属 Archetype 中移除对应槽位。 不级联删除子实体（关系目标指向它的实体保持悬挂）。
        /// </summary>
        /// <param name="entity">要销毁的实体句柄。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(Entity entity)
        {
            DestroyEntity(entity, false);
        }

        /// <summary>
        /// 销毁指定实体。 cascade 为 true 时递归级联删除所有子实体（任一关系类型的目标实体等于该实体的实体）。
        /// </summary>
        /// <param name="entity">要销毁的实体句柄。</param>
        /// <param name="cascade">是否级联删除子实体，默认 false（子实体保持悬挂）。</param>
        public void DestroyEntity(Entity entity, bool cascade)
        {
            ThrowIfNotMainThread();
            if (cascade)
                DestroyEntityCascade(entity);
            else
                EManager.DestroyEntity(entity);
        }

        /// <summary>
        /// 批量销毁实体（不级联）。
        /// </summary>
        /// <param name="entities">要销毁的实体集合。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(ReadOnlySpan<Entity> entities)
        {
            DestroyEntity(entities, false);
        }

        /// <summary>
        /// 批量销毁实体。 cascade 为 true 时对每个实体递归级联删除子实体（先删自身再删子实体，互指环天然终止，无需防环集合）。
        /// </summary>
        /// <param name="entities">要销毁的实体集合。</param>
        /// <param name="cascade">是否级联删除子实体，默认 false。</param>
        public void DestroyEntity(ReadOnlySpan<Entity> entities, bool cascade)
        {
            ThrowIfNotMainThread();
            if (!cascade)
            {
                EManager.DestroyEntity(entities);
                return;
            }

            foreach (var entity in entities)
                DestroyEntityCascade(entity);
        }

        /// <summary>
        /// 级联删除核心：先收集子实体（关系目标指向当前实体的存活实体）并删除自身，再逐个递归级联删除子实体。
        /// 「先删自身」的顺序使互指环天然终止：环中实体被删后，另一侧收集时它已不在任何表中，不会再次被枚举。
        /// 无需 IsAlive 检查：删除已销毁实体是安全 no-op（EntityManager 内部按版本校验拦截）。
        /// 注意：对已销毁实体调用级联删除会清理仍悬挂指向它的实体（行内目标值未清除，仍会被收集）。
        /// IsEmpty 短路保留：关系目标列未写入时的默认值即 Entity.Empty，防止误把「未设置目标」的行收集进来。
        /// 收集使用局部列表：遍历期间删除行会破坏段迭代，必须先收集再删（级联删除为低频操作，此分配可接受）。
        /// </summary>
        private void DestroyEntityCascade(Entity entity)
        {
            if (entity.IsEmpty)
                return;

            var children = new List<Entity>();
            foreach (var child in GetChildren(entity))
                children.Add(child);

            EManager.DestroyEntity(entity);

            foreach (var child in children)
                DestroyEntityCascade(child);
        }

        #endregion Destroy

        /// <summary>
        /// 获取指定实体的组件访问器，用于直接访问和修改实体的组件数据。 该方法会执行安全检查，确保实体有效且未销毁。
        /// </summary>
        /// <param name="entity">指定实体句柄。</param>
        /// <returns>返回实体的组件访问器。</returns>
        /// <exception cref="Exception">当实体无效或已销毁时抛出异常。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentLookup GetEntityComponentLookup(Entity entity)
        {
            if (EManager.TryGetArchetype(entity, out var archetype, out int archetypeIndex) &&
               (archetype?.TryGetComponentHandle(archetypeIndex, out var handle) ?? false))
            {
                return new(handle);
            }

            throw new Exception("未找到实体的组件句柄，可能实体无效或已销毁。");
        }
    }
}