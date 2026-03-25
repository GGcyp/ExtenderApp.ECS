using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 实体查询管理器（Query 缓存与工厂）。
    /// 
    /// 职责：
    /// - 根据给定的 <see cref="EntityQueryDesc"/> 创建或复用查询核心 <see cref="QueryCore"/>；
    /// - 将查询核心按描述缓存以避免重复匹配扫描，提高重复查询的性能；
    /// - 为不同的泛型 EntityQuery 结构体返回共享的查询核心，以减小内存占用与匹配开销。
    /// 
    /// 线程语义：该类型不是线程安全的，通常每个 World 持有一个实例并在主线程上使用；若在多线程场景下访问，需要外部同步。
    /// </summary>
    internal class EntityQueryManager
    {
        /// <summary>
        /// 当前 World 中的全部 Archetype 注册表（由 Archetype 仓库维护），用于查询核心的匹配阶段遍历。
        /// </summary>
        private readonly ArchetypeRepository _archetypeRepository;

        /// <summary>
        /// World 版本管理器。用于在查询核心中判断匹配缓存是否失效并触发重建。
        /// </summary>
        private readonly WorldVersionManager _worldVersionManager;

        /// <summary>
        /// 查询核心缓存表。Key 为查询描述，Value 为对应的查询核心实例。
        /// </summary>
        private readonly Dictionary<EntityQueryDesc, QueryCore> _queries;

        /// <summary>
        /// 创建查询管理器实例。通常由 World 在初始化时构造并持有生命周期。
        /// </summary>
        /// <param name="archetypeRepository">当前 World 中全部 Archetype 的只读集合。</param>
        /// <param name="worldVersionManager">World 版本管理器。</param>
        public EntityQueryManager(ArchetypeRepository archetypeRepository, WorldVersionManager worldVersionManager)
        {
            _archetypeRepository = archetypeRepository;
            _worldVersionManager = worldVersionManager;
            _queries = new();
        }

        /// <summary>
        /// 获取或创建非泛型的实体查询包装（仅返回实体句柄的查询）。
        /// 返回值包装了共享的查询核心，可用于 foreach 或转换为具体泛型查询。
        /// </summary>
        public EntityQuery GetOrCreateQuery(EntityQueryDesc desc)
            => new(GetOrCreateCore(desc));

        /// <summary>
        /// 获取或创建单组件查询包装。
        /// 返回的 <see cref="EntityQuery{T}"/> 会共享同一个 <see cref="QueryCore"/> 实例。
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
        /// 获取或创建查询核心（QueryCore）。
        /// 若缓存中已存在对应描述的核心则直接返回，否则创建新核心并缓存。
        /// </summary>
        private QueryCore GetOrCreateCore(EntityQueryDesc desc)
        {
            if (_queries.TryGetValue(desc, out var core))
                return core;

            core = new(_archetypeRepository, desc, _worldVersionManager);
            _queries.Add(desc, core);
            return core;
        }
    }
}