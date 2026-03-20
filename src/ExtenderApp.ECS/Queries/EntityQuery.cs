using System.Buffers;
using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 一个简单的 EntityQuery 实现：
    /// - 构建目标 ComponentMask
    /// - 在指定 World 的 ArchetypeManager 中查找所有匹配的 Archetype（All, Any, None 可在未来扩展）
    /// - 为每个匹配的 Archetype 提供一个按-current 访问的枚举器
    /// </summary>
    public abstract class EntityQuery : DisposableObject, IDisposable
    {
        /// <summary>
        /// 当前实际的所有组件管理器
        /// </summary>
        private readonly ArchetypeDictionary _archetypeDict;

        /// <summary>
        /// 当前查询所属的 World 的版本管理器，用于监听 ArchetypeManager 版本变化以更新匹配结果。
        /// </summary>
        internal readonly WorldVersionManager WVManager;

        /// <summary>
        /// 当前查询的描述信息，包含查询所需的组件类型列表以及查询模式（All/Any/None）。该描述在 EntityQuery 创建时提供，并用于匹配 Archetype。
        /// </summary>
        private readonly EntityQueryDesc _desc;

        /// <summary>
        /// 当前查询匹配的 Archetype 列表以及每个匹配项中查询组件的列索引映射。该列表在 ArchetypeManager 版本变化时重建。
        /// </summary>
        private ArchetypeMatch[] archetypeMatchs;

        /// <summary>
        /// 当前匹配结果对应的 ArchetypeManager 版本号，用于判断何时需要重建匹配列表。
        /// </summary>
        private ulong Version;

        /// <summary>
        /// 强制在下一次访问匹配结果时重建匹配列表（即使版本未变化）。该属性可用于在外部强制刷新查询结果，例如在已知 ArchetypeManager 发生了变化但版本未更新的特殊情况下。
        /// </summary>
        public bool ForceRun { get; set; }

        /// <summary>
        /// 构造一个 EntityQuery 实例并保存 ArchetypeManager 与查询描述。
        /// </summary>
        /// <param name="dict">用于检索 Archetype 的组件管理器。</param>
        /// <param name="worldVersionManager">用于监听 World 版本变化以更新匹配结果的版本管理器。</param>
        /// <param name="desc">描述查询所需的组件掩码与顺序。</param>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc)
        {
            WVManager = worldVersionManager;
            _archetypeDict = dict;
            _desc = desc;
            archetypeMatchs = Array.Empty<ArchetypeMatch>();
            Version = 0;
        }

        /// <summary>
        /// 获取当前匹配的 ArchetypeMatch 列表。该方法要求在主线程调用并确保匹配缓存是最新的。
        /// </summary>
        internal ArchetypeMatch[] GetArchetypeMatches()
        {
            MainThreadDetector.ThrowIfNotMainThread();
            EnsureMatches();
            return archetypeMatchs;
        }

        /// <summary>
        /// 确保缓存的匹配结果与 ArchetypeManager 的版本一致；若不一致则重建匹配项。
        /// </summary>
        private void EnsureMatches()
        {
            if (Version == WVManager.ArchetypeVersion)
                return;

            RebuildMatches();
            Version = WVManager.ArchetypeVersion;
        }

        /// <summary>
        /// 重建匹配的 Archetype 列表以及每个匹配项的列索引映射。
        /// 遍历 ArchetypeManager 中的所有 Archetype，根据查询描述进行过滤并生成 ArchetypeMatch。
        /// </summary>
        private void RebuildMatches()
        {
            foreach (var match in archetypeMatchs)
                match.Dispose();
            int lenth = _desc.ComponentCount;
            List<ArchetypeMatch> list = new();

            foreach (var archetype in _archetypeDict.Values)
            {
                if (MatchArchetype(archetype))
                {
                    var columnIndices = ArrayPool<int>.Shared.Rent(lenth);

                    int count = 0;
                    foreach (var componentType in _desc.Query)
                    {
                        archetype.ComponentTypes.TryGetEncodedPosition(componentType, out var columnIndex);
                        columnIndices[count++] = columnIndex;
                    }
                    list.Add(new ArchetypeMatch(archetype, columnIndices, lenth));
                }
            }
            archetypeMatchs = list.ToArray();
        }

        /// <summary>
        /// 判断给定 Archetype 是否满足查询描述（All/Any/None）。
        /// 注意：该方法在内部被频繁调用，应保持高性能。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchArchetype(Archetype archetype)
        {
            if (!archetype.ComponentTypes.All(_desc.All))
                return false;

            if (_desc.HasAll && !archetype.ComponentTypes.All(_desc.All))
                return false;

            if (_desc.HasAny && !archetype.ComponentTypes.Any(_desc.Any))
                return false;

            if (_desc.HasNone && !archetype.ComponentTypes.None(_desc.None))
                return false;

            return true;
        }

        /// <summary>
        /// 获取当前查询版本号（即 ArchetypeManager 版本），用于外部判断何时需要刷新查询结果。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ulong GetVersion() => ForceRun ? 0 : GetVersion();
    }

    public sealed class EntityQuery<T> : EntityQuery where T : struct, IComponent
    {
        /// <summary>
        /// 单泛型 EntityQuery 的构造器。
        /// </summary>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc) : base(dict, worldVersionManager, desc)
        {
        }

        /// <summary>
        /// 为类型 T 创建并返回一个 EntityQueryAccessor（索引 0）。
        /// </summary>
        private EntityQueryAccessor<T> GetAccessor() => new(GetArchetypeMatches(), 0, GetVersion());

        /// <summary>
        /// 获取用于按元素访问组件的 ComponentAccessorEnumerator（可用于 foreach 或手动枚举）。
        /// </summary>
        public EntityQueryAccessor<T>.ComponentAccessorEnumerator GetEnumerator() => GetAccessor().GetComponentAccessorEnumerator();

        /// <summary>
        /// 获取只读引用的枚举器，用于按实体读取 T 的只读引用（RefRO）。
        /// </summary>
        public EntityQueryAccessor<T>.RefROEnumerator GetRefROs() => GetAccessor().GetRefROs();

        /// <summary>
        /// 获取可写引用的枚举器，用于按实体读取/写入 T 的引用（RefRW）。
        /// </summary>
        public EntityQueryAccessor<T>.RefRWEnumerator GetRefRWs() => GetAccessor().GetRefRWs();
    }

    public sealed class EntityQuery<T1, T2> : EntityQuery
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        /// <summary>
        /// 两泛型 EntityQuery 的构造器。
        /// </summary>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc) : base(dict, worldVersionManager, desc)
        {
        }

        /// <summary>
        /// 为类型 T1 创建并返回一个 EntityQueryAccessor（索引 0）。
        /// </summary>
        private EntityQueryAccessor<T1> GetAccessorForT1() => new(GetArchetypeMatches(), 0, GetVersion());

        /// <summary>
        /// 为类型 T2 创建并返回一个 EntityQueryAccessor（索引 1）。
        /// </summary>
        private EntityQueryAccessor<T2> GetAccessorForT2() => new(GetArchetypeMatches(), 1, GetVersion());

        /// <summary>
        /// 返回一个组合枚举器包装，按顺序返回 (T1,T2) 对应的枚举器结果，便于 foreach 使用。
        /// </summary>
        public EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator> GetEnumerator()
            => GetEnumerator(GetEnumeratorForT1(), GetEnumeratorForT2());

        /// <summary>
        /// 使用自定义的结构体枚举器构造组合枚举器类型实例（用于提高性能的手工枚举器）。
        /// </summary>
        public EntityQueryEnumerator<TEnum1, TEnum2> GetEnumerator<TEnum1, TEnum2>(in TEnum1 enum1, in TEnum2 enum2)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            => new(enum1, enum2);

        /// <summary>
        /// 获取 T1 的枚举器实例（结构体枚举器）。
        /// </summary>
        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();

        /// <summary>
        /// 获取 T2 的枚举器实例（结构体枚举器）。
        /// </summary>
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();

        /// <summary>
        /// 获取 T1 的只读引用枚举器。
        /// </summary>
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();

        /// <summary>
        /// 获取 T2 的只读引用枚举器。
        /// </summary>
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();

        /// <summary>
        /// 获取 T1 的可写引用枚举器。
        /// </summary>
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();

        /// <summary>
        /// 获取 T2 的可写引用枚举器。
        /// </summary>
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();

        /// <summary>
        /// 多枚举器组合的封装结构，用于将两个结构体枚举器组合为同步枚举器。
        /// </summary>
        public struct EntityQueryEnumerator<TEnum1, TEnum2>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;

            /// <summary>
            /// 构造组合枚举器封装实例。
            /// </summary>
            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
            }

            /// <summary>
            /// 返回用于 foreach 的 MulticastEnumerator（同步推进两个枚举器并返回元组）。
            /// </summary>
            public MulticastEnumerator<TEnum1, T1, TEnum2, T2> GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2>(enum1, enum2);
        }
    }

    public sealed class EntityQuery<T1, T2, T3> : EntityQuery
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        /// <summary>
        /// 三泛型 EntityQuery 的构造器。
        /// </summary>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc) : base(dict, worldVersionManager, desc) { }

        /// <summary>为 T1 创建访问器（索引 0）。</summary>
        private EntityQueryAccessor<T1> GetAccessorForT1() => new(GetArchetypeMatches(), 0, GetVersion());

        /// <summary>为 T2 创建访问器（索引 1）。</summary>
        private EntityQueryAccessor<T2> GetAccessorForT2() => new(GetArchetypeMatches(), 1, GetVersion());

        /// <summary>为 T3 创建访问器（索引 2）。</summary>
        private EntityQueryAccessor<T3> GetAccessorForT3() => new(GetArchetypeMatches(), 2, GetVersion());

        /// <summary>
        /// 返回一个可用于 foreach 的三元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator> GetEnumerator()
            => GetEnumerator(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3());

        /// <summary>
        /// 使用自定义结构体枚举器构造三元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3> GetEnumerator<TEnum1, TEnum2, TEnum3>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            => new(enum1, enum2, enum3);

        /// <summary>获取 T1 的结构体枚举器。</summary>
        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();

        /// <summary>获取 T2 的结构体枚举器。</summary>
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();

        /// <summary>获取 T3 的结构体枚举器。</summary>
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();

        /// <summary>获取 T1 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();

        /// <summary>获取 T2 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();

        /// <summary>获取 T3 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();

        /// <summary>获取 T1 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();

        /// <summary>获取 T2 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();

        /// <summary>获取 T3 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();

        /// <summary>
        /// 三枚举器组合的封装结构，用于并行推进并返回三元组。
        /// </summary>
        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;

            /// <summary>构造三元组合枚举器封装。</summary>
            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
            }

            /// <summary>返回用于 foreach 的 MulticastEnumerator（三元组）。</summary>
            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3> GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3>(enum1, enum2, enum3);
        }
    }

    public sealed class EntityQuery<T1, T2, T3, T4> : EntityQuery
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        /// <summary>
        /// 四泛型 EntityQuery 的构造器。
        /// </summary>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc) : base(dict, worldVersionManager, desc) { }

        /// <summary>为 T1 创建访问器（索引 0）。</summary>
        private EntityQueryAccessor<T1> GetAccessorForT1() => new(GetArchetypeMatches(), 0, GetVersion());

        /// <summary>为 T2 创建访问器（索引 1）。</summary>
        private EntityQueryAccessor<T2> GetAccessorForT2() => new(GetArchetypeMatches(), 1, GetVersion());

        /// <summary>为 T3 创建访问器（索引 2）。</summary>
        private EntityQueryAccessor<T3> GetAccessorForT3() => new(GetArchetypeMatches(), 2, GetVersion());

        /// <summary>为 T4 创建访问器（索引 3）。</summary>
        private EntityQueryAccessor<T4> GetAccessorForT4() => new(GetArchetypeMatches(), 3, GetVersion());

        /// <summary>
        /// 返回一个可用于 foreach 的四元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator, EntityQueryAccessor<T4>.Enumerator> GetEnumerator()
            => GetEnumerator(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3(), GetEnumeratorForT4());

        /// <summary>
        /// 使用自定义结构体枚举器构造四元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4> GetEnumerator<TEnum1, TEnum2, TEnum3, TEnum4>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            => new(enum1, enum2, enum3, enum4);

        /// <summary>获取 T1 的结构体枚举器。</summary>
        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();

        /// <summary>获取 T2 的结构体枚举器。</summary>
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();

        /// <summary>获取 T3 的结构体枚举器。</summary>
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();

        /// <summary>获取 T4 的结构体枚举器。</summary>
        public EntityQueryAccessor<T4>.Enumerator GetEnumeratorForT4() => GetAccessorForT4().GetEnumerator();

        /// <summary>获取 T1 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();

        /// <summary>获取 T2 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();

        /// <summary>获取 T3 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();

        /// <summary>获取 T4 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T4>.RefROEnumerator GetRefROsForT4() => GetAccessorForT4().GetRefROs();

        /// <summary>获取 T1 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();

        /// <summary>获取 T2 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();

        /// <summary>获取 T3 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();

        /// <summary>获取 T4 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T4>.RefRWEnumerator GetRefRWsForT4() => GetAccessorForT4().GetRefRWs();

        /// <summary>
        /// 四枚举器组合的封装结构，用于并行推进并返回四元组。
        /// </summary>
        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;
            private TEnum4 enum4;

            /// <summary>构造四元组合枚举器封装。</summary>
            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3, TEnum4 enum4)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
                this.enum4 = enum4;
            }

            /// <summary>返回用于 foreach 的 MulticastEnumerator（四元组）。</summary>
            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4> GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4>(enum1, enum2, enum3, enum4);
        }
    }

    public sealed class EntityQuery<T1, T2, T3, T4, T5> : EntityQuery
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
        where T5 : struct, IComponent
    {
        /// <summary>
        /// 五泛型 EntityQuery 的构造器。
        /// </summary>
        internal EntityQuery(ArchetypeDictionary dict, WorldVersionManager worldVersionManager, EntityQueryDesc desc) : base(dict, worldVersionManager, desc) { }

        /// <summary>为 T1 创建访问器（索引 0）。</summary>
        private EntityQueryAccessor<T1> GetAccessorForT1() => new(GetArchetypeMatches(), 0, GetVersion());

        /// <summary>为 T2 创建访问器（索引 1）。</summary>
        private EntityQueryAccessor<T2> GetAccessorForT2() => new(GetArchetypeMatches(), 1, GetVersion());

        /// <summary>为 T3 创建访问器（索引 2）。</summary>
        private EntityQueryAccessor<T3> GetAccessorForT3() => new(GetArchetypeMatches(), 2, GetVersion());

        /// <summary>为 T4 创建访问器（索引 3）。</summary>
        private EntityQueryAccessor<T4> GetAccessorForT4() => new(GetArchetypeMatches(), 3, GetVersion());

        /// <summary>为 T5 创建访问器（索引 4）。</summary>
        private EntityQueryAccessor<T5> GetAccessorForT5() => new(GetArchetypeMatches(), 4, GetVersion());

        /// <summary>
        /// 返回一个可用于 foreach 的五元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator, EntityQueryAccessor<T4>.Enumerator, EntityQueryAccessor<T5>.Enumerator> GetEnumerator()
            => GetEnumerator(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3(), GetEnumeratorForT4(), GetEnumeratorForT5());

        /// <summary>
        /// 使用自定义结构体枚举器构造五元组合枚举器封装。
        /// </summary>
        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5> GetEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4, in TEnum5 enum5)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            where TEnum5 : struct, IStructEnumerator<T5>
            => new(enum1, enum2, enum3, enum4, enum5);

        /// <summary>获取 T1 的结构体枚举器。</summary>
        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();

        /// <summary>获取 T2 的结构体枚举器。</summary>
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();

        /// <summary>获取 T3 的结构体枚举器。</summary>
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();

        /// <summary>获取 T4 的结构体枚举器。</summary>
        public EntityQueryAccessor<T4>.Enumerator GetEnumeratorForT4() => GetAccessorForT4().GetEnumerator();

        /// <summary>获取 T5 的结构体枚举器。</summary>
        public EntityQueryAccessor<T5>.Enumerator GetEnumeratorForT5() => GetAccessorForT5().GetEnumerator();

        /// <summary>获取 T1 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();

        /// <summary>获取 T2 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();

        /// <summary>获取 T3 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();

        /// <summary>获取 T4 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T4>.RefROEnumerator GetRefROsForT4() => GetAccessorForT4().GetRefROs();

        /// <summary>获取 T5 的只读引用枚举器。</summary>
        public EntityQueryAccessor<T5>.RefROEnumerator GetRefROsForT5() => GetAccessorForT5().GetRefROs();

        /// <summary>获取 T1 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();

        /// <summary>获取 T2 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();

        /// <summary>获取 T3 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();

        /// <summary>获取 T4 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T4>.RefRWEnumerator GetRefRWsForT4() => GetAccessorForT4().GetRefRWs();

        /// <summary>获取 T5 的可写引用枚举器。</summary>
        public EntityQueryAccessor<T5>.RefRWEnumerator GetRefRWsForT5() => GetAccessorForT5().GetRefRWs();

        /// <summary>
        /// 五枚举器组合的封装结构，用于并行推进并返回五元组。
        /// </summary>
        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            where TEnum5 : struct, IStructEnumerator<T5>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;
            private TEnum4 enum4;
            private TEnum5 enum5;

            /// <summary>构造五元组合枚举器封装。</summary>
            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3, TEnum4 enum4, TEnum5 enum5)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
                this.enum4 = enum4;
                this.enum5 = enum5;
            }

            /// <summary>返回用于 foreach 的 MulticastEnumerator（五元组）。</summary>
            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4, TEnum5, T5> GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4, TEnum5, T5>(enum1, enum2, enum3, enum4, enum5);
        }
    }
}