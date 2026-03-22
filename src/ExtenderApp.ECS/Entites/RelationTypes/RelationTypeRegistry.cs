using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 关系类型注册表。
    /// 负责为关系类型分配唯一索引并维护索引到类型缓存的映射。
    /// </summary>
    internal static class RelationTypeRegistry
    {
        private const int CurrentMaxRelationTypes = 64;

        private static List<RelationTypeCache> relationTypes = new(CurrentMaxRelationTypes);

        /// <summary>
        /// 获取或创建关系类型 <typeparamref name="T"/> 对应的 <see cref="RelationType"/>。
        /// </summary>
        public static RelationType RegisterRelationType<T>() where T : struct => new(RelationTypeCache<T>.Instance);

        /// <summary>
        /// 按索引获取关系类型。
        /// </summary>
        public static RelationType GetRelationType(int index) => new(relationTypes[index]);

        /// <summary>
        /// 关系类型缓存抽象基类。
        /// </summary>
        public abstract class RelationTypeCache
        {
            public static readonly RelationTypeCache Empty = new EmptyRelationTypeCache();

            public abstract ushort Index { get; }
            public abstract string Name { get; }
            public abstract Type RelationDataType { get; }
        }

        /// <summary>
        /// 空关系类型缓存。
        /// </summary>
        public sealed class EmptyRelationTypeCache : RelationTypeCache
        {
            public override ushort Index => 0;
            public override string Name => "Empty";
            public override Type RelationDataType => typeof(void);
        }

        /// <summary>
        /// 泛型关系类型缓存。
        /// </summary>
        private sealed class RelationTypeCache<T> : RelationTypeCache where T : struct
        {
            public static readonly RelationTypeCache<T> Instance = new RelationTypeCache<T>();

            private readonly ushort index;

            public override ushort Index => index;
            public override string Name => typeof(T).Name;
            public override Type RelationDataType => typeof(T);

            public RelationTypeCache()
            {
                index = (ushort)relationTypes.Count;
                relationTypes.Add(this);
            }
        }
    }
}