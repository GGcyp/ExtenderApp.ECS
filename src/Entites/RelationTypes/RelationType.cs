using System.Diagnostics;
using System.Runtime.CompilerServices;
using static ExtenderApp.ECS.Entities.RelationTypeRegistry;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 关系类型轻量标识。 在 ECS 中用于唯一表示某个关系数据类型，并通过内部索引实现高效比较与哈希。
    /// </summary>
    [DebuggerDisplay("RelationType : {TypeName}")]
    public readonly struct RelationType : IEquatable<RelationType>
    {
        /// <summary>
        /// 空关系类型标识（索引为 0）。
        /// </summary>
        public static readonly RelationType Empty = new(RelationTypeCache.Empty);

        private readonly RelationTypeCache _cache;

        /// <summary>
        /// 关系类型索引。
        /// </summary>
        internal readonly ushort TypeIndex;

        /// <summary>
        /// 为兼容旧调用方保留的 Id 访问。
        /// </summary>
        internal ushort Id => TypeIndex;

        /// <summary>
        /// 具体关系数据类型。
        /// </summary>
        internal Type TypeInstance => _cache.RelationDataType;

        /// <summary>
        /// 具体关系数据类型名称。
        /// </summary>
        internal string TypeName => _cache.Name;

        /// <summary>
        /// 获取或创建关系类型 <typeparamref name="T" /> 对应的标识。
        /// </summary>
        public static RelationType Create<T>() where T : struct => RegisterRelationType<T>();

        /// <summary>
        /// 程序集内按索引还原关系类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RelationType GetCommandType(int typeIndex) => GetRelationType(typeIndex);

        internal RelationType(RelationTypeCache cache)
        {
            TypeIndex = cache.Index;
            _cache = cache;
        }

        public bool Equals(RelationType other) => TypeIndex == other.TypeIndex;

        public override bool Equals(object? obj) => obj is RelationType other && Equals(other);

        public override int GetHashCode() => TypeIndex;

        public override string ToString() => TypeName;

        public static bool operator ==(RelationType left, RelationType right) => left.Equals(right);

        public static bool operator !=(RelationType left, RelationType right) => !left.Equals(right);
    }
}