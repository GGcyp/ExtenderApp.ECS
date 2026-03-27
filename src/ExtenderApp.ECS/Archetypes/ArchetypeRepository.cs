using System.Runtime.InteropServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// Archetype 仓库：
    /// - 查询路径使用字典（ComponentMask + RelationMask -&gt; Archetype）
    /// - 遍历路径使用列表（按插入顺序）
    /// </summary>
    internal class ArchetypeRepository : DisposableObject
    {
        private readonly Dictionary<ArchetypeKey, Archetype> _dictionary;
        private readonly List<Archetype> _list;

        /// <summary>
        /// 复合键：由组件掩码与关系掩码共同决定。
        /// </summary>
        private readonly struct ArchetypeKey : IEquatable<ArchetypeKey>
        {
            public readonly ComponentMask ComponentMask;
            public readonly RelationMask RelationMask;

            public ArchetypeKey(in ComponentMask componentMask, in RelationMask relationMask)
            {
                ComponentMask = componentMask;
                RelationMask = relationMask;
            }

            public bool Equals(ArchetypeKey other)
                => ComponentMask.Equals(other.ComponentMask) && RelationMask.Equals(other.RelationMask);

            public override bool Equals(object? obj)
                => obj is ArchetypeKey other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(ComponentMask, RelationMask);
        }

        /// <summary>
        /// 当前 Archetype 数量。
        /// </summary>
        public int Count => totalCount;

        private int totalCount;
        private int removeCount;

        public ArchetypeRepository()
        {
            _dictionary = new();
            _list = new();
        }

        /// <summary>
        /// 获取从指定索引开始的 Archetype 只读切片。
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <returns>从指定索引开始的 Archetype 只读切片</returns>
        public ReadOnlySpan<Archetype> GetArchetypeSpan(int startIndex) => CollectionsMarshal.AsSpan(_list).Slice(startIndex - removeCount);

        /// <summary>
        /// 按组件掩码（关系掩码为空）获取 Archetype。
        /// </summary>
        public bool TryGetValue(in ComponentMask componentMask, out Archetype archetype)
            => TryGetValue(componentMask, default, out archetype);

        /// <summary>
        /// 按组件掩码与关系掩码获取 Archetype。
        /// </summary>
        public bool TryGetValue(in ComponentMask componentMask, in RelationMask relationMask, out Archetype archetype)
            => _dictionary.TryGetValue(new(componentMask, relationMask), out archetype!);

        /// <summary>
        /// 使用键构造器查询 Archetype。
        /// </summary>
        public bool TryGetValue(in ArchetypeBuilder builder, out Archetype archetype)
            => TryGetValue(builder.ComponentMask, builder.RelationMask, out archetype);

        /// <summary>
        /// 按组件掩码索引 Archetype（关系掩码为空）。
        /// </summary>
        public Archetype this[ComponentMask componentMask]
        {
            get => this[componentMask, default];
            set => this[componentMask, default] = value;
        }

        /// <summary>
        /// 按组件掩码与关系掩码索引 Archetype。
        /// </summary>
        public Archetype this[ComponentMask componentMask, RelationMask relationMask]
        {
            get => _dictionary[new ArchetypeKey(componentMask, relationMask)];
            set
            {
                var key = new ArchetypeKey(componentMask, relationMask);
                if (_dictionary.TryGetValue(key, out var oldValue))
                {
                    _dictionary[key] = value;
                    if (!ReferenceEquals(oldValue, value))
                    {
                        int idx = _list.IndexOf(oldValue);
                        if (idx >= 0)
                            _list[idx] = value;
                    }
                    return;
                }

                _dictionary.Add(key, value);
                _list.Add(value);
                totalCount++;
            }
        }

        /// <summary>
        /// 使用键构造器索引 Archetype。
        /// </summary>
        public Archetype this[ArchetypeBuilder builder]
        {
            get => this[builder.ComponentMask, builder.RelationMask];
            set => this[builder.ComponentMask, builder.RelationMask] = value;
        }

        /// <summary>
        /// 删除指定键对应的 Archetype（关系掩码为空）。
        /// </summary>
        /// <param name="componentMask">指定组件掩码</param>
        /// <param name="archetype">输出被删除的 Archetype</param>
        /// <returns>如果删除成功返回 true，否则返回 false</returns>
        public bool Remove(in ComponentMask componentMask, out Archetype archetype)
        {
            return Remove(componentMask, default, out archetype);
        }

        /// <summary>
        /// 删除指定键对应的 Archetype。
        /// </summary>
        public bool Remove(in ComponentMask componentMask, in RelationMask relationMask, out Archetype archetype)
        {
            var key = new ArchetypeKey(componentMask, relationMask);
            if (!_dictionary.Remove(key, out archetype!))
                return false;

            _list.Remove(archetype);
            removeCount++;
            return true;
        }

        /// <summary>
        /// 使用键构造器删除 Archetype。
        /// </summary>
        public bool Remove(in ArchetypeBuilder builder, out Archetype archetype)
            => Remove(builder.ComponentMask, builder.RelationMask, out archetype);

        protected override void DisposeManagedResources()
        {
            _dictionary.Clear();
            foreach (var archetype in _list)
            {
                archetype.Dispose();
            }

            base.DisposeManagedResources();
        }
    }
}