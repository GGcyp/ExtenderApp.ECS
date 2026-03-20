using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 管理实体信息的创建与生命周期。
    /// </summary>
    internal class EntityManager
    {
        /// <summary>
        /// 默认的段容量。
        /// </summary>
        private const int DefaultSegmentSize = 4 * 1024;

        /// <summary>
        /// 默认的有序段集合初始容量。根据实际使用情况可以调整以平衡内存占用与性能。
        /// </summary>
        private const int DefaultSegmentListSize = 128;

        /// <summary>
        /// 使用有序字典（基于红黑树）的段索引，键为 StartIndex。
        /// 这样可以通过二分查找（或 SortedList 的键集合二分）更快地定位包含某全局 Id 的段。
        /// </summary>
        private readonly SortedList<int, EntitySegment> _segments;

        /// <summary>
        /// 缓存的空闲或可复用的段实例，用于减少重复分配开销。
        /// </summary>
        private EntitySegment? _cachedSegment;

        /// <summary>
        /// 初始化 <see cref="EntityManager" /> 的新实例。
        /// </summary>
        public EntityManager()
        {
            // 初始化首段
            var first = new EntitySegment(1, DefaultSegmentSize);
            _segments = new(DefaultSegmentListSize);
            _segments.Add(first.StartIndex, first);
        }

        /// <summary>
        /// 创建新实体的快捷方法，等同于调用 <see cref="CreateEntity(Archetype?)"/> 并传入 null。
        /// </summary>
        /// <returns>新创建的实体句柄。</returns>
        public Entity CreateEntity() => CreateEntity(null);

        /// <summary>
        /// 创建新实体信息并返回。
        /// 若当前段已满会沿有序段集合查找或在必要时创建新段。
        /// </summary>
        /// <param name="archetype">实体所属的 Archetype（原型），可为空表示暂不关联。</param>
        /// <returns>新分配的实体句柄（包含 Id 与 Version）。</returns>
        public Entity CreateEntity(Archetype? archetype)
        {
            // 遍历所有段，从头开始寻找可用槽位（因为 Id 可回收，首段可能有空位）
            foreach (var seg in _segments.Values)
            {
                if (seg.TryRentEntityInfo(out var localIndex))
                {
                    ref var info = ref seg.GetEntityInfo(localIndex);
                    InitializeEntityInfo(ref info, seg.StartIndex + localIndex, archetype);
                    return info;
                }
            }

            // 若所有段都满，则在末尾追加新段并分配
            EntitySegment last = _segments.Values[_segments.Count - 1];
            EntitySegment newSeg = _cachedSegment ?? new(0, DefaultSegmentSize);
            newSeg.StartIndex = last.StartIndex + DefaultSegmentSize;
            _segments.Add(newSeg.StartIndex, newSeg);

            if (!newSeg.TryRentEntityInfo(out var newLocalIndex))
                throw new InvalidOperationException("新段分配失败");

            ref var info2 = ref newSeg.GetEntityInfo(newLocalIndex);
            InitializeEntityInfo(ref info2, newSeg.StartIndex + newLocalIndex, archetype);
            return info2;

            void InitializeEntityInfo(ref EntityInfo info, int globalId, Archetype? archetype)
            {
                if (info.Id == 0) info.Id = globalId;
                info.Version = info.Version == 0 ? (ushort)1 : info.Version++; // 初始版本为 1，0 代表无效

                if (archetype != null)
                {
                    info.Archetype = archetype;
                    info.ArchetypeIndex = archetype.AddEntity(info);
                }
            }
        }

        /// <summary>
        /// 销毁指定实体信息。
        /// 如果实体位于某个段中该段会回收该实体并在必要时回收空段（非首段）。
        /// </summary>
        /// <param name="entity">要销毁的实体信息。</param>
        public void DestroyEntity(Entity entity)
        {
            if (entity.IsEmpty)
                return;

            if (!TryFindSegmentForEntity(entity, out var segment, out var localIndex))
                return;

            segment.ReturnEntity(localIndex, entity);

            // 如果当前段为空且不是首段，则移除并缓存
            if (segment.AliveCount > 0 && segment != _segments.Values[0])
            {
                _segments.Remove(segment.StartIndex);
                _cachedSegment = segment;
            }
        }

        /// <summary>
        /// 将指定实体从其当前 Archetype 切换到新的 Archetype。
        /// 仅当实体仍然存活且版本匹配时才执行切换。
        /// </summary>
        /// <param name="entity">要变更的实体句柄。</param>
        /// <param name="archetype">目标 Archetype。</param>
        /// <param name="archetypeIndex">当返回 true 时，输出实体在 Archetype 中的索引。</param>
        public bool TryChangedArchetype(Entity entity, Archetype? archetype, out int archetypeIndex)
        {
            archetypeIndex = -1;
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                Entity changedEntity = Entity.Empty;
                int newIndex = -1;
                if (info.IsAlive(entity))
                {
                    // 从旧 Archetype 中移除实体数据
                    var oldArchetype = info.Archetype;
                    var oldArchetypeIndex = info.ArchetypeIndex;

                    if (archetype != null)
                    {
                        // 更新为新 Archetype 并添加实体数据
                        info.ArchetypeIndex = archetypeIndex = archetype.AddEntity(entity);

                        if (oldArchetype != null)
                            oldArchetype.CopyToAndRemoveEntity(oldArchetypeIndex, archetype, archetypeIndex, out changedEntity, out newIndex);
                    }
                    else
                    {
                        if (oldArchetype != null &&
                            !oldArchetype.TryRemoveEntity(oldArchetypeIndex, out changedEntity, out newIndex))
                            return false;
                    }

                    TryChangedEntityIndexForArchetype(changedEntity, newIndex);
                    info.Archetype = archetype;

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 尝试直接更新指定实体的 Archetype 索引（不修改 Archetype 引用），仅当实体仍然存活且版本匹配时才执行更新。
        /// </summary>
        /// <param name="entity">要更新的实体句柄。</param>
        /// <param name="newArchetypeIndex">新的 Archetype 索引值。</param>
        /// <returns>如果更新成功则返回 true，否则返回 false。</returns>
        public bool TryChangedEntityIndexForArchetype(Entity entity, int newArchetypeIndex)
        {
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                if (info.IsAlive(entity))
                {
                    info.ArchetypeIndex = newArchetypeIndex;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查指定实体信息是否存活。
        /// </summary>
        /// <param name="entity">要检查的实体句柄。</param>
        /// <returns>存活返回 true；否则返回 false。</returns>
        public bool IsAlive(Entity entity)
        {
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var current, out var localIndex))
                return current.GetEntityInfo(localIndex).IsAlive(entity);

            return false;
        }

        /// <summary>
        /// 尝试获取指定实体信息所属的原型（Archetype）及在原型中的索引。
        /// </summary>
        /// <param name="entity">要查询的实体信息。</param>
        /// <param name="archetype">当返回 true 时，输出实体所属的 Archetype。</param>
        /// <param name="archetypeIndex">当返回 true 时，输出实体在 Archetype 中的索引。</param>
        /// <returns>如果找到并且实体仍然存活则返回 true；否则返回 false。</returns>
        public bool TryGetArchetype(Entity entity, out Archetype? archetype, out int archetypeIndex)
        {
            archetype = null!;
            archetypeIndex = -1;
            if (entity.IsEmpty)
                return false;

            if (TryFindSegmentForEntity(entity, out var entitySegment, out var localIndex))
            {
                ref var info = ref entitySegment.GetEntityInfo(localIndex);
                if (info.IsAlive(entity))
                {
                    archetype = info.Archetype;
                    archetypeIndex = info.ArchetypeIndex;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在有序段集合中查找包含指定实体的段，并返回对应段与段内局部索引。
        /// 使用二分查找快速定位可能包含指定全局 Id 的段。
        /// </summary>
        private bool TryFindSegmentForEntity(Entity entity, [NotNullWhen(true)] out EntitySegment entitySegment, out int localIndex)
        {
            entitySegment = null!;
            localIndex = -1;
            int globalIndex = entity.Id;

            // 二分查找：找到最大的 startIndex <= globalIndex
            var keys = _segments.Keys;
            int lo = 0, hi = keys.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int start = keys[mid];
                if (start <= globalIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best == -1)
                return false;

            var seg = _segments.Values[best];
            if (seg.TryGetLocalIndex(entity, out localIndex))
            {
                entitySegment = seg;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取结构体枚举器（foreach 优先走该路径，避免装箱）。
        /// </summary>
        /// <returns>结构体枚举器。</returns>
        public AliveEntityEnumerator GetEnumerator()
        {
            return new AliveEntityEnumerator(this);
        }

        #region Enumerable

        /// <summary>
        /// 存活实体信息的结构体可枚举序列。
        /// </summary>
        public readonly struct AliveEntityEnumerable
        {
            private readonly EntityManager _manager;

            internal AliveEntityEnumerable(EntityManager manager)
            {
                _manager = manager;
            }

            /// <summary>
            /// 获取结构体枚举器。
            /// </summary>
            /// <returns>结构体枚举器。</returns>
            public AliveEntityEnumerator GetEnumerator() => new(_manager);
        }

        /// <summary>
        /// 存活实体信息的结构体枚举器。
        /// </summary>
        public struct AliveEntityEnumerator : IEnumerator<EntityInfo>
        {
            private readonly EntityManager _manager;

            /// <summary>
            /// 当前正在遍历的实体段引用。
            /// </summary>
            private EntitySegment? _currentSegment;

            /// <summary>
            /// 当前遍历段内的局部索引（用于 TryGetNextAliveEntity 传入/返回位置）。
            /// </summary>
            private int _localIndex;

            /// <summary>
            /// 枚举器锁定标志（目前保留以便未来扩展）。
            /// </summary>
            private bool _lockTaken;

            internal AliveEntityEnumerator(EntityManager manager)
            {
                _manager = manager;
                _currentSegment = manager._segments.Values[0];
                _localIndex = -1;
                _lockTaken = false;
                Current = EntityInfo.Empty;
            }

            /// <summary>
            /// 当前实体信息。
            /// </summary>
            public EntityInfo Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一个存活实体信息。
            /// </summary>
            /// <returns>存在下一个实体信息返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                while (_currentSegment is not null)
                {
                    if (_currentSegment.TryGetNextAliveEntity(ref _localIndex, out var entityInfo))
                    {
                        Current = entityInfo;
                        return true;
                    }

                    // 移动到下一段并重置局部索引
                    var segList = _manager._segments.Values;
                    int idx = Array.IndexOf(segList.ToArray(), _currentSegment);
                    idx++;
                    _currentSegment = idx < segList.Count ? segList[idx] : null;
                    _localIndex = -1;
                }

                Current = EntityInfo.Empty;
                return false;
            }

            /// <summary>
            /// 将枚举器重置到初始位置。
            /// </summary>
            public void Reset()
            {
                _currentSegment = _manager._segments.Values[0];
                _localIndex = -1;
                Current = EntityInfo.Empty;
            }

            /// <summary>
            /// 释放枚举器占用的锁。
            /// </summary>
            public void Dispose()
            {
                _lockTaken = false;
            }
        }

        #endregion Enumerable

        #region EntitySegment

        /// <summary>
        /// 实体段，负责管理一段连续实体索引的版本与分配状态。
        /// </summary>
        [DebuggerDisplay("StartIndex={StartIndex}, AliveCount={AliveCount}")]
        private sealed class EntitySegment
        {
            private readonly EntityInfo[] _entityInfos;

            /// <summary>
            /// 空闲局部索引栈，按 LIFO 管理可重用的槽位。
            /// </summary>
            private readonly Stack<int> _freeStack;

            /// <summary>
            /// 段起始全局索引（用于计算实体的全局 Id）。
            /// </summary>
            public int StartIndex { get; set; }

            /// <summary>
            /// 获取当前段内存活实体数量。
            /// </summary>
            public int AliveCount => _freeStack.Count - DefaultSegmentSize;

            /// <summary>
            /// 使用指定起始索引初始化实体段。
            /// </summary>
            /// <param name="startIndex">段起始全局索引。</param>
            /// <param name="maxSize">段最大大小。</param>
            public EntitySegment(int startIndex, int maxSize)
            {
                CreateArray(maxSize, out _entityInfos);

                _freeStack = new(maxSize);
                StartIndex = startIndex;

                // 将所有有效的局部索引（0..maxSize-1）压入空闲栈，供后续分配使用
                for (var localIndex = maxSize - 1; localIndex >= 0; localIndex--)
                    _freeStack.Push(localIndex);
            }

            /// <summary>
            /// 创建指定长度的数组，并通过输出参数返回。
            /// </summary>
            /// <typeparam name="T">数组元素类型。</typeparam>
            /// <param name="length">数组长度。</param>
            /// <param name="array">返回创建的数组实例。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CreateArray<T>(int length, out T[] array) => array = new T[length];

            /// <summary>
            /// 尝试从当前段分配一个实体信息。
            /// </summary>
            /// <param name="archetype">要分配的实体对应的 Archetype。</param>
            /// <param name="entityInfo">分配成功时返回的实体信息。</param>
            /// <returns>分配成功返回 true，否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRentEntityInfo(out int localIndex)
            {
                return _freeStack.TryPop(out localIndex);
            }

            /// <summary>
            /// 回收指定局部索引的实体，更新版本并将槽返回到空闲栈中。
            /// 同时会从所属 Archetype 中移除对应实体数据。
            /// </summary>
            /// <param name="localIndex">要回收的段内局部索引。</param>
            /// <param name="version">要回收的实体版本，用于验证实体仍然存活。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReturnEntity(int localIndex, Entity entity)
            {
                ref var info = ref _entityInfos[localIndex];
                // 实体已被销毁或版本不匹配，忽略回收请求
                if (info.Id == 0 || info.Version != entity.Version)
                    return;

                info.Archetype = default!;
                info.Version++;
                _freeStack.Push(localIndex);
            }

            /// <summary>
            /// 以引用形式获取指定段内局部索引对应的 EntityInfo，以便直接读取或写入。
            /// </summary>
            /// <param name="localIndex">段内局部索引。</param>
            /// <returns>对应的 <see cref="EntityInfo"/> 的引用。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref EntityInfo GetEntityInfo(int localIndex) => ref _entityInfos[localIndex];

            /// <summary>
            /// 从指定局部索引之后查找下一个存活的实体信息（供枚举器使用）。
            /// </summary>
            /// <param name="localIndex">当前局部索引，成功时更新为命中索引。</param>
            /// <param name="entityInfo">找到的实体信息（输出）。</param>
            /// <returns>找到返回 true，否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetNextAliveEntity(ref int localIndex, out EntityInfo entityInfo)
            {
                for (var i = localIndex + 1; i < _entityInfos.Length; i++)
                {
                    ref var info = ref _entityInfos[i];
                    if (!info.IsEmpty)
                    {
                        localIndex = i;
                        entityInfo = info;
                        return true;
                    }
                }

                entityInfo = EntityInfo.Empty;
                return false;
            }

            /// <summary>
            /// 尝试将全局索引转换为段内局部索引。
            /// 局部索引为 0-based（对应数组下标）。
            /// </summary>
            /// <param name="entity">要获取原型的实体句柄。</param>
            /// <param name="localIndex">转换成功时返回局部索引。</param>
            /// <returns>转换成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetLocalIndex(Entity entity, out int localIndex)
            {
                int globalIndex = entity.Id;
                localIndex = globalIndex - StartIndex;
                return (uint)localIndex < (uint)DefaultSegmentSize;
            }
        }

        #endregion EntitySegment
    }
}