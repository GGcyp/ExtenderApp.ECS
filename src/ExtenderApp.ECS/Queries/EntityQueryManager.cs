using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 实体查询管理器。
    /// 负责按 <see cref="EntityQueryDesc" /> 缓存并复用 <see cref="QueryCore" />，
    /// 让不同的 <see cref="EntityQuery{T}" /> 结构体包装共享同一份查询核心数据。
    /// </summary>
    internal class EntityQueryManager
    {
        /// <summary>
        /// 当前 World 中的全部 Archetype 集合。
        /// </summary>
        private readonly IReadOnlyList<Archetype> _allArchetypeList;

        /// <summary>
        /// World 版本管理器。
        /// 用于在查询核心中判断匹配缓存是否失效。
        /// </summary>
        private readonly WorldVersionManager _worldVersionManager;

        /// <summary>
        /// 查询核心缓存表。
        /// Key 为查询描述，Value 为对应的查询核心实例。
        /// </summary>
        private readonly Dictionary<EntityQueryDesc, QueryCore> _queries;

        /// <summary>
        /// 创建查询管理器实例。
        /// </summary>
        /// <param name="allArchetypeList">当前 World 中全部 Archetype 的只读集合。</param>
        /// <param name="worldVersionManager">World 版本管理器。</param>
        public EntityQueryManager(IReadOnlyList<Archetype> allArchetypeList, WorldVersionManager worldVersionManager)
        {
            _allArchetypeList = allArchetypeList;
            _worldVersionManager = worldVersionManager;
            _queries = new();
        }

        /// <summary>
        /// 获取或创建单组件查询包装。
        /// </summary>
        public EntityQuery<T> GetOrCreateQuery<T>(EntityQueryDesc desc) where T : struct
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建双组件查询包装。
        /// </summary>
        public EntityQuery<T1, T2> GetOrCreateQuery<T1, T2>(EntityQueryDesc desc)
            where T1 : struct
            where T2 : struct
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建三组件查询包装。
        /// </summary>
        public EntityQuery<T1, T2, T3> GetOrCreateQuery<T1, T2, T3>(EntityQueryDesc desc)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建四组件查询包装。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4> GetOrCreateQuery<T1, T2, T3, T4>(EntityQueryDesc desc)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建五组件查询包装。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4, T5> GetOrCreateQuery<T1, T2, T3, T4, T5>(EntityQueryDesc desc)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建查询核心。
        /// </summary>
        private QueryCore GetOrCreateCore(EntityQueryDesc desc)
        {
            if (_queries.TryGetValue(desc, out var core))
                return core;

            core = new QueryCore(_allArchetypeList, _worldVersionManager, desc);
            _queries.Add(desc, core);
            return core;
        }
    }
}