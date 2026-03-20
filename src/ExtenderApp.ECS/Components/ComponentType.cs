using System.Diagnostics;
using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示组件类型的轻量标识，用于在 ECS 系统中唯一标识一个组件的类型。
    /// 该结构体通过内部的整数索引进行比较和哈希，以便于快速比较与查找。
    /// </summary>
    [DebuggerDisplay("ComponentType : {TypeName}")]
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        /// <summary>
        /// 获取一个空的组件标识
        /// </summary>
        public static readonly ComponentType Empty = new(0);

        /// <summary>
        /// 内部使用的类型索引，用于快速比较和哈希。
        /// </summary>
        internal readonly ushort TypeIndex;

        /// <summary>
        /// 组件类型的名称（通常为类型的简单名称）。
        /// </summary>
        public string? TypeName => TypeIndex < 0 ? string.Empty : ComponentManager.GetTypeByIndex(TypeIndex).Name;

        /// <summary>
        /// 获取组件类型实例
        /// </summary>
        public Type? TypeInstance => TypeIndex < 0 ? null : ComponentManager.GetTypeByIndex(TypeIndex);

        /// <summary>
        /// 为指定的组件类型 `T` 创建一个 `ComponentType` 实例。
        /// 该方法会从 `ArchetypeManager` 获取或创建类型索引。
        /// </summary>
        /// <typeparam name="T">实现了 `IComponent` 的值类型组件。</typeparam>
        /// <returns>对应组件类型的 `ComponentType` 实例。</returns>
        public static ComponentType Create<T>() where T : struct, IComponent
        {
            var index = ComponentManager.GetOrCreateIndex<T>();
            return new(index);
        }

        /// <summary>
        /// 程序集内通过指定类型序列获取组件类型。
        /// </summary>
        /// <param name="typeIndex">组件类型索引。</param>
        /// <returns>对应组件类型的 `ComponentType` 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ComponentType GetCommandType(ushort typeIndex) => new(typeIndex);

        /// <summary>
        /// 内部构造函数，直接使用已存在的索引和类型名称创建实例。
        /// </summary>
        /// <param name="index">组件类型索引。</param>
        /// <param name="typeName">组件类型名称。</param>
        internal ComponentType(ushort index) => TypeIndex = index;

        /// <summary>
        /// 基于内部索引比较两个 `ComponentType` 是否相等。
        /// </summary>
        /// <param name="other">要比较的另一个 `ComponentType`。</param>
        /// <returns>如果两个实例表示相同的组件类型则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool Equals(ComponentType other) => this.TypeIndex == other.TypeIndex;

        /// <summary>
        /// 重写的对象相等比较实现，支持与任意对象比较。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>如果对象为 `ComponentType` 且表示相同类型则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public override bool Equals(object? obj) => obj is ComponentType other && Equals(other);

        /// <summary>
        /// 返回用于字典或哈希集合的哈希代码，基于内部类型索引。
        /// </summary>
        /// <returns>类型索引的哈希码。</returns>
        public override int GetHashCode() => TypeIndex;

        /// <summary>
        /// 返回组件类型的字符串表示，默认为类型名称。
        /// </summary>
        /// <returns>组件类型名称。</returns>
        public override string ToString() => TypeName ?? $"未知类型({TypeIndex})";

        #region Operators

        /// <summary>
        /// 相等运算符，比较两个 `ComponentType` 是否表示相同类型。
        /// </summary>
        public static bool operator ==(ComponentType left, ComponentType right) => left.Equals(right);

        /// <summary>
        /// 不等运算符，比较两个 `ComponentType` 是否表示不同类型。
        /// </summary>
        public static bool operator !=(ComponentType left, ComponentType right) => !left.Equals(right);

        /// <summary>
        /// 隐式转换运算符，将 `ComponentType` 转换为其类型名称字符串。
        /// </summary>
        /// <param name="type">要转换的 `ComponentType`。</param>
        public static implicit operator string(ComponentType type) => type.TypeName;

        #endregion Operators
    }
}