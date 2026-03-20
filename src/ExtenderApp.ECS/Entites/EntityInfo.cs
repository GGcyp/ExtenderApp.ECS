using System.Diagnostics;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示实体的轻量信息（Id 与 Version），用于在系统中标识与验证实体句柄。
    /// </summary>
    [DebuggerDisplay("EntityInfo(Version = {Version}, Id = {Id}, ArchetypeIndex = {ArchetypeIndex}, Archetype = {Archetype})")]
    internal struct EntityInfo : IEquatable<EntityInfo>
    {
        /// <summary>
        /// 表示空无效的实体信息（Id 与 Version 为 0）。
        /// </summary>
        public static readonly EntityInfo Empty = new(0, 0);

        /// <summary>
        /// 实体的版本号，用于检测实体重用导致的失效句柄。
        /// </summary>
        public ushort Version;

        /// <summary>
        /// 实体的全局标识（非 0）。
        /// </summary>
        public int Id;

        /// <summary>
        /// 实体在所属 Archetype 中的索引（仅在运行时由管理器维护）。
        /// </summary>
        public int ArchetypeIndex;

        /// <summary>
        /// 实体所属的 Archetype 引用（仅在运行时由管理器维护）。
        /// </summary>
        public Archetype? Archetype;

        /// <summary>
        /// 获取一个值，指示当前实体信息是否为空或无效（Id==0 或 Version==0）。
        /// </summary>
        public bool IsEmpty => Id == 0 || Version == 0;

        /// <summary>
        /// 使用指定 Id 与版本初始化实体信息。
        /// </summary>
        /// <param name="id">实体全局 Id。</param>
        /// <param name="version">实体版本号。</param>
        public EntityInfo(int id, ushort version)
        {
            Id = id;
            Version = version;
        }

        /// <summary>
        /// 比较两个实体信息是否表示同一实体（Id 与 Version 均相同）。
        /// </summary>
        /// <param name="other">要比较的另一个实体信息。</param>
        /// <returns>相等返回 true，否则返回 false。</returns>
        public bool Equals(EntityInfo other) => Id == other.Id && Version == other.Version;

        /// <summary>
        /// 与指定对象比较相等性。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>若对象为 EntityInfo 且等同则返回 true，否则返回 false。</returns>
        public override bool Equals(object? obj) => obj is EntityInfo other && Equals(other);

        /// <summary>
        /// 检查给定的 Entity 是否与当前 EntityInfo 表示相同且仍然匹配版本。
        /// </summary>
        /// <param name="entity">要比较的实体句柄。</param>
        /// <returns>匹配返回 true；否则返回 false。</returns>
        public bool IsAlive(Entity entity) => Id == entity.Id && Version == entity.Version;

        /// <summary>
        /// 生成当前 EntityInfo 的哈希码（基于 Id 与 Version）。
        /// </summary>
        /// <returns>哈希码。</returns>
        public override int GetHashCode() => HashCode.Combine(Id, Version);

        /// <summary>
        /// 判断两个实体信息是否相等。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public static bool operator ==(EntityInfo left, EntityInfo right) => left.Equals(right);

        /// <summary>
        /// 判断两个实体信息是否不相等。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>不相等返回 true；否则返回 false。</returns>
        public static bool operator !=(EntityInfo left, EntityInfo right) => !left.Equals(right);

        /// <summary>
        /// 隐式从 Entity 转换为 EntityInfo（复制 Id 与 Version）。
        /// </summary>
        /// <param name="entity">源 Entity。</param>
        public static implicit operator EntityInfo(Entity entity) => new(entity.Id, entity.Version);

        /// <summary>
        /// 隐式将 EntityInfo 转换为 int（返回 Id）。
        /// </summary>
        /// <param name="info">源 EntityInfo。</param>
        public static implicit operator int(EntityInfo info) => info.Id;

        /// <summary>
        /// 隐式将 EntityInfo 转换为 ushort（返回 Version）。
        /// </summary>
        /// <param name="info">源 EntityInfo。</param>
        public static implicit operator ushort(EntityInfo info) => info.Version;

        /// <summary>
        /// 隐式将 EntityInfo 转换为 Entity（根据 Id 与 Version 构建）。
        /// </summary>
        /// <param name="entityInfo">源 EntityInfo。</param>
        public static implicit operator Entity(EntityInfo entityInfo) => new(entityInfo.Id, entityInfo.Version);
    }
}