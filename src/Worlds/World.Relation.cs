using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的关系相关 API 的局部实现。 关系目标以行内列存储，Add/Remove 关系仅在关系类型位变化时触发 Archetype 迁移；
    /// 同表内不同实体可以持有不同的关系目标。
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// 向实体添加或覆盖关系目标（当前 Archetype 已含该关系类型时原地改写，零迁移）。
        /// </summary>
        /// <param name="entity">实体句柄。</param>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">目标实体。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddRelation(Entity entity, RelationType relationType, Entity target)
            => GetEntityComponentOperation(entity).AddRelation(relationType, target);

        /// <summary>
        /// 向实体添加或覆盖泛型关系目标。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <param name="entity">实体句柄。</param>
        /// <param name="target">目标实体。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation AddRelation<T>(Entity entity, Entity target) where T : struct
            => AddRelation(entity, RelationType.Create<T>(), target);

        /// <summary>
        /// 从实体移除关系类型（存在时迁移到移除该关系类型后的 Archetype）。
        /// </summary>
        /// <param name="entity">实体句柄。</param>
        /// <param name="relationType">关系类型。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveRelation(Entity entity, RelationType relationType)
            => GetEntityComponentOperation(entity).RemoveRelation(relationType);

        /// <summary>
        /// 从实体移除泛型关系类型。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <param name="entity">实体句柄。</param>
        /// <returns>用于链式调用的 <see cref="EntityComponentOperation" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityComponentOperation RemoveRelation<T>(Entity entity) where T : struct
            => RemoveRelation(entity, RelationType.Create<T>());

        /// <summary>
        /// 尝试获取实体指定关系类型的目标实体。 目标实体可能已被销毁（悬挂），调用方应使用 <see cref="EntityManager.IsAlive" /> 校验。
        /// </summary>
        /// <param name="entity">实体句柄。</param>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">输出目标实体。</param>
        /// <returns>实体存在该关系类型且读取成功返回 true；否则返回 false。</returns>
        public bool TryGetRelationTarget(Entity entity, RelationType relationType, out Entity target)
        {
            target = default;
            if (!EManager.TryGetArchetype(entity, out var archetype, out int archetypeIndex) || archetype == null)
                return false;

            return archetype.TryGetRelationTarget(relationType, archetypeIndex, out target);
        }

        /// <summary>
        /// 尝试获取实体指定泛型关系类型的目标实体。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <param name="entity">实体句柄。</param>
        /// <param name="target">输出目标实体。</param>
        /// <returns>实体存在该关系类型且读取成功返回 true；否则返回 false。</returns>
        public bool TryGetRelationTarget<T>(Entity entity, out Entity target) where T : struct
            => TryGetRelationTarget(entity, RelationType.Create<T>(), out target);

        /// <summary>
        /// 查询指定关系类型下目标为 <paramref name="target" /> 的全部实体（类型位筛表 + 行内过滤）。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">目标实体。</param>
        /// <returns>按目标过滤后的实体枚举器。</returns>
        public RelationTargetEntityEnumerator GetEntitiesWithRelation(RelationType relationType, Entity target)
        {
            EntityQueryDesc desc = new(ComponentMask.Empty, ComponentMask.Empty, ComponentMask.Empty, ComponentMask.Empty, new RelationMask(relationType));
            return EQManager.GetOrCreateQuery(desc).Core.GetRelationTargetEntityEnumerator(relationType, target);
        }

        /// <summary>
        /// 查询指定泛型关系类型下目标为 <paramref name="target" /> 的全部实体。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <param name="target">目标实体。</param>
        /// <returns>按目标过滤后的实体枚举器。</returns>
        public RelationTargetEntityEnumerator GetEntitiesWithRelation<T>(Entity target) where T : struct
            => GetEntitiesWithRelation(RelationType.Create<T>(), target);

        /// <summary>
        /// 创建实体并同时设置关系目标（一步完成建表与写行，避免创建后二次 Archetype 查找）。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <param name="target">目标实体。</param>
        /// <returns>新创建的实体句柄。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntityWithRelation<T>(Entity target) where T : struct
        {
            ThrowIfNotMainThread();

            var relationType = RelationType.Create<T>();
            var relationMask = new RelationMask(relationType);
            var archetype = AManager.GetOrCreateArchetype(default, relationMask);
            var entity = EManager.CreateEntity(archetype, out int archetypeIndex);
            archetype.TrySetRelationTarget(relationType, archetypeIndex, target);
            return entity;
        }

        /// <summary>
        /// 返回所有「任一关系类型的目标实体等于 <paramref name="entity" />」的存活实体（子实体）的零堆结构体枚举器。
        /// 直接遍历仓库中含关系类型的 Archetype 并逐行比较目标值，不创建任何集合。
        /// 注意：关系目标值在目标销毁后不清除（悬挂），因此对已销毁实体遍历仍会产出指向它的悬挂实体；
        /// 遍历期间不可执行结构变更。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <returns>子实体枚举器。</returns>
        public ChildrenEntityEnumerator GetChildren(Entity entity)
        {
            ThrowIfNotMainThread();
            return new(ArchetypeRepository, entity);
        }
    }
}
