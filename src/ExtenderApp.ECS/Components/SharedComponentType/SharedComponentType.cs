using static ExtenderApp.ECS.Components.SharedComponentRegistry;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 共享组件类型轻量标识（框架内部使用）。
    /// 用于表示引用类型的全局共享对象/单例的类型元信息，语义上类似于 <see cref="ComponentType"/>，
    /// 但针对托管引用类型（class）。该结构体包含静态注册表缓存的元数据引用与分配的类型索引，
    /// 可用于快速比较、哈希与调试输出。
    /// </summary>
    internal readonly struct SharedComponentType : IEquatable<SharedComponentType>
    {
        /// <summary>
        /// 对应的缓存元数据（内部注册表持有），用于获取类型名与其它信息。
        /// </summary>
        private readonly SharedTypeCache _cache;

        /// <summary>
        /// 共享类型的索引（用于快速比较与位掩码定位）。索引从 1 开始分配，0 可视为无效/空。
        /// </summary>
        internal readonly ushort TypeIndex;

        /// <summary>
        /// 使用给定的缓存对象构造共享组件类型标识（内部注册表调用）。
        /// </summary>
        /// <param name="cache">来自 <see cref="SharedComponentRegistry"/> 的缓存实例。</param>
        public SharedComponentType(SharedTypeCache cache)
        {
            _cache = cache;
            TypeIndex = cache.Index;
        }

        /// <summary>
        /// 判断两个共享组件类型标识是否相等（基于索引）。
        /// </summary>
        public bool Equals(SharedComponentType other) => TypeIndex == other.TypeIndex;

        /// <summary>
        /// 判断当前对象与另一个对象是否相等。
        /// </summary>
        public override bool Equals(object? obj) => obj is SharedComponentType other && Equals(other);

        /// <summary>
        /// 获取基于类型索引的哈希码，用于字典/集合键。
        /// </summary>
        public override int GetHashCode() => TypeIndex;

        /// <summary>
        /// 返回类型名称的字符串表示（用于调试）。
        /// </summary>
        public override string ToString() => _cache.Name;

        /// <summary>
        /// 获取或创建指定引用类型对应的共享组件类型标识。
        /// 典型用法：<c>var t = SharedComponentType.Create&lt;MySingleton&gt;();</c>
        /// </summary>
        /// <typeparam name="T">引用类型（class）。</typeparam>
        /// <returns>对应的 <see cref="SharedComponentType"/> 标识。</returns>
        public static SharedComponentType Create<T>() where T : struct => GetOrCreate<T>();
    }
}