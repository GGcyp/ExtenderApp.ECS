using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 查询核心对象。
    ///
    /// 职责：
    /// - 持有查询描述 (<see cref="EntityQueryDesc"/>)；
    /// - 基于全局 Archetype 列表匹配出符合查询条件的 Archetype 集合；
    /// - 管理匹配结果的有效性（当世界的 Archetype 列表发生变化时会触发重建）。
    ///
    /// 说明：组件列的位置不会在此处固定缓存（以避免不同 Archetype 上列位置差异），
    /// 组件列的动态解析由访问器层（如 <see cref="ArchetypeAccessor{T}"/>）在运行时完成。
    /// 此类型只负责匹配与维护匹配集合的生命周期与版本检测。
    /// </summary>
    internal sealed class EntityQueryCore
    {
        /// <summary>
        /// 所有已注册的 Archetype 仓库引用（用于遍历并进行匹配扫描）。
        /// </summary>
        private readonly ArchetypeRepository _repository;

        /// <summary>
        /// 世界版本管理器，用于获取当前世界版本以支持跳过未变化的块的优化逻辑。
        /// </summary>
        private readonly WorldVersionManager _worldVersionManager;

        /// <summary>
        /// 查询描述，包含 Query/All/Any/None/Relation 等筛选条件。
        /// </summary>
        private readonly EntityQueryDesc _desc;

        /// <summary>
        /// 匹配到的 Archetype 链表头。
        /// </summary>
        private ArchetypeSegment? head;

        /// <summary>
        /// 匹配到的 Archetype 链表尾（便于追加）。
        /// </summary>
        private ArchetypeSegment? tail;

        /// <summary>
        /// 上次用于匹配扫描时仓库的元素计数，用于快速判断是否需要重建匹配列表。
        /// </summary>
        private int lastCount;

        /// <summary>
        /// 获取当前查询匹配的实体总数。该属性会遍历匹配的 Archetype 链表并累加每个 Archetype 中的实体数量。
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                var current = head;
                while (current != null)
                {
                    count += current.Archetype.EntityCount;
                    current = current.Next;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取当前查询的描述信息。返回值为只读引用以避免外部修改。
        /// </summary>
        internal ref readonly EntityQueryDesc QueryDesc => ref _desc;

        /// <summary>
        /// 使用必要上下文创建 <see cref="EntityQueryCore"/> 实例。
        /// </summary>
        /// <param name="allArchetypeList">全局 Archetype 仓库引用，用于匹配扫描。</param>
        /// <param name="desc">查询描述，包含筛选条件。</param>
        /// <param name="worldVersionManager">世界版本管理器，用于版本检测与跳过未变化块的优化。</param>
        internal EntityQueryCore(ArchetypeRepository allArchetypeList, EntityQueryDesc desc, WorldVersionManager worldVersionManager)
        {
            _worldVersionManager = worldVersionManager;
            _repository = allArchetypeList;
            _desc = desc;
            lastCount = 0;
        }

        /// <summary>
        /// 为指定组件类型创建按 Archetype 聚合的访问器。
        /// 返回的 <see cref="ArchetypeAccessor{T}"/> 可用于跨匹配 Archetype 遍历该组件的列数据。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="skipUnchanged">是否启用跳过未发生变化的块的优化（true 将传入当前世界版本）。</param>
        /// <returns>相应类型的 <see cref="ArchetypeAccessor{T}"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArchetypeEnumerator<T> GetArchetypeEnumerator<T>(bool skipUnchanged) => new(GetArchetypeAccessor<T>(skipUnchanged));

        /// <summary>
        /// 获取实体枚举器（按匹配的 Archetype 遍历实体句柄）。
        /// 该枚举器可与组件访问器配合使用以生成按行的查询结果。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArchetypeEntityEnumerator GetArchetypeEntityEnumerator() => new(GetArchetypeEntityAccessorEnumerator());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ArchetypeAccessorEnumerator<T> GetArchetypeAccessor<T>(bool skipUnchanged) => new(GetArchetypeSegmentHead(), GetVersion(skipUnchanged));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ArchetypeEntityAccessorEnumerator GetArchetypeEntityAccessorEnumerator() => new(GetArchetypeSegmentHead());

        /// <summary>
        /// 返回当前缓存的匹配 Archetype 链表头。在返回前会确保匹配结果为最新（必要时触发重建）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArchetypeSegment? GetArchetypeSegmentHead()
        {
            EnsureMatches();
            return head;
        }

        /// <summary>
        /// 根据是否跳过未变化块返回访问器应使用的版本参数。
        /// 若 <paramref name="skipUnchanged"/> 为 true，则返回当前世界版本号以启用跳过逻辑；否则返回 0 表示不启用跳过。
        /// </summary>
        /// <param name="skipUnchanged">是否跳过未变化的块。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetVersion(bool skipUnchanged) => skipUnchanged ? _worldVersionManager.WorldVersion : 0;

        /// <summary>
        /// 确保当前缓存的匹配集合仍然有效。若仓库计数发生变化则触发重建匹配链表。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMatches()
        {
            if (lastCount == _repository.Count)
                return;

            RebuildMatches();
            lastCount = _repository.Count;
        }

        /// <summary>
        /// 根据查询描述遍历仓库中自上次扫描以来新增的 Archetype，并筛选出满足条件的 Archetype，
        /// 将匹配结果以链表形式追加到内部缓存中。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildMatches()
        {
            foreach (var archetype in _repository.GetArchetypeSpan(lastCount))
            {
                if (MatchArchetype(archetype))
                {
                    AppendSegment(archetype);
                }
            }
        }

        /// <summary>
        /// 将满足查询条件的 Archetype 包装成 <see cref="ArchetypeSegment"/> 并追加到匹配链表的末尾。
        /// </summary>
        /// <param name="archetype">指定要添加的原型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendSegment(Archetype archetype)
        {
            ArchetypeSegment segment = new(archetype);
            if (head == null)
            {
                head = tail = segment;
            }
            else
            {
                tail!.Next = segment;
                tail = segment;
            }
        }

        /// <summary>
        /// 判断给定 Archetype 是否满足当前查询描述的所有筛选条件（Query/All/Any/None/Relation）。
        /// </summary>
        /// <param name="archetype">要判断的 Archetype。</param>
        /// <returns>满足返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchArchetype(Archetype archetype)
        {
            if (!archetype.ComponentMask.All(_desc.Query))
                return false;

            if (_desc.HasAll && !archetype.ComponentMask.All(_desc.All))
                return false;

            if (_desc.HasAny && !archetype.ComponentMask.Any(_desc.Any))
                return false;

            if (_desc.HasNone && !archetype.ComponentMask.None(_desc.None))
                return false;

            if (_desc.HasRelation && !archetype.RelationTypes.On(_desc.Relation))
                return false;

            return true;
        }
    }
}