using System.Diagnostics;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示 ECS 中具有唯一标识的最小实体。
    /// </summary>
    [DebuggerDisplay("Entity(Id = {Id})")]
    public readonly struct Entity : IEquatable<Entity>
    {
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

        public bool Equals(Entity other) => Id == other.Id && Version == other.Version;

        #region Object Overrides

        public override bool Equals(object? obj) => obj is Entity other && Equals(other);

        public override int GetHashCode() => (int)Id;

        public override string ToString() => Id.ToString();

        #endregion Object Overrides

        #region Operator

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);

        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        #endregion Operator
    }
}