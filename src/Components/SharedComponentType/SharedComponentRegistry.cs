namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 共享组件类型注册表（框架内部使用）。
    /// 管理被标记为“共享”的组件类型的元数据并为其分配唯一的类型索引。
    /// 这些索引可用于快速比较、哈希、位掩码定位以及调试输出。
    ///
    /// 说明：本类型为 internal，仅供框架内部在构建 SharedComponentMask 或管理共享对象时使用。
    /// 对于共享组件是值类型还是引用类型的约束由上层使用约定决定，本注册表仅负责分配与缓存元数据。
    /// </summary>
    internal static class SharedComponentRegistry
    {
        // 每段 64 位
        internal const int SegmentBits = 64;

        // 使用 4 段，支持最多 256 个共享类型
        internal const int SegmentCount = 4;

        internal const int MaxSharedCount = SegmentBits * SegmentCount;
        internal const int IndexShift = 6;
        internal const int SegmentMask = SegmentBits - 1;

        // 内部缓存的共享类型列表（索引从 1 开始分配）
        private static readonly List<SharedTypeCache> _sharedTypes = new();

        /// <summary>
        /// 获取或创建对应于类型 T1 的 <see cref="SharedComponentType"/> 标识。
        /// 首次访问时会在内部缓存列表中注册并分配唯一索引。
        /// </summary>
        /// <typeparam name="T">共享组件的类型（具体为值类型或引用类型由上层约定）。</typeparam>
        public static SharedComponentType GetOrCreate<T>()
            => new(SharedTypeCache<T>.Instance);

        /// <summary>
        /// 按已分配的索引获取共享类型标识（内部使用）。索引从 1 开始。
        /// </summary>
        /// <param name="index">类型索引。</param>
        internal static SharedComponentType GetSharedType(int index) => new(_sharedTypes[index - 1]);

        /// <summary>
        /// 缓存基类：封装共享类型的索引、名称与运行时 Type 信息。
        /// 具体类型由泛型子类在首次访问时创建并注册到列表中。
        /// </summary>
        public abstract class SharedTypeCache
        {
            /// <summary>
            /// 唯一分配的类型索引（从 1 开始）。
            /// </summary>
            public abstract ushort Index { get; }

            /// <summary>
            /// 类型名称（仅用于调试输出）。
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// 运行时的 Type 实例。
            /// </summary>
            public abstract Type TypeInstance { get; }
        }

        /// <summary>
        /// 泛型缓存实现：针对每个类型 T1 分配唯一索引并缓存元数据。
        /// 在构造时会将自身加入到 <see cref="_sharedTypes"/> 列表，从而完成索引分配。
        /// </summary>
        /// <typeparam name="T">共享组件的具体类型。</typeparam>
        private sealed class SharedTypeCache<T> : SharedTypeCache
        {
            public static readonly SharedTypeCache<T> Instance = new SharedTypeCache<T>();

            private readonly ushort _index;

            public override ushort Index => _index;

            public override string Name => typeof(T).Name;

            public override Type TypeInstance => typeof(T);

            public SharedTypeCache()
            {
                _sharedTypes.Add(this);
                _index = (ushort)_sharedTypes.Count;
            }
        }
    }
}