using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 管理 Archetype 的各组件列的块集合，并为按列查找提供二分查找加速方法。
    ///
    /// 实现细节：
    /// - 每列维护一个按 <see cref="ArchetypeChunk.StartIndex"/> 升序的 <see cref="List{ArchetypeChunk}"/>。
    /// - 查找时使用二分查找定位最近的起始块索引（最大 start <= globalIndex），随后做少量邻近块检查以处理边界情况。
    /// - 新块通常追加到列表末尾（通过 <see cref="ArchetypeChunk.RentAndSetNext"/> 获得），从而使追加开销最小化。
    ///
    /// 设计目标：在块数量较多时仍保证对数级别的查找性能，同时保持实现简单且无额外缓存一致性负担。
    /// </summary>
    internal sealed class ArchetypeChunkManager
    {
        /// <summary>
        /// 原型块提供者数组，每个元素对应 Archetype 中的一个组件列。
        /// </summary>
        private readonly ArchetypeChunkProvider[] _archetypeChunkProviders;

        /// <summary>
        /// 每列的块集合数组，数组索引对应 Archetype 中的组件编码位置。
        /// 每个元素为一个按 StartIndex 升序排序的 ArchetypeChunk 列表。
        /// </summary>
        private readonly ArchetypeChunkList[] _columns;

        /// <summary>
        /// 获取当前管理器管理的块头数量（等同于 Archetype 中组件列数量）。
        /// </summary>
        public int ChunkHeadCount => _columns.Length;

        /// <summary>
        /// 获取当前管理器管理的实体总数（即所有列中所有块的实体数量之和）。
        /// </summary>
        public int EntityCount { get; private set; }

        /// <summary>
        /// 使用给定的块头数组构建管理器实例。
        /// 会把每个块链扁平化为按起始索引升序的列表并保存以备后续查找。
        /// </summary>
        /// <param name="heads">按组件编码位置排列的块头数组（每列可能为链表）。</param>
        public ArchetypeChunkManager(ArchetypeChunkProvider[] providers)
        {
            _archetypeChunkProviders = providers;
            _columns = new ArchetypeChunkList[providers.Length];
            for (int i = 0; i < providers.Length; i++)
            {
                _columns[i] = providers[i].CreateChunkList();
            }
        }

        /// <summary>
        /// 获取指定列的第一个块（若存在）。
        /// 该方法仅用于兼容调用方对头块的直接访问（例如 Dispose 时遍历）。
        /// </summary>
        /// <param name="columnIndex">要获取的列索引（0-based）。</param>
        /// <returns>若列包含至少一个块则返回第一个块，否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeChunk? GetHead(int columnIndex) => _columns[columnIndex].Count > 0 ? _columns[columnIndex][0] : null;

        /// <summary>
        /// 尝试在指定列中为一个实体追加槽位（若末尾块已满会向该列追加新块）。
        /// </summary>
        /// <param name="entity">要添加的实体。</param>
        /// <param name="columnIndex">目标列索引（0-based）。</param>
        /// <param name="worldVersion">当前世界版本，用于更新块的版本以支持版本化访问控制。</param>
        /// <param name="globalIndex">输出被分配到的全局实体索引（若返回 true）。</param>
        /// <returns>若成功为本列分配到槽位则返回 true；否则返回 false（例如该列无初始块）。</returns>
        public bool TryAddToColumn(Entity entity, int columnIndex, ulong worldVersion, out int globalIndex)
        {
            globalIndex = -1;
            var list = _columns[columnIndex];
            ArchetypeChunk? chunk;
            // 该列尚无块，尝试从提供者租用一个新块并追加
            if (list.Count == 0)
            {
                var provider = _archetypeChunkProviders[columnIndex];
                chunk = provider.Rent();
                chunk.Initialize();
                list.Add(chunk);
            }
            else
                chunk = list[0];

            while (chunk != null)
            {
                if (chunk.TryAdd(entity, out globalIndex))
                {
                    EntityCount++;
                    chunk.Version = worldVersion;
                    return true;
                }

                if (chunk.Next == null)
                {
                    chunk = chunk.RentAndSetNext();
                    list.Add(chunk);
                }
                else
                    chunk = chunk.Next;
            }
            return false;
        }

        /// <summary>
        /// 在指定列中移除对应的 globalIndex（若存在）。
        /// 只在找到包含该索引的块时执行 Remove。
        /// </summary>
        /// <param name="columnIndex">列索引（0-based）。</param>
        /// <param name="globalIndex">要移除的实体全局索引。</param>
        /// <param name="worldVersion">当前世界版本，用于更新块的版本以支持版本化访问控制。</param>
        /// <param name="changedEntity">输出更改过的实体</param>
        /// <param name="newIndex">输出被移除实体在块内的局部索引（若返回 true）。</param>
        public bool TryRemoveFromColumn(int columnIndex, int globalIndex, ulong worldVersion, out Entity changedEntity, out int newIndex)
        {
            changedEntity = Entity.Empty;
            if (TryFindChunkForGlobalIndex(columnIndex, globalIndex, out var chunk, out newIndex, out var chunkIndex))
            {
                chunk!.Remove(newIndex, out changedEntity);

                EntityCount--;
                chunk.Version = worldVersion;

                if (chunk.Count == 0)
                {
                    var list = _columns[columnIndex];
                    for (int i = list.Count - 1; i > chunkIndex; i--)
                    {
                        chunk = list[i];
                        if (chunk.Count > 0)
                            break;

                        list.RemoveAt(i);
                        chunk.Return();
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 在指定列中通过二分查找定位包含 globalIndex 的块并返回该块与局部索引。
        /// 查找逻辑：先二分找到最大 startIndex <= globalIndex 的索引（best），再从 best 向后/向前做少量检查以处理边界/间隙情形。
        /// </summary>
        /// <param name="columnIndex">列索引（0-based）。</param>
        /// <param name="globalIndex">要定位的全局实体索引。</param>
        /// <param name="foundChunk">输出找到的块引用（若返回 true）。</param>
        /// <param name="localIndex">输出在块内的局部索引（若返回 true）。</param>
        /// <returns>若在该列找到包含 globalIndex 的块则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindChunkForGlobalIndex(int columnIndex, int globalIndex, out ArchetypeChunk? foundChunk, out int localIndex)
        {
            return TryFindChunkForGlobalIndex(columnIndex, globalIndex, out foundChunk, out localIndex, out _);
        }

        /// <summary>
        /// 在指定列中通过二分查找定位包含 globalIndex 的块并返回该块与局部索引及块索引。
        /// 查找逻辑：先二分找到最大 startIndex <= globalIndex 的索引（best），再从 best 向后/向前做少量检查以处理边界/间隙情形。
        /// </summary>
        /// <param name="columnIndex">列索引（0-based）。</param>
        /// <param name="globalIndex">要定位的全局实体索引。</param>
        /// <param name="foundChunk">输出找到的块引用（若返回 true）。</param>
        /// <param name="localIndex">输出在块内的局部索引（若返回 true）。</param>
        /// <param name="chunkIndex">输出找到的块在列中的索引（若返回 true）。</param>
        /// <returns>若在该列找到包含 globalIndex 的块则返回 true，否则返回 false。</returns>
        public bool TryFindChunkForGlobalIndex(int columnIndex, int globalIndex, out ArchetypeChunk? foundChunk, out int localIndex, out int chunkIndex)
        {
            var list = _columns[columnIndex];
            foundChunk = null;
            localIndex = -1;
            chunkIndex = -1;
            if (list.Count == 0)
                return false;

            int lo = 0, hi = list.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int start = list[mid].StartIndex;
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

            // 从 best 开始向前/向后做少量检查以处理非满块或间隙情况
            // 首先检查 best 本身
            for (int i = best; i < list.Count && list[i].StartIndex <= globalIndex; i++)
            {
                var candidate = list[i];
                if (candidate.TryWithinChunk(globalIndex, out localIndex))
                {
                    chunkIndex = i;
                    foundChunk = candidate;
                    return true;
                }
                // 如果 candidate.StartIndex > globalIndex 则可中断（但 for 条件已确保）
            }

            // 若未命中，尝试向前检查下标小于 best 的块（可能存在覆盖的情形）
            for (int i = best - 1; i >= 0; i--)
            {
                var candidate = list[i];
                if (candidate.TryWithinChunk(globalIndex, out localIndex))
                {
                    chunkIndex = i;
                    foundChunk = candidate;
                    return true;
                }
                if (candidate.StartIndex < globalIndex - candidate.Count)
                    break; // 早期中断（经验性）
            }

            return false;
        }

        /// <summary>
        /// 尝试获取指定列的块列表（如果该列至少有一个块则返回 true）。
        /// </summary>
        /// <param name="columnIndex">列表序号。</param>
        /// <param name="chunkList">块列表。</param>
        /// <returns>若在该列找到块则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetChunkListForColumn(int columnIndex, out ArchetypeChunkList chunkList)
        {
            chunkList = _columns[columnIndex];
            return chunkList.Count > 0;
        }
    }
}