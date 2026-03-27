using static ExtenderApp.ECS.Components.SharedComponentRegistry;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 共享组件类型的轻量标识（供框架内部使用）。
    /// 表示针对引用类型（class）的全局共享数据或单例的类型元信息，语义上类似于 <see cref="ComponentType"/>，
    /// 但专门用于托管引用类型的共享组件。该结构体包含对注册表缓存条目的引用与分配的类型索引，
    /// 可用于快速比较、哈希与调试显示。
    /// </summary>
    internal readonly struct SharedComponentType : IEquatable<SharedComponentType>
    {
        /// <summary>
        /// 对应的缓存元数据（由 <see cref="SharedComponentRegistry"/> 管理），用于获取类型名等信息。
        /// </summary>
        private readonly SharedTypeCache _cache;

        /// <summary>
        /// 共享类型的索引（从 1 开始分配）。0 表示无效或未设置。
        /// </summary>
        internal readonly ushort TypeIndex;

        /// <summary>
        /// 获取共享组件类型的实际 System.Type 实例（通过缓存访问）。该属性仅供框架内部使用，外部代码不应直接依赖于它。
        /// </summary>
        internal Type TypeInstance => _cache.TypeInstance;

        /// <summary>
        /// 使用注册表缓存条目构造 SharedComponentType（内部注册表调用）。
        /// </summary>
        /// <param name="cache">来自 <see cref="SharedComponentRegistry"/> 的缓存实例。</param>
        public SharedComponentType(SharedTypeCache cache)
        {
            _cache = cache;
            TypeIndex = cache.Index;
        }

        /// <summary>
        /// 基于类型索引比较是否相等（高效）。
        /// </summary>
        public bool Equals(SharedComponentType other) => TypeIndex == other.TypeIndex;

        /// <summary>
        /// 对象等价判断。
        /// </summary>
        public override bool Equals(object? obj) => obj is SharedComponentType other && Equals(other);

        /// <summary>
        /// 返回用于字典/集合的哈希码（基于 TypeIndex）。
        /// </summary>
        public override int GetHashCode() => TypeIndex;

        /// <summary>
        /// 返回类型名称的字符串表示，便于调试输出。
        /// </summary>
        public override string ToString() => _cache.Name;

        /// <summary>
        /// 获取或创建指定引用类型对应的共享组件类型标识。
        /// 用法示例：<c>var t = SharedComponentType.Create&lt;MySingleton&gt;();</c>
        /// </summary>
        /// <typeparam name="T">引用类型（class）。</typeparam>
        public static SharedComponentType Create<T>() => GetOrCreate<T>();
    }
}