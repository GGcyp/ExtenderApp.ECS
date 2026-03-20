using ExtenderApp.Contracts;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 管理组件的存储与按实体查询。
    /// </summary>
    internal class ArchetypeManager : DisposableObject
    {
        /// <summary>
        /// 管理世界版本的实例。
        /// </summary>
        private readonly WorldVersionManager _wvManager;

        /// <summary>
        /// 将组件掩码映射到对应的 Archetype 的并发字典。
        /// key: ComponentMask, value: Archetype
        /// </summary>
        private readonly ArchetypeDictionary _archetypes;

        /// <summary>
        /// 创建一个新的 <see cref="ArchetypeManager"/> 实例并初始化内部存储。
        /// </summary>
        public ArchetypeManager(WorldVersionManager worldVersionManager, ArchetypeDictionary archetypeDict)
        {
            _archetypes = archetypeDict;
            _wvManager = worldVersionManager;
            _archetypes = new();
        }

        /// <summary>
        /// 根据指定的组件掩码获取已存在的 <see cref="Archetype"/>，如果不存在则创建并返回新的 Archetype。
        /// </summary>
        /// <param name="mask">表示一组组件类型的掩码。</param>
        /// <returns>与给定掩码对应的 <see cref="Archetype"/> 实例。</returns>
        public Archetype GetOrCreateArchetype(in ComponentMask mask)
        {
            // 先尝试无锁快速读取
            if (_archetypes.TryGetValue(mask, out var archetype))
                return archetype;

            // 为每个组件类型从提供者租用并初始化 ArchetypeChunk
            var providers = new ArchetypeChunkProvider[mask.ComponentCount];
            int i = 0;
            foreach (var componentType in mask)
            {
                providers[i++] = ArchetypeChunkProvider.GetOrCreate(componentType);
            }

            // 创建并缓存新的 Archetype
            archetype = new(providers, mask, _wvManager);
            _archetypes[mask] = archetype;
            _wvManager.IncrementArchetypeVersion(); // 组件类型发生变化，版本号递增
            return archetype;
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            foreach (var archetype in _archetypes.Values)
            {
                archetype.Dispose();
            }
        }
    }
}