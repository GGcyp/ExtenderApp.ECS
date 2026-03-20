using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在全局范围内遍历 EntityQuery 匹配到的所有实体组件（针对组件类型 T）的访问器。
    ///
    /// 提供三种层级的枚举器：
    /// - `Enumerator`：按组件值（T）逐个枚举，返回组件副本（值语义），适用于只读取场景的简洁遍历。
    /// - `ComponentAccessorEnumerator`：按块（`ComponentAccessor{T}`）枚举，每次返回一个块访问器，便于按索引/引用进行批量或随机访问。
    /// - `ArchetypeChunkAccessorEnumerator`：按匹配的 archetype 枚举，返回对应列的 `ArchetypeChunkAccessor{T}`，便于获取整个块链进行批量操作。
    ///
    /// 语义：`EntityQueryAccessor{T}` 由查询构建时传入的 `ArchetypeMatch[]` 初始化。外部调用者可根据查询中组件的位置（列索引）创建对应列的枚举器。
    /// </summary>
    /// <typeparam name="T">组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
    public struct EntityQueryAccessor<T> where T : struct, IComponent
    {
        private readonly ArchetypeMatch[] _archetypeMatchs;
        private readonly int _columnIndex;
        private readonly ulong _version;

        /// <summary>
        /// 初始化一个 <see cref="EntityQueryAccessor{T}"/> 实例。
        /// </summary>
        /// <param name="archetypeMatchs">用于查询的原型匹配数组。</param>
        /// <param name="columnIndex">查询中组件的列索引。</param>
        internal EntityQueryAccessor(ArchetypeMatch[] archetypeMatchs, int columnIndex, ulong version)
        {
            _archetypeMatchs = archetypeMatchs;
            _columnIndex = columnIndex;
            _version = version;
        }

        /// <summary>
        /// 获取按值枚举的枚举器（针对查询中指定的列索引）。
        /// 每次枚举返回一个组件副本（值语义）。
        /// </summary>
        /// <returns>用于遍历该列所有组件值的 `Enumerator`。</returns>
        public Enumerator GetEnumerator() => new(_archetypeMatchs, _columnIndex, _version);

        /// <summary>
        /// 获取按块访问器枚举器（针对查询中指定的列索引）。
        /// 每次枚举返回一个 `ComponentAccessor{T}`，代表某个块中的该列访问器，适合按局部索引获取引用/值或批量处理块中元素。
        /// </summary>
        /// <returns>用于遍历该列所有块访问器的 `ComponentAccessorEnumerator`。</returns>
        public ComponentAccessorEnumerator GetComponentAccessorEnumerator() => new(_archetypeMatchs, _columnIndex, _version);

        /// <summary>
        /// 获取按 archetype 层级的区块访问器枚举器（针对查询中指定的列索引）。
        /// 每次枚举返回一个 `ArchetypeChunkAccessor{T}`，表示匹配 archetype 上该列的所有块集合。
        /// </summary>
        /// <returns>用于按 archetype 遍历块集合的 `ArchetypeChunkAccessorEnumerator`。</returns>
        internal ArchetypeChunkAccessorEnumerator GetArchetypeChunkAccessorEnumerator() => new(_archetypeMatchs, _columnIndex, _version);

        /// <summary>
        /// 获取按值枚举的只读引用枚举器（针对查询中指定的列索引）。
        /// </summary>
        /// <returns>返回 <see cref="RefROEnumerator"/> 实例</returns>
        public RefROEnumerator GetRefROs() => new(_archetypeMatchs, _columnIndex, _version);

        /// <summary>
        /// 获取按值枚举的可写引用枚举器（针对查询中指定的列索引）。
        /// </summary>
        /// <returns>返回 <see cref="RefRWEnumerator"/> 实例</returns>
        public RefRWEnumerator GetRefRWs() => new(_archetypeMatchs, _columnIndex, _version);

        /// <summary>
        /// 值枚举器：按块链遍历并按元素返回组件副本（T）。
        /// 设计为 struct 以避免堆分配，适合在热路径中使用。
        /// </summary>
        public struct Enumerator : IStructEnumerator<T>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.Enumerator accessorEnumerator;

            /// <summary>
            /// 当前项：组件值副本（T）。在 <see cref="MoveNext"/> 返回 true 后有效。
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// 内部构造函数：基于匹配数组和列索引初始化值枚举器的状态。
            /// </summary>
            /// <param name="matches">用于遍历的 ArchetypeMatch 数组。</param>
            /// <param name="version">查询版本，用于验证枚举器的有效性。</param>
            /// <param name="columnIndex">目标组件在查询中的列索引。</param>
            internal Enumerator(ArchetypeMatch[] matches, int columnIndex, ulong version)
            {
                enumerator = new(matches, columnIndex, version);
                accessorEnumerator = default;
            }

            /// <summary>
            /// 将枚举器推进到下一个可用组件值位置。
            /// </summary>
            /// <returns>若成功推进并使 <see cref="Current"/> 可用则返回 true；否则返回 false（遍历结束）。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (accessorEnumerator.MoveNext())
                    {
                        Current = accessorEnumerator.Current;
                        return true;
                    }

                    if (enumerator.MoveNext())
                    {
                        accessorEnumerator = enumerator.Current.GetEnumerator();
                        continue;
                    }
                    return false;
                }
            }

            /// <summary>
            /// 为当前枚举器与一个可写引用枚举器 <paramref name="refRW"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量写入操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRW">可写引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRW"/> 的多播枚举器。</returns>
            public MulticastEnumerator<Enumerator, T, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefRWEnumerator refRW)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<Enumerator, T, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>>(this, refRW);

            /// <summary>
            /// 为当前枚举器与一个只读引用枚举器 <paramref name="refRO"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时读取连接的实体的只读数据。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRO">只读引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRO"/> 的多播枚举器。</returns>
            public MulticastEnumerator<Enumerator, T, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefROEnumerator refRO)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<Enumerator, T, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>>(this, refRO);

            /// <summary>
            /// 为当前枚举器与另一个组件枚举器 <paramref name="enumerator"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="enumerator">另一个组件枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="enumerator"/> 的多播枚举器。</returns>
            public MulticastEnumerator<Enumerator, T, EntityQueryAccessor<TJoin>.Enumerator, TJoin> Join<TJoin>(EntityQueryAccessor<TJoin>.Enumerator enumerator)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<Enumerator, T, EntityQueryAccessor<TJoin>.Enumerator, TJoin>(this, enumerator);
        }

        /// <summary>
        /// 按 archetype 层级返回 `ArchetypeChunkAccessor{T}` 的枚举器。
        /// 每次返回的 `ArchetypeChunkAccessor{T}` 表示匹配 archetype 上该列的所有块集合（以列表形式提供）。
        /// </summary>
        internal struct ArchetypeChunkAccessorEnumerator
        {
            private readonly ArchetypeMatch[] _matches;
            private readonly ulong _version;
            private int _matchIndex;
            private int _columnIndex;
            private ArchetypeChunkList<T> current;

            /// <summary>
            /// 当前项：返回 `ArchetypeChunkAccessor{T}`（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// </summary>
            public ArchetypeChunkAccessor<T> Current => new(current, _version);

            internal ArchetypeChunkAccessorEnumerator(ArchetypeMatch[] matches, int columnIndex, ulong version)
            {
                _matches = matches;
                _matchIndex = 0;
                _columnIndex = columnIndex;
                _version = version;
                current = default!;
            }

            /// <summary>
            /// 将枚举器推进到下一个匹配的 archetype，并准备返回对应的 `ArchetypeChunkAccessor{T}`。
            /// </summary>
            /// <returns>若找到下一个匹配并使 <see cref="Current"/> 可用则返回 true；否则返回 false（遍历结束）。</returns>
            public bool MoveNext()
            {
                if (_matchIndex >= _matches.Length)
                    return false;

                var math = _matches[_matchIndex++];
                return math.Archetype.TryGetChunkList(math.ColumnIndices[_columnIndex], out current);
            }
        }

        /// <summary>
        /// 按块返回 `ComponentAccessor{T}` 的枚举器（每次返回一个块访问器）。
        /// 适用于需要在块级别进行索引或引用访问的场景。
        /// </summary>
        public struct ComponentAccessorEnumerator
        {
            private readonly ulong _version;
            private ArchetypeChunkAccessorEnumerator accessor;
            private ArchetypeChunkAccessor<T>.Enumerator enumerator;

            /// <summary>
            /// 当前项：返回 `ComponentAccessor{T}`（在 <see cref="MoveNext"/> 返回 true 后有效）。
            /// 使用该访问器可调用 `GetValue` / `GetRefRO` / `GetRefRW` 等方法访问块内元素。
            /// </summary>
            public ComponentAccessor<T> Current { get; private set; }

            /// <summary>
            /// 内部构造函数：基于匹配数组和列索引初始化块访问器枚举器的状态。
            /// </summary>
            /// <param name="matches">用于遍历的 ArchetypeMatch 数组。</param>
            /// <param name="columnIndex">目标组件在查询中的列索引。</param>
            internal ComponentAccessorEnumerator(ArchetypeMatch[] matches, int columnIndex, ulong version)
            {
                _version = version;
                accessor = new(matches, columnIndex, version);
                enumerator = default;
                Current = default;
            }

            /// <summary>
            /// 将枚举器推进到下一个包含至少一个元素的块，并使 <see cref="Current"/> 可用。
            /// </summary>
            /// <returns>若成功推进并使 <see cref="Current"/> 可用则返回 true；否则返回 false（遍历结束）。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (enumerator.MoveNext())
                    {
                        Current = enumerator.Current;
                        return true;
                    }

                    if (accessor.MoveNext())
                    {
                        enumerator = accessor.Current.GetEnumerator(_version);
                        continue;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// 枚举器：返回每个元素的只读包装（RefRO&lt;T&gt;），仅遍历当前原型。
        /// </summary>
        public struct RefROEnumerator : IStructEnumerator<RefRO<T>>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.RefROEnumerator accessorEnumerator;
            public RefRO<T> Current { get; private set; }

            /// <summary>
            /// 内部构造函数：基于匹配数组和列索引初始化只读引用枚举器的状态。
            /// </summary>
            /// <param name="matches">用于遍历的 ArchetypeMatch 数组。</param>
            /// <param name="columnIndex">目标组件在查询中的列索引。</param>
            internal RefROEnumerator(ArchetypeMatch[] matches, int columnIndex, ulong version)
            {
                enumerator = new(matches, columnIndex, version);
                accessorEnumerator = default;
            }

            /// <summary>
            /// 将枚举器推进到下一个元素位置并使 <see cref="Current"/> 可用（只读引用枚举器）。
            /// </summary>
            /// <returns>若成功推进并使 <see cref="Current"/> 可用则返回 true；否则返回 false（遍历结束）。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (accessorEnumerator.MoveNext())
                    {
                        Current = accessorEnumerator.Current;
                        return true;
                    }

                    if (enumerator.MoveNext())
                    {
                        accessorEnumerator = enumerator.Current.GetRefROs();
                        continue;
                    }
                    return false;
                }
            }

            /// <summary>
            /// 为当前只读引用枚举器与一个可写引用枚举器 <paramref name="refRW"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量写入操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRW">可写引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRW"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefRWEnumerator refRW)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>>(this, refRW);

            /// <summary>
            /// 为当前只读引用枚举器与一个只读引用枚举器 <paramref name="refRO"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时读取连接的实体的只读数据。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRO">只读引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRO"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefROEnumerator refRO)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>>(this, refRO);

            /// <summary>
            /// 为当前只读引用枚举器与另一个组件枚举器 <paramref name="enumerator"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="enumerator">另一个组件枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="enumerator"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.Enumerator, TJoin> Join<TJoin>(EntityQueryAccessor<TJoin>.Enumerator enumerator)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefROEnumerator, RefRO<T>, EntityQueryAccessor<TJoin>.Enumerator, TJoin>(this, enumerator);
        }

        /// <summary>
        /// 枚举器：返回每个元素的可写包装（RefRW&lt;T&gt;），仅遍历当前原型。
        /// </summary>
        public struct RefRWEnumerator : IStructEnumerator<RefRW<T>>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.RefRWEnumerator accessorEnumerator;
            public RefRW<T> Current { get; private set; }

            /// <summary>
            /// 内部构造函数：基于匹配数组和列索引初始化可写引用枚举器的状态。
            /// </summary>
            /// <param name="matches">用于遍历的 ArchetypeMatch 数组。</param>
            /// <param name="columnIndex">目标组件在查询中的列索引。</param>
            internal RefRWEnumerator(ArchetypeMatch[] matches, int columnIndex, ulong version)
            {
                enumerator = new(matches, columnIndex, version);
                accessorEnumerator = default;
            }

            /// <summary>
            /// 将枚举器推进到下一个元素位置并使 <see cref="Current"/> 可用（可写引用枚举器）。
            /// </summary>
            /// <returns>若成功推进并使 <see cref="Current"/> 可用则返回 true；否则返回 false（遍历结束）。</returns>
            public bool MoveNext()
            {
                while (true)
                {
                    if (accessorEnumerator.MoveNext())
                    {
                        Current = accessorEnumerator.Current;
                        return true;
                    }

                    if (enumerator.MoveNext())
                    {
                        accessorEnumerator = enumerator.Current.GetRefRWs();
                        continue;
                    }
                    return false;
                }
            }

            /// <summary>
            /// 为当前可写引用枚举器与一个可写引用枚举器 <paramref name="refRW"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量写入操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRW">可写引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRW"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefRWEnumerator refRW)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.RefRWEnumerator, RefRW<TJoin>>(this, refRW);

            /// <summary>
            /// 为当前可写引用枚举器与一个只读引用枚举器 <paramref name="refRO"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时读取连接的实体的只读数据。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="refRO">只读引用枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="refRO"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>> Join<TJoin>(EntityQueryAccessor<TJoin>.RefROEnumerator refRO)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.RefROEnumerator, RefRO<TJoin>>(this, refRO);

            /// <summary>
            /// 为当前可写引用枚举器与另一个组件枚举器 <paramref name="enumerator"/> 创建一个多播枚举器。
            ///
            /// 可用于在遍历的同时对连接的实体执行批量操作。
            /// </summary>
            /// <typeparam name="TJoin">连接的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
            /// <param name="enumerator">另一个组件枚举器。</param>
            /// <returns>返回包含当前枚举器与 <paramref name="enumerator"/> 的多播枚举器。</returns>
            public MulticastEnumerator<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.Enumerator, TJoin> Join<TJoin>(EntityQueryAccessor<TJoin>.Enumerator enumerator)
                where TJoin : struct, IComponent
                => MulticastEnumerator.Create<RefRWEnumerator, RefRW<T>, EntityQueryAccessor<TJoin>.Enumerator, TJoin>(this, enumerator);
        }
    }
}