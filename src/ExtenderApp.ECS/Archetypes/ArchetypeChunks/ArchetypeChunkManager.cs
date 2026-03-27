using System.Buffers;
using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 管理 Archetype 的组件列块与实体段索引。 负责实体槽位的分配/回收、按全局索引定位块位置，以及组件句柄的维护。
    /// </summary>
    internal sealed class ArchetypeChunkManager
    {
        /// <summary>
        /// 列块列表的默认初始容量。
        /// </summary>
        private const int DefaultListSize = 16;

        /// <summary>
        /// 超出预设增长表后的固定段容量。
        /// </summary>
        private const int FixedSegmentSize = 2048;

        /// <summary>
        /// 预设段容量增长表。 在索引范围内按该数组递增，超出后使用 <see cref="FixedSegmentSize" />。
        /// </summary>
        private static readonly int[] SizeArray = { 16, 32, 64, 128, 256, 512, 1024 };

        /// <summary>
        /// 每个组件列对应的块提供器数组。
        /// </summary>
        private readonly ArchetypeChunkProvider[] _archetypeChunkProviders;

        /// <summary>
        /// 组件列块列表数组，索引与组件编码位置一致。
        /// </summary>
        private readonly ArchetypeChunkList?[] _columns;

        /// <summary>
        /// 组件句柄池，用于管理组件句柄的租用与归还，支持实体移除时的尾部交换操作。 该池为共享实例，适用于所有 ArchetypeChunkManager。
        /// </summary>
        private readonly ComponentHandlePool handlePool = ComponentHandlePool.Share;

        /// <summary>
        /// 实体段信息列表，用于将全局索引映射到段内局部索引。
        /// </summary>
        internal readonly ArchetypeEntitySegmentInfoList Entities;

        /// <summary>
        /// 初始化 <see cref="ArchetypeChunkManager" /> 的新实例。
        /// </summary>
        /// <param name="providers">按组件编码位置排列的块提供器数组。</param>
        public ArchetypeChunkManager(ArchetypeChunkProvider[] providers)
        {
            _archetypeChunkProviders = providers;
            _columns = new ArchetypeChunkList[providers.Length];
            for (int i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                if (provider.IsEmptyComponent)
                    continue;

                _columns[i] = provider.CreateChunkList(DefaultListSize);
            }

            Entities = new(DefaultListSize);
        }

        /// <summary>
        /// 组件列数量。
        /// </summary>
        public int ChunkHeadCount => _columns.Length;

        /// <summary>
        /// 获取指定列的头块。
        /// </summary>
        /// <param name="columnIndex">列索引（0-based）。</param>
        /// <returns>存在头块则返回对应块，否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeChunk? GetHead(int columnIndex) => _columns[columnIndex]?.Count > 0 ? _columns[columnIndex]?[0] : null;

        /// <summary>
        /// 按段序号获取下一段容量。
        /// </summary>
        /// <param name="index">段序号。</param>
        /// <returns>预设容量或固定容量。</returns>
        internal static int GetNextSize(int index) => index < SizeArray.Length ? SizeArray[index] : FixedSegmentSize;

        /// <summary>
        /// 按段序号获取上一段容量。
        /// </summary>
        /// <param name="index">段序号。</param>
        /// <returns>上一段的预设容量或固定容量。</returns>
        internal static int GetPreviousSize(int index) => index < SizeArray.Length ? SizeArray[index] : FixedSegmentSize;

        #region Add

        /// <summary>
        /// 向实体段与所有组件列追加一个实体槽位。
        /// </summary>
        /// <param name="entity">要追加的实体。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <param name="globalIndex">输出分配到的实体全局索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntity(Entity entity, ulong worldVersion, out int globalIndex)
        {
            Entities.AddToSegment(entity, null, out globalIndex);

            int capacity = GetNextSize(Entities.Count - 1);

            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                AddToColumn(columnIndex, worldVersion, capacity);
        }

        /// <summary>
        /// 向实体段与所有组件列追加一个实体槽位，并绑定组件句柄。
        /// </summary>
        /// <param name="entity">要追加的实体。</param>
        /// <param name="handle">可选组件句柄。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <param name="globalIndex">输出分配到的实体全局索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntity(Entity entity, ComponentHandle? handle, ulong worldVersion, out int globalIndex)
        {
            if (handle != null)
                handle.Manager = this;
            Entities.AddToSegment(entity, handle, out globalIndex);

            int capacity = GetNextSize(Entities.Count - 1);
            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                AddToColumn(columnIndex, worldVersion, capacity);
        }

        /// <summary>
        /// 批量向实体段与所有组件列追加实体槽位。
        /// </summary>
        /// <param name="entities">待追加实体集合。</param>
        /// <param name="globalIndexSpan">输出分配到的实体全局索引集合。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntityRange(Span<Entity> entities, Span<int> globalIndexSpan, ulong worldVersion)
        {
            int count = entities.Length;
            if (count == 0)
                return;

            Entities.AddToSegmentRange(entities, globalIndexSpan, count, out _);

            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                AddToColumns(columnIndex, worldVersion, count);
        }

        /// <summary>
        /// 批量向实体段与所有组件列追加实体槽位，并绑定组件句柄。
        /// </summary>
        /// <param name="entities">待追加实体集合。</param>
        /// <param name="globalIndexSpan">输出分配到的实体全局索引集合。</param>
        /// <param name="handles">待绑定组件句柄集合。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntityRange(Span<Entity> entities, Span<int> globalIndexSpan, Span<ComponentHandle?> handles, ulong worldVersion)
        {
            int count = entities.Length;
            if (count == 0)
                return;

            foreach (var handle in handles)
                if (handle != null)
                    handle.Manager = this;

            Entities.AddToSegmentRange(entities, globalIndexSpan, handles, count, out _);

            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                AddToColumns(columnIndex, worldVersion, count);
        }

        /// <summary>
        /// 在指定列追加一个槽位；必要时创建新块。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <param name="capacity">新建块时使用的容量。</param>
        private void AddToColumn(int columnIndex, ulong worldVersion, int capacity)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            ArchetypeChunk? chunk;
            // 该列尚无块，尝试从提供者租用一个新块并追加
            if (chunkList.Count == 0)
            {
                var provider = _archetypeChunkProviders[columnIndex];
                chunk = provider.Rent();
                chunk.Initialize(capacity);
                chunkList.Add(chunk);
            }
            else
                chunk = chunkList[0];

            while (chunk != null)
            {
                if (chunk.TryAdd())
                {
                    chunk.Version = worldVersion;
                    return;
                }

                if (chunk.Next == null)
                {
                    chunk = chunk.RentAndSetNext(capacity);
                    chunkList.Add(chunk);
                }
                else
                    chunk = chunk.Next;
            }
        }

        /// <summary>
        /// 在指定列批量追加槽位。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <param name="count">要追加的数量。</param>
        /// <remarks>新块容量按增长表递增，超过预设后固定为 2048。</remarks>
        private void AddToColumns(int columnIndex, ulong worldVersion, int count)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            ArchetypeChunk? chunk;
            int capacity = GetNextSize(chunkList.Count - 1);
            if (chunkList.Count == 0)
            {
                var provider = _archetypeChunkProviders[columnIndex];
                chunk = provider.Rent();
                chunk.Initialize(capacity);
                chunkList.Add(chunk);
            }
            else
                chunk = chunkList[0];

            while (chunk != null && count > 0)
            {
                if (!chunk.TryAdds(count, out int addCount) || count != addCount)
                {
                    count = count - addCount;
                    chunk.Version = worldVersion;
                    if (chunk.Next == null)
                    {
                        capacity = GetNextSize(chunkList.Count);
                        chunk = chunk.RentAndSetNext(capacity);
                        chunkList.Add(chunk);
                    }
                    else
                        chunk = chunk.Next;
                }
            }
        }

        #endregion Add

        #region Remove

        /// <summary>
        /// 按全局索引移除实体，并同步移除各组件列对应槽位。
        /// </summary>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <param name="removedHandle">输出被移除实体对应的组件句柄。</param>
        /// <param name="changedEntity">若触发尾部交换，输出被移动到当前位置的实体；否则为 <see cref="Entity.Empty" />。</param>
        /// <returns>移除成功返回 true；否则返回 false。</returns>
        public bool TryRemove(int globalIndex, ulong worldVersion, out ComponentHandle? removedHandle, out Entity changedEntity)
        {
            if (!Entities.TryRemoveFromSegment(globalIndex, out int localIndex, out var chunkIndex, out removedHandle, out changedEntity, out _))
                return false;

            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                RemoveFromColumn(columnIndex, chunkIndex, localIndex, worldVersion);

            return true;
        }

        /// <summary>
        /// 批量移除实体，并同步移除所有组件列中的对应槽位。
        /// </summary>
        /// <param name="globalIndexs">要移除的全局索引集合。</param>
        /// <param name="removedHandles">输出被移除实体对应的组件句柄集合。</param>
        /// <param name="changedEntities">输出被移动到目标位置的实体集合。</param>
        /// <param name="changedHandles">输出被移动句柄集合。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        public bool TryRemoveRange(Span<int> globalIndexs, Span<ComponentHandle?> removedHandles, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles, ulong worldVersion)
        {
            int count = globalIndexs.Length;

            var chunkIndexBytes = ArrayPool<int>.Shared.Rent(count);
            var localIndexBytes = ArrayPool<int>.Shared.Rent(count);

            var chunkIndexSpan = chunkIndexBytes.AsSpan(0, count);
            var localIndexSpan = localIndexBytes.AsSpan(0, count);

            try
            {
                if (!Entities.TryRemoveFromSegmentRange(globalIndexs, localIndexSpan, chunkIndexSpan, removedHandles, changedEntities, changedHandles))
                    return false;
                for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                    RemoveFromColumns(columnIndex, chunkIndexSpan, localIndexSpan, worldVersion);
                return true;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(chunkIndexBytes);
                ArrayPool<int>.Shared.Return(localIndexBytes);
            }
        }

        /// <summary>
        /// 批量移除实体，并同步移除所有组件列中的对应槽位。
        /// </summary>
        /// <param name="globalIndexs">要移除的全局索引集合。</param>
        /// <param name="changedEntities">输出被移动到目标位置的实体集合。</param>
        /// <param name="changedHandles">输出被移动句柄集合。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        public bool TryRemoveRange(Span<int> globalIndexs, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles, ulong worldVersion)
        {
            int count = globalIndexs.Length;

            var chunkIndexBytes = ArrayPool<int>.Shared.Rent(count);
            var localIndexBytes = ArrayPool<int>.Shared.Rent(count);

            var chunkIndexSpan = chunkIndexBytes.AsSpan(0, count);
            var localIndexSpan = localIndexBytes.AsSpan(0, count);

            try
            {
                if (!Entities.TryRemoveFromSegmentRange(globalIndexs, localIndexSpan, chunkIndexSpan, changedEntities, changedHandles))
                    return false;
                for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                    RemoveFromColumns(columnIndex, chunkIndexSpan, localIndexSpan, worldVersion);
                return true;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(chunkIndexBytes);
                ArrayPool<int>.Shared.Return(localIndexBytes);
            }
        }

        /// <summary>
        /// 清理指定列末尾的空块并归还到对象池。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        private void RemoveEmptyChunks(int columnIndex)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            int count = chunkList.Count;
            int lastIndex = count - 1;
            for (int i = lastIndex; i >= 0; i--)
            {
                var chunk = chunkList[i];
                if (chunk.Count > 0)
                    break;

                chunkList.RemoveAt(i);
                chunk.Return();
                lastIndex--;
            }

            if (chunkList.Count == 0) return;
            chunkList[lastIndex].Next = null;
        }

        /// <summary>
        /// 从指定列的指定块中移除局部索引位置的实体槽。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkIndex">块索引。</param>
        /// <param name="localIndex">块内局部索引。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        private void RemoveFromColumn(int columnIndex, int chunkIndex, int localIndex, ulong worldVersion)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            var chunk = chunkList[chunkIndex];
            chunk.RemoveAt(localIndex);
            chunk.Version = worldVersion;

            RemoveEmptyChunks(columnIndex);
        }

        /// <summary>
        /// 从指定列批量移除实体槽位。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkIndexSpan">块索引集合。</param>
        /// <param name="localIndexSpan">块内局部索引集合。</param>
        /// <param name="worldVersion">当前世界版本。</param>
        private void RemoveFromColumns(int columnIndex, Span<int> chunkIndexSpan, Span<int> localIndexSpan, ulong worldVersion)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            int count = chunkIndexSpan.Length;

            for (int i = 0; i < count; i++)
            {
                var chunkIndex = chunkIndexSpan[i];
                var localIndex = localIndexSpan[i];
                var chunk = chunkList[chunkIndex];
                chunk.RemoveAt(localIndex);
                chunk.Version = worldVersion;
            }

            RemoveEmptyChunks(columnIndex);
        }

        #endregion Remove

        #region Find

        /// <summary>
        /// 按列索引与全局索引查找对应块及局部索引。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="foundChunk">输出找到的块。</param>
        /// <param name="localIndex">输出块内局部索引。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindChunkForGlobalIndex(int columnIndex, int globalIndex, out ArchetypeChunk foundChunk, out int localIndex)
        {
            return TryFindChunkForGlobalIndex(columnIndex, globalIndex, out foundChunk, out localIndex, out _);
        }

        /// <summary>
        /// 按列索引与全局索引查找对应块、局部索引与块索引。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="foundChunk">输出找到的块。</param>
        /// <param name="localIndex">输出块内局部索引。</param>
        /// <param name="chunkIndex">输出块索引。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        public bool TryFindChunkForGlobalIndex(int columnIndex, int globalIndex, out ArchetypeChunk foundChunk, out int localIndex, out int chunkIndex)
        {
            foundChunk = null!;
            if (Entities.TryFindLocalIndexForGlobalIndex(globalIndex, out localIndex, out chunkIndex) &&
                TryGetChunkListForColumn(columnIndex, out var chunkList))
            {
                foundChunk = chunkList[chunkIndex];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试获取指定列的块列表。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkList">输出块列表。</param>
        /// <returns>该列存在块时返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetChunkListForColumn(int columnIndex, out ArchetypeChunkList chunkList)
        {
            chunkList = _columns[columnIndex]!;
            return chunkList != null;
        }

        #endregion Find

        #region ComponentHandle

        /// <summary>
        /// 尝试根据全局索引获取对应的组件句柄。 若当前位置没有句柄则会从池中租用并写回。
        /// </summary>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="handle">输出组件句柄。</param>
        /// <returns>查找成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponentHandle(int globalIndex, out ComponentHandle handle)
        {
            handle = default!;
            if (Entities.TryFindLocalIndexForGlobalIndex(globalIndex, out int localIndex, out int chunkIndex))
            {
                var segment = Entities.Span[chunkIndex];
                handle = segment.ComponentHandles[localIndex]!;
                if (handle == null)
                {
                    handle = handlePool.Rent();
                    segment.ComponentHandles[localIndex] = handle;
                    handle.Manager = this;
                    handle.GlobalIndex = globalIndex;
                }
                return true;
            }
            return false;
        }

        #endregion ComponentHandle

        #region Copy

        /// <summary>
        /// 尝试将指定全局索引的实体槽位从当前管理器复制到目标管理器。 复制成功后会移除当前槽位并同步句柄的管理器与全局索引信息。
        /// </summary>
        /// <param name="globalIndex">源全局索引。</param>
        /// <param name="newManager">目标管理器。</param>
        /// <param name="newGlobalIndex">目标全局索引。</param>
        /// <param name="oldIndexSpan">源组件列索引映射。</param>
        /// <param name="newIndexSpan">目标组件列索引映射。</param>
        /// <param name="componentTypes">迁移后组件掩码。</param>
        /// <returns>复制并移除成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyToAndRemove(int globalIndex, ArchetypeChunkManager newManager, int newGlobalIndex, scoped Span<int> oldIndexSpan, scoped Span<int> newIndexSpan, ComponentMask componentTypes)
        {
            if (!Entities.TryFindLocalIndexForGlobalIndex(globalIndex, out int localIndex, out int chunkIndex) ||
                !newManager.Entities.TryFindLocalIndexForGlobalIndex(newGlobalIndex, out int newLocalIndex, out int newChunkIndex))
                return false;

            for (int i = 0; i < oldIndexSpan.Length; i++)
            {
                int oldColumnIndex = oldIndexSpan[i];
                int newColumnIndex = newIndexSpan[i];

                if (!TryGetChunkListForColumn(oldColumnIndex, out var chunkList) ||
                   !newManager.TryGetChunkListForColumn(newColumnIndex, out var newChunkList))
                {
                    continue;
                }

                var oldChunk = chunkList[chunkIndex];
                var newChunk = newChunkList[newChunkIndex];

                if (!oldChunk.TryCopyTo(globalIndex, newChunk, newGlobalIndex))
                    return false;

                oldChunk.RemoveAt(localIndex);
            }

            ref var info = ref Entities.Span[chunkIndex];
            var entity = info.Entities[localIndex];
            var handle = info.ComponentHandles[localIndex];
            info.Remove(localIndex, out _, out _, out _);

            ref var newInfo = ref newManager.Entities.Span[newChunkIndex];
            newInfo.Entities[newLocalIndex] = entity;
            newInfo.ComponentHandles[newLocalIndex] = handle;

            if (handle != null)
            {
                handle.Manager = newManager;
                handle.ComponentTypes = componentTypes;
                handle.GlobalIndex = newGlobalIndex;
            }
            return true;
        }

        #endregion Copy
    }
}