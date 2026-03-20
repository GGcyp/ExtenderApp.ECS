namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 描述一个实体查询的过滤条件（查询描述符）。
    ///
    /// Query 描述由三个组件掩码组成：
    /// - All：要求实体必须包含的组件集合（全部匹配）；
    /// - Any：实体至少包含其中之一的组件集合；
    /// - None：实体必须不包含的组件集合（排除）。
    ///
    /// 该类型用于作为查询缓存的键或比较依据，以便复用相同查询描述的查询结果。
    /// </summary>
    internal struct EntityQueryDesc : IEquatable<EntityQueryDesc>
    {
        /// <summary>
        /// 获取表示查询条件的组件掩码引用（只读）。
        /// </summary>
        public readonly ComponentMask Query;

        /// <summary>
        /// 获取表示必须全部匹配的组件掩码引用（只读）。
        /// </summary>
        public readonly ComponentMask All;

        /// <summary>
        /// 获取表示任意匹配的组件掩码引用（只读）。
        /// </summary>
        public readonly ComponentMask Any;

        /// <summary>
        /// 获取表示必须不包含的组件掩码引用（只读）。
        /// </summary>
        public readonly ComponentMask None;

        /// <summary>
        /// 获取查询描述中是否包含 All 掩码。
        /// </summary>
        public bool HasAll { get; }

        /// <summary>
        /// 获取查询描述中是否包含 Any 掩码。
        /// </summary>
        public bool HasAny { get; }

        /// <summary>
        /// 获取查询描述中是否包含 None 掩码。
        /// </summary>
        public bool HasNone { get; }

        /// <summary>
        /// 获取查询条件的总数。
        /// </summary>
        public int ComponentCount { get; }

        /// <summary>
        /// 使用指定的 All/Any/None 掩码创建一个新的查询描述符实例。
        /// </summary>
        /// <param name="all">表示必须全部包含的组件掩码（可为空掩码）。</param>
        /// <param name="any">表示至少包含其一的组件掩码（可为空掩码）。</param>
        /// <param name="none">表示必须不包含的组件掩码（可为空掩码）。</param>
        public EntityQueryDesc(ComponentMask query, ComponentMask all, ComponentMask any, ComponentMask none)
        {
            Query = query;
            All = all;
            Any = any;
            None = none;

            HasAll = !all.IsEmpty;
            HasAny = !any.IsEmpty;
            HasNone = !none.IsEmpty;

            ComponentCount = query.ComponentCount;
        }

        /// <summary>
        /// 重写 Object.Equals，用于与其他对象比较是否表示相同的查询描述。
        /// </summary>
        public override bool Equals(object? obj)
            => obj is EntityQueryDesc other && Equals(other);

        /// <summary>
        /// 比较两个 <see cref="EntityQueryDesc"/> 是否等价（三个掩码逐一相等）。
        /// </summary>
        public bool Equals(EntityQueryDesc other)
            => All.Equals(other.All) &&
               Any.Equals(other.Any) &&
               None.Equals(other.None);

        /// <summary>
        /// 根据三个掩码计算哈希码，便于作为字典或集合的键。
        /// </summary>
        public override int GetHashCode()
            => HashCode.Combine(Query, All, Any, None);

        /// <summary>
        /// 返回便于调试的字符串表示，包含 All/Any/None 的描述。
        /// </summary>
        public override string ToString()
            => $"EntityQueryDesc(Query: {Query}, All: {All}, Any: {Any}, None: {None})";

        /// <summary>
        /// 相等运算符重载，按查询描述相等性判断。
        /// </summary>
        public static bool operator ==(EntityQueryDesc left, EntityQueryDesc right) => left.Equals(right);

        /// <summary>
        /// 不等运算符重载。
        /// </summary>
        public static bool operator !=(EntityQueryDesc left, EntityQueryDesc right) => !left.Equals(right);
    }
}