using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Threading;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 按实体访问组件的轻量封装。提供获取、设置、添加与移除组件的便捷方法。 该结构体为只读值类型，包含对所属 World 与 Entity 的引用；调用前会检查主线程。
    /// </summary>
    public struct EntityComponentOperation
    {
        /// <summary>
        /// 当前 ComponentBuffer 所属的 World 实例，提供访问 EntityManager 与 ArchetypeManager 的入口。
        /// </summary>
        private readonly World _world;

        /// <summary>
        /// 当前实体的 Entity 句柄，标识了该 ComponentBuffer 操作的目标实体。通过 EntityManager 获取或修改组件数据。
        /// </summary>
        private readonly Entity _entity;

        /// <summary>
        /// 当前实体所在的 Archetype 与在该 Archetype 中的索引。 该字段用于优化连续访问同一实体组件时避免重复查询 Archetype 信息。 在迁移到新 Archetype 后会更新该字段以保持正确性。
        /// </summary>
        private Archetype? currentArchetype;

        /// <summary>
        /// 当前实体所在 Archetype 中的索引。 该字段与 currentArchetype 配合使用，指示实体在当前 Archetype 中的位置。 在迁移到新 Archetype 后会更新该字段以保持正确性。
        /// </summary>
        private int currentArchetypeIndex;

        /// <summary>
        /// 当前 World 中的 EntityManager 实例，提供获取实体 Archetype、迁移实体等功能。
        /// </summary>
        private EntityManager Entities => _world.EntityManager;

        /// <summary>
        /// 当前 World 中的 ArchetypeManager 实例，提供获取或创建 Archetype 的功能。
        /// </summary>
        private ArchetypeManager Components => _world.ArchetypeManager;

        /// <summary>
        /// 使用指定 World 与 Entity 构造一个 ComponentBuffer 实例（内部使用）。
        /// </summary>
        /// <param name="world">所属的 World 实例。</param>
        /// <param name="entity">目标实体句柄。</param>
        internal EntityComponentOperation(World world, Entity entity)
        {
            _world = world;
            _entity = entity;
        }

        /// <summary>
        /// 设置实体在其当前 Archetype 中已存在的组件的值。 若该组件列不存在会抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="component">要写入的组件值。</param>
        /// <returns>返回自身以支持链式调用（轻量便捷）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation SetComponent<T>(T component) where T : struct
        {
            ThrowIfNotMainThread();
            if (!TryGetArchetype(out var archetype, out var archetypeIndex) ||
                archetype == null)
                throw new InvalidOperationException("实体无效或已被销毁。");

            archetype.SetComponent(archetypeIndex, component);
            return this;
        }

        /// <summary>
        /// 获取实体在其当前 Archetype 中已存在的组件的值。 若该组件列不存在会抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <returns>指定组件的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>() where T : struct
        {
            ThrowIfNotMainThread();
            if (!TryGetArchetype(out var archetype, out var archetypeIndex) ||
                archetype == null)
                throw new InvalidOperationException("实体无效或已被销毁。");

            return archetype.GetComponent<T>(archetypeIndex);
        }

        /// <summary>
        /// 获取实体在其当前 Archetype 中已存在的组件的值，并通过输出参数返回。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="value">输出参数，用于返回组件的值。</param>
        /// <returns>返回自身以支持链式调用（轻量便捷）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation GetComponent<T>(out T value) where T : struct
        {
            ThrowIfNotMainThread();
            if (!TryGetArchetype(out var archetype, out var archetypeIndex) ||
                archetype == null)
                throw new InvalidOperationException("实体无效或已被销毁。");

            value = archetype.GetComponent<T>(archetypeIndex);
            return this;
        }

        /// <summary>
        /// 向实体添加一个新组件（可能导致实体迁移到新的 Archetype）。 如果实体已经包含该组件则直接覆盖组件的值。
        /// </summary>
        /// <typeparam name="T">组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="component">要添加或覆盖的组件值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddComponent<T>(T component) where T : struct
        {
            ThrowIfNotMainThread();

            var compType = ComponentType.Create<T>();
            if (!TryGetArchetype(out var archetype, out var archetypeIndex) ||
                archetype == null)
            {
                ComponentMask componentMask = new(compType);
                archetype = BuildArchetype(componentMask);
            }
            else
            {
                if (archetype.ComponentTypes.On(compType))
                {
                    archetype.SetComponent(archetypeIndex, component);
                    return this;
                }

                var newMask = archetype.ComponentTypes;
                newMask.Add(compType);
                archetype = BuildArchetype(newMask, archetype.RelationTypes);
            }

            var newIndex = archetype.AddEntity(_entity);
            ChangedArchetype(archetype, newIndex);
            archetype.SetComponent(newIndex, component);
            return this;
        }

        /// <summary>
        /// 从实体中移除指定组件（可能导致实体迁移到新的 Archetype）。 若组件不存在则忽略。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveComponent<T>() where T : struct
        {
            ThrowIfNotMainThread();

            if (!TryGetArchetype(out var archetype, out var archetypeIndex) ||
                archetype == null)
                return this; // 实体无效或已被销毁，忽略

            var compType = ComponentType.Create<T>();
            if (!archetype.ComponentTypes.On(compType))
                return this; // 不存在，忽略

            var newMask = archetype.ComponentTypes;
            newMask.Remove(compType);

            if (newMask.IsEmpty)
            {
                ChangedArchetype(null, -1); // 无组件 Archetype 视为 null
                return this;
            }

            Archetype newArchetype = Components.GetOrCreateArchetype(newMask);
            var newIndex = newArchetype.AddEntity(_entity);
            ChangedArchetype(newArchetype, newIndex);
            return this;
        }

        /// <summary>
        /// 向实体添加关系。 若当前 Archetype 已包含该关系类型，则直接覆盖目标实体； 否则会迁移到包含该关系类型的新 Archetype。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">目标实体。</param>
        /// <returns>返回自身以支持链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddRelation(RelationType relationType, Entity target)
        {
            ThrowIfNotMainThread();

            if (!TryGetArchetype(out var archetype, out var archetypeIndex) || archetype == null)
            {
                var relationMask = new RelationMask(relationType);
                var newArchetype = BuildArchetype(default, relationMask);
                var newIndex = newArchetype.AddEntity(_entity);
                ChangedArchetype(newArchetype, newIndex);
                newArchetype.AddRelation(relationType, target);
                return this;
            }

            if (archetype.RelationTypes.On(relationType))
            {
                archetype.AddRelation(relationType, target);
                return this;
            }

            var newRelationMask = archetype.RelationTypes;
            newRelationMask.Add(relationType);

            var migratedArchetype = BuildArchetype(archetype.ComponentTypes, newRelationMask);
            var migratedIndex = migratedArchetype.AddEntity(_entity);
            ChangedArchetype(migratedArchetype, migratedIndex);
            migratedArchetype.AddRelation(relationType, target);
            return this;
        }

        /// <summary>
        /// 从实体移除关系类型。 若关系类型不存在则忽略；存在则迁移到移除该关系类型后的 Archetype。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>返回自身以支持链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveRelation(RelationType relationType)
        {
            ThrowIfNotMainThread();

            if (!TryGetArchetype(out var archetype, out var archetypeIndex) || archetype == null)
                return this;

            if (!archetype.RelationTypes.On(relationType))
                return this;

            var newRelationMask = archetype.RelationTypes;
            newRelationMask.Remove(relationType);

            var newArchetype = BuildArchetype(archetype.ComponentTypes, newRelationMask);
            var newIndex = newArchetype.AddEntity(_entity);
            ChangedArchetype(newArchetype, newIndex);
            return this;
        }

        /// <summary>
        /// 构建（或获取）指定组件掩码与关系掩码对应的 Archetype。
        /// </summary>
        /// <param name="componentMask">组件掩码。</param>
        /// <returns>对应的 Archetype 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype BuildArchetype(in ComponentMask componentMask)
            => BuildArchetype(componentMask, default);

        /// <summary>
        /// 构建（或获取）指定组件掩码与关系掩码对应的 Archetype。
        /// </summary>
        /// <param name="componentMask">组件掩码。</param>
        /// <param name="relationMask">关系掩码。</param>
        /// <returns>对应的 Archetype 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype BuildArchetype(in ComponentMask componentMask, in RelationMask relationMask)
            => Components.GetOrCreateArchetype(componentMask, relationMask);

        /// <summary>
        /// 尝试获取实体当前的 Archetype 与在该 Archetype 中的索引（内部辅助）。 若实体不存在或已被销毁会抛出异常。
        /// </summary>
        /// <param name="archetype">输出实体所属的 Archetype。</param>
        /// <param name="archetypeIndex">输出实体在 Archetype 中的索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetArchetype(out Archetype? archetype, out int archetypeIndex)
        {
            if (currentArchetype != null)
            {
                archetype = currentArchetype;
                archetypeIndex = currentArchetypeIndex;
                return true;
            }

            if (Entities.TryGetArchetype(_entity, out archetype, out archetypeIndex))
            {
                currentArchetype = archetype;
                currentArchetypeIndex = archetypeIndex;
                return true;
            }

            throw new InvalidOperationException($"无法获取实体组件原型。 {_entity}");
        }

        /// <summary>
        /// 将实体切换到指定的 Archetype，并输出新 Archetype 中的索引。 该方法委托给 EntityManager 执行实际的迁移逻辑。
        /// </summary>
        /// <param name="archetype">目标 Archetype。</param>
        /// <param name="archetypeIndex">输出迁移后实体在目标 Archetype 中的索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangedArchetype(Archetype? archetype, int archetypeIndex)
        {
            if (archetype != null && currentArchetype != null && archetypeIndex >= 0)
            {
                currentArchetype.TryCopyToAndRemove(currentArchetypeIndex, archetype, archetypeIndex);
            }

            if (Entities.TryChangedArchetype(_entity, archetype, archetypeIndex))
            {
                currentArchetype = archetype;
                currentArchetypeIndex = archetypeIndex;
                return;
            }

            throw new InvalidOperationException($"无法更新实体组件原型。 {_entity}");
        }

        /// <summary>
        /// 校验当前线程为主线程，否则抛出异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfNotMainThread() => MainThreadDetector.ThrowIfNotMainThread();
    }
}