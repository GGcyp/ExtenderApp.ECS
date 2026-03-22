using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 组件注册表。 用于管理组件类型与其对应 <see cref="ComponentType" /> 的映射关系。 通过泛型静态缓存为每个组件类型分配并复用唯一索引，以支持高效类型比较与掩码计算。
    /// </summary>
    internal static class ComponentRegistry
    {
        /// <summary>
        /// 当前支持的最大组件类型容量（初始化容量）。
        /// </summary>
        private const int CurrentMaxComponentTypes = 512;

        /// <summary>
        /// 组件类型缓存列表。 列表下标与组件类型索引存在对应关系（注意该实现索引从 1 开始分配）。
        /// </summary>
        private static List<ComponentTypeCache> componentTypes = new(CurrentMaxComponentTypes);

        /// <summary>
        /// 获取或创建组件类型 <typeparamref name="T" /> 对应的 <see cref="ComponentType" />。
        /// </summary>
        /// <typeparam name="T">组件类型，必须为值类型并实现 <see cref="IComponent" />。</typeparam>
        /// <returns>与 <typeparamref name="T" /> 对应的组件类型描述。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentType GetOrCreateIndex<T>() where T : struct => new(ComponentTypeCache<T>.Instance);

        /// <summary>
        /// 按索引获取已注册的组件类型描述。
        /// </summary>
        /// <param name="index">组件类型索引。</param>
        /// <returns>对应索引的 <see cref="ComponentType" />。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentType GetComponentType(int index) => new(componentTypes[index - 1]);

        /// <summary>
        /// 组件类型缓存抽象基类。 封装组件索引、名称与运行时类型信息。
        /// </summary>
        public abstract class ComponentTypeCache
        {
            /// <summary>
            /// 空组件缓存实例（索引为 0）。
            /// </summary>
            public static readonly ComponentTypeCache Empty = new EmptyComponentTypeCache();

            /// <summary>
            /// 组件类型索引。
            /// </summary>
            public abstract ushort Index { get; }

            /// <summary>
            /// 组件类型名称。
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// 组件运行时类型。
            /// </summary>
            public abstract Type ComponentType { get; }
        }

        /// <summary>
        /// 空组件缓存实现。 用于表示“无组件”，其索引固定为 0。
        /// </summary>
        public sealed class EmptyComponentTypeCache : ComponentTypeCache
        {
            /// <summary>
            /// 空组件索引（固定为 0）。
            /// </summary>
            public override ushort Index => 0;

            /// <summary>
            /// 空组件名称。
            /// </summary>
            public override string Name => "Empty";

            /// <summary>
            /// 空组件对应的占位类型。
            /// </summary>
            public override Type ComponentType => typeof(void);
        }

        /// <summary>
        /// 泛型组件类型缓存。 每个不同的 <typeparamref name="T" /> 会拥有独立静态实例，并在首次访问时分配唯一索引。
        /// </summary>
        /// <typeparam name="T">组件类型，必须为值类型并实现 <see cref="IComponent" />。</typeparam>
        private sealed class ComponentTypeCache<T> : ComponentTypeCache where T : struct
        {
            /// <summary>
            /// 当前泛型组件类型的全局缓存实例。
            /// </summary>
            public static readonly ComponentTypeCache<T> Instance = new ComponentTypeCache<T>();

            /// <summary>
            /// 当前组件类型的唯一索引。
            /// </summary>
            private ushort index;

            /// <summary>
            /// 当前组件类型索引。
            /// </summary>
            public override ushort Index => index;

            /// <summary>
            /// 当前组件类型名称。
            /// </summary>
            public override string Name => typeof(T).Name;

            /// <summary>
            /// 当前组件运行时类型。
            /// </summary>
            public override Type ComponentType => typeof(T);

            /// <summary>
            /// 初始化泛型组件类型缓存并分配唯一索引。
            /// </summary>
            public ComponentTypeCache()
            {
                componentTypes.Add(this);
                index = (ushort)componentTypes.Count;
            }
        }
    }
}