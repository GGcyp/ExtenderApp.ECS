using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 关系目标实体枚举器。 基于查询已匹配的 Archetype 链表，按实体段遍历有效行并过滤出目标匹配的实体。
    /// 注意：不能按「行 0..EntityCount-1」扫描——实体移除采用段内尾交换，删除会使前部段出现空洞，行号空间在段粒度并不密集。
    /// 遍历期间不可执行结构变更。
    /// </summary>
    public struct RelationTargetEntityEnumerator
    {
        private ArchetypeSegment? _segment;
        private Archetype? _archetype;
        private int _segmentIndex;
        private int _localIndex;
        private Entity _current;
        private readonly RelationType _relationType;
        private readonly Entity _target;

        internal RelationTargetEntityEnumerator(ArchetypeSegment? head, RelationType relationType, Entity target)
        {
            _segment = head;
            _archetype = null;
            _segmentIndex = 0;
            _localIndex = 0;
            _current = default;
            _relationType = relationType;
            _target = target;
        }

        /// <summary>
        /// 当前枚举到的实体。
        /// </summary>
        public Entity Current => _current;

        /// <summary>
        /// 获取自身枚举器以支持 foreach。
        /// </summary>
        public RelationTargetEntityEnumerator GetEnumerator() => this;

        /// <summary>
        /// 移动到下一个关系目标匹配的实体。
        /// </summary>
        /// <returns> 存在下一项返回 true；否则返回 false。 </returns>
        public bool MoveNext()
        {
            while (true)
            {
                if (_archetype != null)
                {
                    var segmentList = _archetype.Entities;
                    while (_segmentIndex < segmentList.Count)
                    {
                        var segment = segmentList.Span[_segmentIndex];
                        while (_localIndex < segment.Count)
                        {
                            int local = _localIndex++;
                            int globalIndex = segment.StartIndex + local;
                            if (_archetype.TryGetRelationTarget(_relationType, globalIndex, out var target) &&
                                target == _target)
                            {
                                _current = segment.Entities[local];
                                return true;
                            }
                        }

                        _segmentIndex++;
                        _localIndex = 0;
                    }

                    _archetype = null;
                }

                if (_segment == null)
                    return false;

                _archetype = _segment.Archetype;
                _segment = _segment.Next;
                _segmentIndex = 0;
                _localIndex = 0;
            }
        }
    }
}
