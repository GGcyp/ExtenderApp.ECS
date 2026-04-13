using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 关系对（Pair）标识。 使用一个 <see cref="ulong" /> 打包存储：
    /// - 高 32 位：关系类型索引（RelationType）
    /// - 低 32 位：目标实体 Id（不包含 Version）
    /// </summary>
    [DebuggerDisplay("RelationPair(Type={RelationType}, TargetId={TargetId})")]
    public readonly struct RelationPair : IEquatable<RelationPair>
    {
        /// <summary>
        /// 类型位移，用于将关系类型索引存储在高 32 位。 由于 Entity Id 占用 32 位，因此类型索引需要左移 32 位以避免与目标实体 Id 冲突。
        /// </summary>
        private const int TypeShift = 32;

        /// <summary>
        /// 目标掩码，用于提取关系对中的目标实体 Id（不含 Version）。 由于 Entity Id 占用 32 位，因此掩码为 0xFFFF_FFFF。
        /// </summary>
        private const ulong TargetMask = 0xFFFF_FFFFUL;

        /// <summary>
        /// 关系对的原始打包值。
        /// </summary>
        public readonly ulong Value;

        /// <summary>
        /// 获取关系类型。
        /// </summary>
        public RelationType RelationType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RelationType.GetCommandType((int)(Value >> TypeShift));
        }

        /// <summary>
        /// 获取目标实体 Id（不含 Version）。
        /// </summary>
        public int TargetId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Value & TargetMask);
        }

        /// <summary>
        /// 创建关系对。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">目标实体。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RelationPair Create(RelationType relationType, Entity target) => new((((ulong)relationType.TypeIndex) << TypeShift) | (uint)target.Id);

        /// <summary>
        /// 创建关系对。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="targetId">目标实体 Id。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RelationPair Create(RelationType relationType, int targetId) => new((((ulong)relationType.TypeIndex) << TypeShift) | (uint)targetId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RelationPair(ulong value) => Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelationPair other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is RelationPair other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"({RelationType}, {TargetId})";

        public static bool operator ==(RelationPair left, RelationPair right) => left.Equals(right);

        public static bool operator !=(RelationPair left, RelationPair right) => !left.Equals(right);
    }
}