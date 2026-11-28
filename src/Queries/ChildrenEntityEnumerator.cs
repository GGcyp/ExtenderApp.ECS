using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 子实体枚举器：遍历 Archetype 仓库中所有含关系类型的 Archetype，产出「任一关系类型的目标实体等于 target」的存活实体。
    /// 零堆分配：内部仅以 (表索引, 段索引, 行索引) 三个游标推进，每次 MoveNext 在栈上获取仓库切片，不创建任何集合。
    /// 注意：遍历期间不可执行结构变更（实体增删/迁移），否则迭代行为未定义；对已销毁目标的悬挂指向值同样会被产出。
    /// </summary>
    public struct ChildrenEntityEnumerator
    {
        private readonly ArchetypeRepository _repository;
        private readonly Entity _target;
        private int _archetypeIndex;
        private int _segmentIndex;
        private int _localIndex;
        private Entity _current;
        private Archetype? _archetype;

        internal ChildrenEntityEnumerator(ArchetypeRepository repository, Entity target)
        {
            _repository = repository;
            _target = target;
            _archetypeIndex = 0;
            _segmentIndex = 0;
            _localIndex = 0;
            _current = default;
            _archetype = null;
        }

        /// <summary>
        /// 当前枚举到的实体。
        /// </summary>
        public Entity Current => _current;

        /// <summary>
        /// 获取自身枚举器以支持 foreach。
        /// </summary>
        public ChildrenEntityEnumerator GetEnumerator() => this;

        /// <summary>
        /// 移动到下一个关系目标匹配的实体。
        /// </summary>
        /// <returns> 存在下一项返回 true；否则返回 false。 </returns>
        public bool MoveNext()
        {
            var span = _repository.GetArchetypeSpan(0);
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
                            foreach (var relationType in _archetype.RelationTypes)
                            {
                                if (_archetype.TryGetRelationTarget(relationType, globalIndex, out var relationTarget) &&
                                    relationTarget == _target)
                                {
                                    _current = segment.Entities[local];
                                    return true;
                                }
                            }
                        }

                        _segmentIndex++;
                        _localIndex = 0;
                    }

                    _archetype = null;
                }

                if (_archetypeIndex >= span.Length)
                    return false;

                _archetype = span[_archetypeIndex];
                _archetypeIndex++;
                _segmentIndex = 0;
                _localIndex = 0;
            }
        }
    }
}
