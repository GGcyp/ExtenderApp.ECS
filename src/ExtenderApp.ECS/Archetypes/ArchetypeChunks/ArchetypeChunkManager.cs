using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 管理某一 <see cref="Archetype" /> 下各组件列对应的块存储、实体在列中的分配与回收，以及全局行号到块内局部索引的映射。
    /// </summary>
    internal sealed class ArchetypeChunkManager
    {
        /// <summary>
        /// 新建列块列表时的初始容量；具体数值由 <see cref="Settings.DefaultArchetypeChunkListSize" /> 决定（默认 2048）。
        /// </summary>
        private const int DefaultListSize = Settings.DefaultArchetypeChunkListSize;

        /// <summary>
        /// 按当前已有块数量计算下一次扩容或新块应使用的容量。
        /// </summary>
        /// <param name="currentCount">当前块数量。</param>
        /// <returns>下一档容量。</returns>
        private static int GetNextSize(int currentCount) => Settings.GetNextArchetypeChunkSize(currentCount);

        /// <summary>
        /// 每个组件列对应的块提供器数组。
        /// </summary>
        private readonly ArchetypeChunkProvider[] _archetypeChunkProviders;

        /// <summary>
        /// 各列的块列表；与组件列索引一一对应，空组件列可为 null。
        /// </summary>
        private readonly ArchetypeChunkList?[] _columns;

        /// <summary>
        /// 共享的组件句柄池，用于租用/归还句柄；在实体移除发生尾部交换时仍会用到。句柄的 <see cref="ComponentHandle.Manager" /> 指向本 <see cref="ArchetypeChunkManager" />。
        /// </summary>
        private readonly ComponentHandlePool handlePool = ComponentHandlePool.Share;

        /// <summary>
        /// 实体段信息列表，用于将实体全局行号映射到段内局部索引与块下标。
        /// </summary>
        internal readonly ArchetypeEntitySegmentInfoList Entities;

        /// <summary>
        /// 初始化 <see cref="ArchetypeChunkManager" /> 的新实例。
        /// </summary>
        /// <param name="providers">与各组件列对齐的块提供器数组。</param>
        public ArchetypeChunkManager(ArchetypeChunkProvider[] providers)
        {
            _archetypeChunkProviders = providers;
            _columns = new ArchetypeChunkList?[providers.Length];
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
        /// 获取组件列数（与块列表头数量一致）。
        /// </summary>
        public int ChunkHeadCount => _columns.Length;

        /// <summary>
        /// 获取指定列的首个块（若该列无块则返回 null）。
        /// </summary>
        /// <param name="columnIndex">列索引（从 0 开始）。</param>
        /// <returns>存在头块时返回该块，否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeChunk? GetHead(int columnIndex) => _columns[columnIndex]?.Count > 0 ? _columns[columnIndex]?[0] : null;

        #region Add

        /// <summary>
        /// 在实体表中追加一行，并在每一组件列中分配对应槽位（无句柄）。
        /// </summary>
        /// <param name="entity">要登记的实体。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
        /// <param name="globalIndex">输出分配到的实体全局行号。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntity(Entity entity, ulong worldVersion, out int globalIndex)
        {
            Entities.AddToSegment(entity, null, out globalIndex);

            int capacity = GetNextSize(Entities.Count - 1);

            for (int columnIndex = 0; columnIndex < _columns.Length; columnIndex++)
                AddToColumn(columnIndex, worldVersion, capacity);
        }

        /// <summary>
        /// 在实体表中追加一行，并在每一组件列中分配槽位，同时可携带组件句柄。
        /// </summary>
        /// <param name="entity">要登记的实体。</param>
        /// <param name="handle">可选的组件句柄。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
        /// <param name="globalIndex">输出分配到的实体全局行号。</param>
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
        /// 批量在实体表中追加多行，并在各列中分配槽位（无句柄）。
        /// </summary>
        /// <param name="entities">待追加的实体集合。</param>
        /// <param name="globalIndexSpan">输出各行分配到的全局行号。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 批量在实体表中追加多行，并在各列中分配槽位，同时写入句柄数组。
        /// </summary>
        /// <param name="entities">待追加的实体集合。</param>
        /// <param name="globalIndexSpan">输出各行分配到的全局行号。</param>
        /// <param name="handles">与各实体对齐的句柄数组。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 在指定列追加一个空槽位；若当前列无块则从提供器租新块并挂入列表。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
        /// <param name="capacity">新建块时使用的容量。</param>
        private void AddToColumn(int columnIndex, ulong worldVersion, int capacity)
        {
            if (!TryGetChunkListForColumn(columnIndex, out var chunkList))
                return;

            ArchetypeChunk? chunk;
            // 若该列尚无块，从提供器租一块新块并加入列表
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
        /// 在指定列连续追加多个空槽位。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
        /// <param name="count">要追加的槽位数。</param>
        /// <remarks>新块容量仍由 <see cref="GetNextSize" /> 与设置项决定（默认基准 2048）。</remarks>
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
        /// 按全局行号移除实体，并同步从各组件列的对应块中移除槽位。
        /// </summary>
        /// <param name="globalIndex">实体全局行号。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
        /// <param name="removedHandle">被移除行上的组件句柄（若有）。</param>
        /// <param name="changedEntity">若发生尾部交换，则为被挪到当前位置的实体；否则为 <see cref="Entity.Empty" />。</param>
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
        /// 批量按全局行号移除实体，并同步从各列移除槽位（输出被移除句柄）。
        /// </summary>
        /// <param name="globalIndexs">待移除的全局行号集合。</param>
        /// <param name="removedHandles">各被移除行上的句柄输出。</param>
        /// <param name="changedEntities">因交换被移动到目标位置的实体输出。</param>
        /// <param name="changedHandles">随实体一起移动的句柄输出。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 批量按全局行号移除实体，并同步从各列移除槽位（不输出被移除句柄的重载）。
        /// </summary>
        /// <param name="globalIndexs">待移除的全局行号集合。</param>
        /// <param name="changedEntities">因交换被移动到目标位置的实体输出。</param>
        /// <param name="changedHandles">随实体一起移动的句柄输出。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 从列尾开始回收连续的空块并归还提供器。
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

            if (chunkList.Count == 0)
                return;

            chunkList[lastIndex].Next = null;
        }

        /// <summary>
        /// 在指定列的指定块中按局部索引移除槽位。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkIndex">块在列列表中的下标。</param>
        /// <param name="localIndex">块内局部索引。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 在指定列上按多组（块下标、块内索引）批量移除槽位。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkIndexSpan">各行的块下标。</param>
        /// <param name="localIndexSpan">各行的块内局部索引。</param>
        /// <param name="worldVersion">当前世界版本号。</param>
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
        /// 根据实体全局行号查找该列对应的块及块内局部索引。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="globalIndex">实体全局行号。</param>
        /// <param name="foundChunk">找到的块。</param>
        /// <param name="localIndex">块内局部索引。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindChunkForGlobalIndex(int columnIndex, int globalIndex, out ArchetypeChunk foundChunk, out int localIndex)
        {
            return TryFindChunkForGlobalIndex(columnIndex, globalIndex, out foundChunk, out localIndex, out _);
        }

        /// <summary>
        /// 根据实体全局行号查找该列对应的块、块内局部索引及块在列列表中的下标。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="globalIndex">实体全局行号。</param>
        /// <param name="foundChunk">找到的块。</param>
        /// <param name="localIndex">块内局部索引。</param>
        /// <param name="chunkIndex">块在列列表中的下标。</param>
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
        /// 尝试获取指定列的块列表引用。
        /// </summary>
        /// <param name="columnIndex">列索引。</param>
        /// <param name="chunkList">输出的块列表。</param>
        /// <returns>该列存在非空列表时返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetChunkListForColumn(int columnIndex, [NotNullWhen(true)] out ArchetypeChunkList chunkList)
        {
            chunkList = _columns[columnIndex]!;
            return chunkList != null;
        }

        #endregion Find

        #region ComponentHandle

        /// <summary>
        /// 根据全局行号获取该位置上的组件访问句柄；若尚无句柄则租用并写入，同时建立映射。
        /// </summary>
        /// <param name="globalIndex">实体全局行号。</param>
        /// <param name="handle">输出的组件句柄。</param>
        /// <returns>解析成功返回 true；否则返回 false。</returns>
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
        /// 将源全局行上的实体迁移到目标管理器：先对交集列完成拷贝，再统一从源各列移除槽位，最后在实体表中 swap-remove 源行并把实体写入目标行。 分两阶段可避免拷贝失败时已部分 RemoveAt 导致的数据不一致；移除阶段须对 <c>_columns</c> 从 0 起重新递增列下标，不可复用第一段循环结束时的列下标（否则等于列数，会越界）。
        /// </summary>
        /// <param name="oldGlobalIndex">源原型全局行号。</param>
        /// <param name="newManager">目标块管理器。</param>
        /// <param name="newGlobalIndex">目标原型全局行号。</param>
        /// <param name="oldIndexSpan">源侧参与拷贝的列索引序列。</param>
        /// <param name="newIndexSpan">目标侧与 <paramref name="oldIndexSpan" /> 对齐的列索引序列。</param>
        /// <param name="componentTypes">迁移后目标原型上的组件掩码（用于更新句柄）。</param>
        /// <param name="entityRowSwapEntity">
        /// 实体表 swap-remove 时被挪到原迁移行（ <paramref name="oldGlobalIndex" />）的实体；无交换则为 <see cref="Entity.Empty" />。调用方应视情况同步 <c>EntityManager</c> 映射。
        /// </param>
        /// <returns>整段操作成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyToAndRemove(int oldGlobalIndex, ArchetypeChunkManager newManager, int newGlobalIndex, scoped Span<int> oldIndexSpan, scoped Span<int> newIndexSpan, ComponentMask componentTypes, out Entity entityRowSwapEntity)
        {
            entityRowSwapEntity = Entity.Empty;

            if (!Entities.TryFindLocalIndexForGlobalIndex(oldGlobalIndex, out int localIndex, out int chunkIndex) ||
                !newManager.Entities.TryFindLocalIndexForGlobalIndex(newGlobalIndex, out int newLocalIndex, out int newChunkIndex))
                return false;

            int columnSpanIndex = 0;
            int copyLength = oldIndexSpan.Length;
            int columnIndex = 0;
            foreach (var oldChunkList in _columns)
            {
                if (oldChunkList == null)
                {
                    columnIndex++;
                    continue;
                }

                var oldChunk = oldChunkList[chunkIndex];

                if (columnSpanIndex < copyLength &&
                    columnIndex == oldIndexSpan[columnSpanIndex])
                {
                    int newColumnIndex = newIndexSpan[columnSpanIndex];
                    if (!newManager.TryGetChunkListForColumn(newColumnIndex, out var newChunkList))
                        return false;

                    var newChunk = newChunkList[newChunkIndex];

                    if (!oldChunk.TryCopyTo(oldGlobalIndex, newChunk, newGlobalIndex))
                        return false;

                    columnSpanIndex++;
                }

                columnIndex++;
            }

            columnIndex = 0;
            foreach (var oldChunkList in _columns)
            {
                if (oldChunkList == null)
                {
                    columnIndex++;
                    continue;
                }

                var oldChunk = oldChunkList[chunkIndex];
                oldChunk.RemoveAt(localIndex);
                RemoveEmptyChunks(columnIndex);
                columnIndex++;
            }

            ref var info = ref Entities.Span[chunkIndex];
            var entity = info.Entities[localIndex];
            var handle = info.ComponentHandles[localIndex];
            info.Remove(localIndex, out _, out entityRowSwapEntity, out _);

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