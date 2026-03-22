using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 在全局范围内遍历 EntityQuery 匹配到的所有实体组件（针对组件类型 T）的访问器。
    /// 组件列位置不预先固定缓存，而是在每个 Archetype 上按组件类型动态解析。
    /// </summary>
    public struct EntityQueryAccessor<T> where T : struct
    {
        private readonly Archetype[] _archetypes;
        private readonly ComponentType _componentType;
        private readonly ulong _version;
        private bool traverseAll;

        internal EntityQueryAccessor(Archetype[] archetypes, ComponentType componentType, ulong version)
        {
            _archetypes = archetypes;
            _componentType = componentType;
            _version = version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetVersion() => traverseAll ? 0 : _version;

        public Enumerator GetEnumerator() => new(GetComponentAccessorEnumerator());
        public ComponentAccessorEnumerator GetComponentAccessorEnumerator() => new(GetArchetypeChunkAccessorEnumerator());
        internal ArchetypeChunkAccessorEnumerator GetArchetypeChunkAccessorEnumerator() => new(_archetypes, _componentType, GetVersion());
        public RefROEnumerator GetRefROs() => new(GetComponentAccessorEnumerator());
        public RefRWEnumerator GetRefRWs() => new(GetComponentAccessorEnumerator());

        public struct Enumerator : IStructEnumerator<T>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.Enumerator accessorEnumerator;
            public T Current { get; private set; }

            internal Enumerator(ComponentAccessorEnumerator enumerator)
            {
                this.enumerator = enumerator;
                accessorEnumerator = default;
            }

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

        public struct RefROEnumerator : IStructEnumerator<RefRO<T>>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.RefROEnumerator accessorEnumerator;
            public RefRO<T> Current { get; private set; }

            internal RefROEnumerator(ComponentAccessorEnumerator enumerator)
            {
                this.enumerator = enumerator;
                accessorEnumerator = default;
            }

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

                    accessorEnumerator = enumerator.Current.GetRefROs();
                }
            }
        }

        public struct RefRWEnumerator : IStructEnumerator<RefRW<T>>
        {
            private ComponentAccessorEnumerator enumerator;
            private ComponentAccessor<T>.RefRWEnumerator accessorEnumerator;
            public RefRW<T> Current { get; private set; }

            internal RefRWEnumerator(ComponentAccessorEnumerator enumerator)
            {
                this.enumerator = enumerator;
                accessorEnumerator = default;
            }

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

        public struct ComponentAndEntityAccessorEnumerator
        {
            private ArchetypeChunkEntityAccessor entityAccessor;
            private ArchetypeChunkAccessorEnumerator chunkAccessor;
            private ComponentAccessor<T>.Enumerator valueEnumerator;
            private ArchetypeChunkEntityAccessor.Enumerator entityEnumerator;
            public (T, Entity) Current { get; private set; }

            internal ComponentAndEntityAccessorEnumerator(ArchetypeChunkAccessorEnumerator accessor)
            {
                chunkAccessor = accessor;
                entityAccessor = default;
                valueEnumerator = default;
                entityEnumerator = default;
                Current = default;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (valueEnumerator.MoveNext() && entityEnumerator.MoveNext())
                    {
                        Current = (valueEnumerator.Current, entityEnumerator.Current);
                        return true;
                    }

                    if (!chunkAccessor.MoveNext())
                        return false;

                    var archetype = chunkAccessor.CurrentArchetype;
                    entityAccessor = new ArchetypeChunkEntityAccessor(archetype);
                    entityEnumerator = entityAccessor.GetEnumerator();
                    valueEnumerator = chunkAccessor.Current.GetEnumerator().Current.GetEnumerator();
                }
            }
        }

        internal struct ArchetypeChunkAccessorEnumerator
        {
            private readonly Archetype[] _archetypes;
            private readonly ComponentType _componentType;
            private readonly ulong _version;
            private ArchetypeChunkList<T> current;
            private int _matchIndex;
            private Archetype _currentArchetype;

            internal Archetype CurrentArchetype => _currentArchetype;
            public ArchetypeChunkAccessor<T> Current => new(current, _version);

            internal ArchetypeChunkAccessorEnumerator(Archetype[] archetypes, ComponentType componentType, ulong version)
            {
                _archetypes = archetypes;
                _componentType = componentType;
                _version = version;
                current = default!;
                _matchIndex = 0;
                _currentArchetype = default;
            }

            public bool MoveNext()
            {
                while (_matchIndex < _archetypes.Length)
                {
                    var archetype = _archetypes[_matchIndex++];
                    if (!archetype.ComponentTypes.TryGetEncodedPosition(_componentType, out var columnIndex))
                        continue;

                    if (!archetype.TryGetChunkList(columnIndex, out current))
                        continue;

                    _currentArchetype = archetype;
                    return true;
                }

                return false;
            }
        }
    }
}