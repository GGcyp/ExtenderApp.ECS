using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在全局范围内遍历 EntityQuery 匹配到的所有实体组件（针对组件类型 T1）的访问器。
    /// 组件列位置不预先固定缓存，而是在每个 Archetype 上按组件类型动态解析。
    /// </summary>
    /// <typeparam name="T">要访问的组件类型（值类型）</typeparam>
    internal struct ArchetypeAccessor<T> where T : struct
    {
        /// <summary>
        /// 所有参与查询的 Archetype 链表头（按匹配顺序链接的 ArchetypeSegment）。
        /// </summary>
        private readonly ArchetypeSegment? _nextSegment;

        /// <summary>
        /// 要访问的组件类型的封装表示。
        /// </summary>
        private readonly ComponentType _componentType;

        /// <summary>
        /// 变更版本（用于只读/写入版本控制），当 traverseAll 为 true 时会被忽略。
        /// </summary>
        private readonly ulong _version;

        /// <summary>
        /// 构造 ArchetypeAccessor 实例。
        /// </summary>
        /// <param name="nextSegment">参与查询的 Archetype 链表头。</param>
        /// <param name="componentType">要访问的组件类型。</param>
        /// <param name="version">当前查询/访问的版本号（用于版本/并发控制）。</param>
        internal ArchetypeAccessor(ArchetypeSegment? nextSegment, ComponentType componentType, ulong version)
        {
            _nextSegment = nextSegment;
            _componentType = componentType;
            _version = version;
        }

        /// <summary>
        /// 获取用于按组件值遍历的枚举器（值副本）。
        /// 用法示例：foreach (var component in accessor) { ... }
        /// </summary>
        public Enumerator GetEnumerator() => new(GetComponentAccessorEnumerator());

        /// <summary>
        /// 获取按组件列访问器枚举器（每个元素为 ComponentAccessor&lt;T&gt;，代表某一 Archetype 的一个 ChunkList）。
        /// 适合需要在 Chunk 级别进行批量处理的场景。
        /// </summary>
        public ComponentAccessorEnumerator GetComponentAccessorEnumerator() => new(GetArchetypeChunkAccessorEnumerator());

        /// <summary>
        /// 内部：获取 ArchetypeChunk 级别的访问器枚举器。
        /// 此枚举器负责在各个 Archetype 中定位包含目标组件类型的 Chunk 列。
        /// </summary>
        internal ArchetypeChunkAccessorEnumerator GetArchetypeChunkAccessorEnumerator() => new(_nextSegment, _componentType, _version);

        /// <summary>
        /// 获取按引用读写的枚举器（RefRW&lt;T&gt;），用于在迭代中对组件进行就地修改。
        /// 用法示例：foreach (var refRw in accessor.GetRefRWs()) { refRw.Value = ...; }
        /// </summary>
        public RefRWEnumerator GetRefRWs() => new(GetComponentAccessorEnumerator());

        /// <summary>
        /// 按值遍历组件的枚举器（返回 T 的副本）。
        /// </summary>
        public struct Enumerator
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.Enumerator accessorEnumerator;
            public T Current { get; private set; }

            internal Enumerator(ComponentAccessorEnumerator enumerator)
            {
                this.enumerator = enumerator;
                accessorEnumerator = default;
            }

            /// <summary>
            /// 将枚举移动到下一个组件值（跨 Archetype/ChunkList 自动切换）。
            /// </summary>
            public bool MoveNext()
            {
                while (true)
                {
                    if (accessorEnumerator.MoveNext())
                    {
                        Current = accessorEnumerator.Current;
                        return true;
                    }

                    if (!enumerator.MoveNext())
                        return false;

                    accessorEnumerator = enumerator.Current.GetEnumerator();
                }
            }
        }

        /// <summary>
        /// 按引用读写遍历组件的枚举器。返回 RefRW&lt;T&gt;，支持就地修改组件。
        /// </summary>
        public struct RefRWEnumerator
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.RefRWEnumerator accessorEnumerator;
            public RefRW<T> Current { get; private set; }

            internal RefRWEnumerator(ComponentAccessorEnumerator enumerator)
            {
                this.enumerator = enumerator;
                accessorEnumerator = default;
            }

            /// <summary>
            /// 将枚举移动到下一个 RefRW 项（跨 Archetype/ChunkList 自动切换）。
            /// </summary>
            public bool MoveNext()
            {
                while (true)
                {
                    if (accessorEnumerator.MoveNext())
                    {
                        Current = accessorEnumerator.Current;
                        return true;
                    }

                    if (!enumerator.MoveNext())
                        return false;

                    accessorEnumerator = enumerator.Current.GetRefRWs();
                }
            }
        }

        /// <summary>
        /// 表示按 ArchetypeChunkList 分组的 ComponentAccessor 的枚举器。
        /// 每次 MoveNext 返回一个 ComponentAccessor&lt;T&gt;，代表某个 Archetype 上的一段连续组件数据（ChunkList）。
        /// </summary>
        public struct ComponentAccessorEnumerator
        {
            private ArchetypeChunkAccessorEnumerator accessor;
            private ArchetypeChunkAccessor<T>.Enumerator enumerator;
            public ComponentAccessor<T> Current { get; private set; }

            internal ComponentAccessorEnumerator(ArchetypeChunkAccessorEnumerator accessor)
            {
                this.accessor = accessor;
                enumerator = default;
                Current = default;
            }

            /// <summary>
            /// 将枚_enum移到下一个 ComponentAccessor（即下一个 ChunkList 的组件列访问器）。
            /// </summary>
            public bool MoveNext()
            {
                while (true)
                {
                    if (enumerator.MoveNext())
                    {
                        Current = enumerator.Current;
                        return true;
                    }

                    if (!accessor.MoveNext())
                        return false;

                    enumerator = accessor.Current.GetEnumerator();
                }
            }
        }

        /// <summary>
        /// 内部枚举器：遍历所有 Archetype，定位包含目标组件类型的 ChunkList 并按 Archetype 返回它们。
        /// 注意：此枚举器会在每个 Archetype 上动态解析组件列位置（而非预先缓存），以支持组件布置差异。
        /// </summary>
        internal struct ArchetypeChunkAccessorEnumerator
        {
            private readonly ComponentType _componentType;
            private readonly ulong _version;
            private ArchetypeChunkList<T> current;
            private ArchetypeSegment? nextSegment;

            /// <summary>
            /// 当前返回的 ArchetypeChunkAccessor（包装了当前的 ChunkList 和版本信息）。
            /// </summary>
            public ArchetypeChunkAccessor<T> Current => new(current, _version);

            internal ArchetypeChunkAccessorEnumerator(ArchetypeSegment? head, ComponentType componentType, ulong version)
            {
                _componentType = componentType;
                _version = version;
                current = default!;
                nextSegment = head;
            }

            /// <summary>
            /// 遍历下一个包含目标组件类型的 ChunkList。返回 true 表示找到并设置了 Current。
            /// </summary>
            public bool MoveNext()
            {
                var segment = nextSegment;
                while (segment != null)
                {
                    var archetype = segment.Archetype;
                    // 在当前 Archetype 上查找目标组件的编码列索引
                    // 获取该列对应的 ChunkList
                    if (!archetype.ComponentMask.TryGetEncodedPosition(_componentType, out var columnIndex) ||
                        !archetype.TryGetChunkList(columnIndex, out current))
                    {
                        segment = segment.Next;
                        continue;
                    }

                    nextSegment = segment.Next;
                    return true;
                }

                // 未找到匹配项，确保 nextSegment 指向末尾（null）
                nextSegment = null;
                return false;
            }
        }
    }
}