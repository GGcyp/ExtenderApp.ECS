using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示 ECS 中具有唯一标识的最小实体。
    /// </summary>
    [DebuggerDisplay("Entity(Version = {Version}, Id = {Id})")]
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>
    {
        /// <summary>
        /// 空实体实例。
        /// </summary>
        public static readonly Entity Empty = new(0, 0);

        /// <summary>
        /// 获取一个虚拟实体实例，供系统内部使用。虚拟实体的标识符为负数，版本号为零。
        /// </summary>
        /// <param name="virtualId">虚拟实体的标识符，通常为负数。</param>
        /// <returns>返回一个虚拟实体实例。</returns>
        internal static Entity GetVirtualEntity(int virtualId) => new(virtualId, 0);

        /// <summary>
        /// 实体的版本号，用于区分不同生命周期的实体实例。
        /// </summary>
        [FieldOffset(0)]
        internal readonly uint Version;

        /// <summary>
        /// 实体的唯一标识符，用于区分不同的实体实例。
        /// </summary>
        [FieldOffset(4)]
        internal readonly int Id;

        /// <summary>
        /// 获取当前实体是否为空实体。空实体的标识符和版本号均为零。
        /// </summary>
        public bool IsEmpty => Id == 0 && Version == 0;

        /// <summary>
        /// 获取当前实体是否为虚拟实体。虚拟实体的标识符为负数，版本号为零，供系统内部使用。
        /// </summary>
        public bool IsVirtual => Id < 0;

        /// <summary>
        /// 使用指定标识初始化 <see cref="Entity" /> 的新实例。
        /// </summary>
        /// <param name="worldId">实体所属世界的唯一标识符。</param>
        /// <param name="id">实体标识。</param>
        /// <param name="version">实体版本号。</param>
        internal Entity(int id, uint version)
        {
            Id = id;
            Version = version;
        }

        /// <summary>
        /// 判断当前实体是否等于另一个实体。
        /// </summary>
        /// <param name="other">要比较的另一个实体。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public bool Equals(Entity other) => Id == other.Id && Version == other.Version;

        #region Object Overrides

        /// <summary>
        /// 判断当前实体是否等于指定对象。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public override bool Equals(object? obj) => obj is Entity other && Equals(other);

        /// <summary>
        /// 获取当前实体的哈希码。
        /// </summary>
        /// <returns>哈希码值。</returns>
        public override int GetHashCode() => HashCode.Combine(Id, Version);

        /// <summary>
        /// 获取当前实体的字符串表示。
        /// </summary>
        /// <returns>字符串表示。</returns>
        public override string ToString() => $"{Id}:{Version}";

        /// <summary>
        /// 对当前实体与另一个实体进行比较，首先比较标识符，如果标识符相同则比较版本号。返回值小于零表示当前实体小于另一个实体，返回值大于零表示当前实体大于另一个实体，返回值等于零表示两个实体相等。
        /// </summary>
        /// <param name="other">要比较的另一个实体。</param>
        /// <returns>比较结果。</returns>
        public int CompareTo(Entity other)
        {
            int idComparison = Id.CompareTo(other.Id);
            if (idComparison != 0)
                return idComparison;
            return Version.CompareTo(other.Version);
        }

        #endregion Object Overrides

        #region Operator

        /// <summary>
        /// 判断两个实体是否相等。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public static bool operator ==(Entity left, Entity right) => left.Equals(right);

        /// <summary>
        /// 判断两个实体是否不相等。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>不相等返回 true；否则返回 false。</returns>
        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        #endregion Operator
    }
}