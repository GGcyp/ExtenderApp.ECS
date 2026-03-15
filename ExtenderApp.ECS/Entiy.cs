using System.Diagnostics;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示 ECS 中具有唯一标识的最小实体。
    /// </summary>
    [DebuggerDisplay("Entity(Id = {Id}, Version = {Version})")]
    public readonly struct Entity : IEquatable<Entity>
    {
        /// <summary>
        /// 空实体实例。
        /// </summary>
        public static readonly Entity Empty = new(0);

        /// <summary>
        /// 实体的唯一标识符，用于区分不同的实体实例。
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// 实体的版本号，用于区分不同生命周期的实体实例。
        /// </summary>
        internal readonly uint Version;

        /// <summary>
        /// 使用指定标识初始化 <see cref="Entity" /> 的新实例。
        /// </summary>
        /// <param name="id">实体标识。</param>
        /// <param name="version">实体版本号。</param>
        internal Entity(int id, uint version = 0)
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