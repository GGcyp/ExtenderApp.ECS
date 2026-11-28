using ExtenderApp.ECS;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 管理组件的存储与按实体查询。
    /// </summary>
    internal class ArchetypeManager : DisposableObject
    {
        /// <summary>
        /// 管理结构版本的实例。
        /// </summary>
        private readonly StructureVersionManager _svManager;

        /// <summary>
        /// Archetype 仓库。
        /// </summary>
        private readonly ArchetypeRepository _archetypes;

        /// <summary>
        /// 创建一个新的 <see cref="ArchetypeManager" /> 实例并初始化内部存储。
        /// </summary>
        public ArchetypeManager(ArchetypeRepository archetypeDict, StructureVersionManager structureVersionManager)
        {
            _svManager = structureVersionManager;
            _archetypes = archetypeDict;
        }

        /// <summary>
        /// 根据指定的组件掩码获取已存在的 <see cref="Archetype" />，如果不存在则创建并返回新的 Archetype。
        /// </summary>
        /// <param name="componentMask">表示一组组件类型的掩码。</param>
        /// <returns>与给定掩码对应的 <see cref="Archetype" /> 实例。</returns>
        public Archetype GetOrCreateArchetype(in ComponentMask componentMask)
            => GetOrCreateArchetype(componentMask, default);

        /// <summary>
        /// 根据组件掩码与关系掩码获取已存在的 <see cref="Archetype" />，不存在则创建。
        /// </summary>
        /// <param name="componentMask">组件掩码，表示实体拥有的组件类型集合。</param>
        /// <param name="relationMask">关系掩码，表示实体之间的关系类型集合。</param>
        /// <returns>与给定掩码对应的 <see cref="Archetype" /> 实例。</returns>
        public Archetype GetOrCreateArchetype(in ComponentMask componentMask, in RelationMask relationMask)
        {
            // 先尝试无锁快速读取
            if (_archetypes.TryGetValue(componentMask, relationMask, out var archetype))
                return archetype;

            // 列提供器：组件列在前（按掩码编码位置），关系目标列在后（每关系类型一列，元素类型为 Entity）
            var providers = new ArchetypeChunkProvider[componentMask.ComponentCount + relationMask.RelationCount];
            int i = 0;
            foreach (var componentType in componentMask)
            {
                providers[i++] = ArchetypeChunkProvider.GetOrCreate(componentType);
            }

            var relationTargetProvider = ArchetypeChunkProvider.GetOrCreate(typeof(Entity));
            foreach (var relationType in relationMask)
            {
                providers[i++] = relationTargetProvider;
            }

            // 创建并缓存新的 Archetype
            archetype = new(providers, componentMask, relationMask, _svManager);
            _archetypes[componentMask, relationMask] = archetype;
            return archetype;
        }

        /// <summary>
        /// 删除指定组件掩码与关系掩码对应的 Archetype。
        /// </summary>
        public bool RemoveArchetype(in ComponentMask mask, in RelationMask relationMask, bool dispose = false)
        {
            if (!_archetypes.Remove(mask, relationMask, out var archetype))
                return false;

            if (dispose)
                archetype.Dispose();
            return true;
        }

        protected override void DisposeManagedResources()
        {
            _archetypes.Dispose();
            base.DisposeManagedResources();
        }
    }
}