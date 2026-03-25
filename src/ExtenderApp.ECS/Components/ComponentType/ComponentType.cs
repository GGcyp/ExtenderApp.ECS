using System.Diagnostics;
using System.Runtime.CompilerServices;
using static ExtenderApp.ECS.Components.ComponentRegistry;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 组件类型轻量标识。 在 ECS 中用于唯一表示某个组件类型，并通过内部索引实现高效比较与哈希。
    /// </summary>
    [DebuggerDisplay("ComponentType : {TypeName}")]
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        /// <summary>
        /// 空组件类型标识（索引为 0）。
        /// </summary>
        public static readonly ComponentType Empty = new(ComponentTypeCache.Empty);

        /// <summary>
        /// 组件元数据缓存（可能为空）。
        /// </summary>
        internal ComponentTypeCache Cache { get; }

        /// <summary>
        /// 组件类型索引，用于比较和哈希。
        /// </summary>
        internal readonly ushort TypeIndex;

        /// <summary>
        /// 组件运行时类型。
        /// </summary>
        internal Type TypeInstance => Cache.ComponentType;

        /// <summary>
        /// 组件类型名称。
        /// </summary>
        internal string TypeName => Cache.Name;

        /// <summary>
        /// 获取组件类型是否为一个空结构体（即不包含任何字段）。
        /// </summary>
        internal bool IsEmptyComponent => Cache.IsEmptyComponent;

        /// <summary>
        /// 根据组件泛型类型创建（或获取）对应的 <see cref="ComponentType" />。
        /// </summary>
        /// <typeparam name="T">组件类型，必须为结构体并实现 <see cref="IComponent" />。</typeparam>
        /// <returns>组件类型标识。</returns>
        public static ComponentType Create<T>() where T : struct => GetOrCreateIndex<T>();

        /// <summary>
        /// 程序集内通过类型索引创建组件类型标识。
        /// </summary>
        /// <param name="typeIndex">组件类型索引。</param>
        /// <returns>组件类型标识。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ComponentType GetCommandType(int typeIndex) => GetComponentType(typeIndex);

        /// <summary>
        /// 通过缓存对象构造组件类型标识。
        /// </summary>
        /// <param name="cache">组件类型缓存。</param>
        internal ComponentType(ComponentTypeCache cache)
        {
            TypeIndex = cache.Index;
            Cache = cache;
        }

        /// <summary>
        /// 判断当前实例与另一个 <see cref="ComponentType" /> 是否相等。
        /// </summary>
        /// <param name="other">要比较的实例。</param>
        /// <returns>索引相同返回 true；否则返回 false。</returns>
        public bool Equals(ComponentType other) => this.TypeIndex == other.TypeIndex;

        /// <summary>
        /// 判断当前实例是否与指定对象相等。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>对象为同索引的 <see cref="ComponentType" /> 时返回 true。</returns>
        public override bool Equals(object? obj) => obj is ComponentType other && Equals(other);

        /// <summary>
        /// 获取哈希码。
        /// </summary>
        /// <returns>基于 <see cref="TypeIndex" /> 的哈希码。</returns>
        public override int GetHashCode() => TypeIndex;

        /// <summary>
        /// 返回组件类型的字符串表示。
        /// </summary>
        /// <returns>组件名称；当无缓存时返回 Unknown。</returns>
        public override string ToString() => TypeName;

        #region Operators

        /// <summary>
        /// 判断两个组件类型标识是否相等。
        /// </summary>
        /// <param name="left">左值。</param>
        /// <param name="right">右值。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public static bool operator ==(ComponentType left, ComponentType right) => left.Equals(right);

        /// <summary>
        /// 判断两个组件类型标识是否不相等。
        /// </summary>
        /// <param name="left">左值。</param>
        /// <param name="right">右值。</param>
        /// <returns>不相等返回 true；否则返回 false。</returns>
        public static bool operator !=(ComponentType left, ComponentType right) => !left.Equals(right);

        #endregion Operators
    }
}