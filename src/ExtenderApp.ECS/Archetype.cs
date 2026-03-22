using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示具有相同组件签名的一组实体集合（原型）。 一个 <see cref="Archetype" /> 通过 <see cref="ArchetypeChunkManager" /> 管理各组件列对应的块数据。
    /// </summary>
    [DebuggerDisplay("Archetype( _componentTypes = {_componentTypes} )")]
    public sealed class Archetype : DisposableObject, IEquatable<Archetype>
    {
        /// <summary>
        /// 当前 Archetype 所属世界的版本管理器。
        /// </summary>
        private readonly WorldVersionManager _wvManager;

        /// <summary>
        /// 当前 Archetype 的组件掩码。
        /// </summary>
        private readonly ComponentMask _componentTypes;

        /// <summary>
        /// 组件列块管理器。
        /// </summary>
        private readonly ArchetypeChunkManager _chunkManager;

        /// <summary>
        /// 获取当前 Archetype 中的实体分段信息列表。每个分段信息包含一个实体数组和对应的全局索引范围，用于快速定位和访问 Archetype 中的实体数据。
        /// </summary>
        internal ArchetypeEntitySegmentInfoList Entities => _chunkManager.Entities;

        /// <summary>
        /// 当前 Archetype 中的关系对列表（仅在存在关系时初始化）。每个关系对包含一个关系类型和一个目标 Archetype 的引用，用于快速查询和更新实体之间的关系。
        /// </summary>
        private readonly List<RelationPair>? _relations;

        /// <summary>
        /// 当前 Archetype 的关系掩码。
        /// </summary>
        private RelationMask relationMask;

        /// <summary>
        /// 获取当前 Archetype 的组件掩码只读引用。
        /// </summary>
        public ref readonly ComponentMask ComponentTypes => ref _componentTypes;

        /// <summary>
        /// 当前 Archetype 的关系掩码只读引用。
        /// </summary>
        public ref readonly RelationMask RelationTypes => ref relationMask;

        /// <summary>
        /// 当前 Archetype 的实体数量。
        /// </summary>
        public int EntityCount { get; private set; }

        /// <summary>
        /// 当前 Archetype 的组件列数量。
        /// </summary>
        public int ComponentCount => _chunkManager.ChunkHeadCount;

        /// <summary>
        /// 当前 Archetype 的关系对只读视图。
        /// </summary>
        public IReadOnlyList<RelationPair>? Relations => _relations;

        /// <summary>
        /// 初始化 <see cref="Archetype" /> 的新实例。
        /// </summary>
        /// <param name="providers">按组件编码位置排列的块提供器数组。</param>
        /// <param name="componentTypes">当前 Archetype 对应的组件掩码。</param>
        /// <param name="relationTypes">当前 Archetype 对应的关系掩码。</param>
        /// <param name="worldVersionManager">世界版本管理器。</param>
        internal Archetype(ArchetypeChunkProvider[] providers, ComponentMask componentTypes, RelationMask relationTypes, WorldVersionManager worldVersionManager)
        {
            _wvManager = worldVersionManager;
            _chunkManager = new(providers);
            _componentTypes = componentTypes;
            relationMask = relationTypes;

            if (!relationTypes.IsEmpty)
                _relations = new();
        }

        #region Relation

        /// <summary>
        /// 添加关系对。 添加前会校验关系类型是否在当前 <see cref="RelationTypes" /> 掩码中。
        /// </summary>
        /// <param name="relationPair">关系对。</param>
        /// <returns>添加（或覆盖）成功返回 true；关系类型不在掩码中返回 false。</returns>
        public bool TryAddRelation(RelationPair relationPair)
        {
            if (!relationMask.On(relationPair.RelationType) || _relations == null)
                return false;

            if (_relations.Contains(relationPair))
                return true;

            _relations.Add(relationPair);
            return true;
        }

        /// <summary>
        /// 添加关系对（失败抛异常）。
        /// </summary>
        /// <param name="relationPair">关系对。</param>
        public void AddRelation(RelationPair relationPair)
        {
            if (!TryAddRelation(relationPair))
                throw new InvalidOperationException($"关系类型 {relationPair.RelationType} 不在当前 Archetype 的 RelationMask 中。");
        }

        /// <summary>
        /// 按关系类型与目标实体添加关系。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="target">目标实体。</param>
        public void AddRelation(RelationType relationType, Entity target)
            => AddRelation(RelationPair.Create(relationType, target));

        /// <summary>
        /// 查询指定关系类型的关系对。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <param name="relationPair">输出关系对。</param>
        /// <returns>找到返回 true；否则返回 false。</returns>
        public bool TryGetRelation(RelationType relationType, out RelationPair relationPair)
        {
            relationPair = default;
            if (!relationMask.On(relationType) || _relations == null)
                return false;

            for (int i = 0; i < _relations.Count; i++)
            {
                if (_relations[i].RelationType == relationType)
                {
                    relationPair = _relations[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 删除指定关系类型的关系对。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>删除成功返回 true；不存在返回 false。</returns>
        public bool RemoveRelation(RelationType relationType)
        {
            if (!relationMask.On(relationType) || _relations == null)
                return false;

            for (int i = 0; i < _relations.Count; i++)
            {
                if (_relations[i].RelationType == relationType)
                {
                    _relations.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion Relation

        #region AddEntity

        /// <summary>
        /// 向当前 Archetype 添加实体，并返回分配到的全局索引。
        /// </summary>
        /// <param name="entity">要添加的实体。</param>
        /// <returns>返回添加后的在当前原型内的全局索引。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int AddEntity(Entity entity)
        {
            _chunkManager.AddEntity(entity, _wvManager.WorldVersion, out var globalIndex);
            EntityCount++;
            return globalIndex;
        }

        /// <summary>
        /// 批量向当前 Archetype 添加实体。
        /// </summary>
        /// <param name="entities">要添加的实体集合。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntityRange(Span<Entity> entities, Span<int> globalIndexSpan)
        {
            _chunkManager.AddEntityRange(entities, globalIndexSpan, _wvManager.WorldVersion);
            EntityCount += entities.Length;
        }

        #endregion AddEntity

        #region RemoveEntity

        /// <summary>
        /// 尝试移除指定全局索引处的实体。
        /// </summary>
        /// <param name="globalIndex">要移除的实体全局索引。</param>
        /// <param name="changedEntity">若触发尾部交换，返回被移动的实体；否则为 <see cref="Entity.Empty" />。</param>
        /// <returns>移除成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveEntity(int globalIndex, out ComponentHandle? removedHandle, out Entity changedEntity, out ComponentHandle? changedHandle)
        {
            if (!_chunkManager.TryRemove(globalIndex, _wvManager.WorldVersion, out removedHandle, out changedEntity, out changedHandle))
                return false;

            EntityCount--;
            return true;
        }

        /// <summary>
        /// 批量尝试移除指定全局索引集合中的实体。
        /// </summary>
        /// <param name="globalIndices">要移除的实体全局索引集合。</param>
        /// <param name="removedHandles">输出被移除实体的组件句柄集合。</param>
        /// <param name="changedEntities">若发生尾部交换，输出被移动实体集合。</param>
        /// <param name="changedHandles">若发生尾部交换，输出被移动实体的组件句柄集合。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveEntityRange(Span<int> globalIndices, Span<ComponentHandle?> removedHandles, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles)
        {
            if (_chunkManager.TryRemoveRange(globalIndices, removedHandles, changedEntities, changedHandles, _wvManager.WorldVersion))
            {
                EntityCount -= globalIndices.Length;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 批量尝试移除指定全局索引集合中的实体。
        /// </summary>
        /// <param name="globalIndices">要移除的实体全局索引集合。</param>
        /// <param name="changedEntities">若发生尾部交换，输出被移动实体集合。</param>
        /// <param name="changedHandles">若发生尾部交换，输出被移动实体的组件句柄集合。</param>
        /// <returns>全部移除成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveEntityRange(Span<int> globalIndices, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles)
        {
            if (_chunkManager.TryRemoveRange(globalIndices, changedEntities, changedHandles, _wvManager.WorldVersion))
            {
                EntityCount -= globalIndices.Length;
                return true;
            }
            return false;
        }

        #endregion RemoveEntity

        #region Chunk

        /// <summary>
        /// 尝试按列索引获取指定组件列的块列表。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="index">组件列索引。</param>
        /// <param name="chunks">输出对应类型的块列表。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetChunkList<T>(int index, [NotNullWhen(true)] out ArchetypeChunkList<T> chunks) where T : struct
        {
            chunks = default!;
            if (_chunkManager.TryGetChunkListForColumn(index, out var chunkList) && chunkList is ArchetypeChunkList<T> archetypeChunks)
            {
                return (chunks = archetypeChunks) != null;
            }
            return false;
        }

        /// <summary>
        /// 尝试按列索引获取头块（强类型版本）。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="index">组件列索引。</param>
        /// <param name="component">输出头块。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>(int index, [NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct
        {
            component = default!;
            if (TryGetHeadChunk(index, out ArchetypeChunk chunk) && chunk is ArchetypeChunk<T> typedChunk)
            {
                component = typedChunk;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试按列索引获取头块。
        /// </summary>
        /// <param name="index">组件列索引。</param>
        /// <param name="component">输出头块。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk(int index, [NotNullWhen(true)] out ArchetypeChunk component)
        {
            component = default!;
            if (index < 0 || index >= _chunkManager.ChunkHeadCount)
                return false;

            component = _chunkManager.GetHead(index)!;
            return component != null;
        }

        /// <summary>
        /// 尝试按组件类型获取头块。
        /// </summary>
        /// <param name="componentType">组件类型描述。</param>
        /// <param name="component">输出头块。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk(ComponentType componentType, [NotNullWhen(true)] out ArchetypeChunk component)
        {
            component = default!;
            if (!_componentTypes.TryGetEncodedPosition(componentType, out int index))
                return false;

            if (index < 0 || index >= _chunkManager.ChunkHeadCount)
                return false;

            component = _chunkManager.GetHead(index)!;
            return component != null;
        }

        /// <summary>
        /// 尝试按组件类型获取头块（强类型版本）。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="componentType">组件类型描述。</param>
        /// <param name="component">输出头块。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>(ComponentType componentType, [NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct
        {
            return TryGetHeadChunk(componentType, out ArchetypeChunk chunk) && chunk is ArchetypeChunk<T> typedChunk ? (component = typedChunk) != null : (component = default!) != null;
        }

        /// <summary>
        /// 尝试按泛型组件类型获取头块。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="component">输出头块。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>([NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct
        {
            return TryGetHeadChunk(ComponentType.Create<T>(), out component);
        }

        #endregion Chunk

        #region SetComponent

        /// <summary>
        /// 设置指定实体索引处的组件值；若失败则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">组件值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetComponent<T>(int globalIndex, T component) where T : struct
        {
            if (!TrySetComponent(globalIndex, component))
                throw new InvalidOperationException($"未找到指定类型的块更新 {globalIndex} : {typeof(T)}");
        }

        /// <summary>
        /// 尝试设置指定实体索引处的组件值。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">组件值。</param>
        /// <returns>设置成功返回 true；否则返回 false。</returns>
        internal bool TrySetComponent<T>(int globalIndex, T component) where T : struct
        {
            if (!_componentTypes.TryGetEncodedPosition(ComponentType.Create<T>(), out int columnIndex) ||
                !_chunkManager.TryFindChunkForGlobalIndex(columnIndex, globalIndex, out var chuck, out int localIndex) ||
                chuck is not ArchetypeChunk<T> c)
                return false;

            c.Version = _wvManager.WorldVersion;
            c.SetComponent(localIndex, component);
            return true;
        }

        #endregion SetComponent

        #region GetComponent

        /// <summary>
        /// 获取指定实体索引处的组件值；若失败则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <returns>组件值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetComponent<T>(int globalIndex) where T : struct
        {
            if (TryGetComponent(globalIndex, out T component))
                return component;

            throw new InvalidOperationException($"未找到指定实例ID组件块更新 {globalIndex} : {typeof(T)}");
        }

        /// <summary>
        /// 尝试读取指定实体索引处的组件值。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">输出组件值。</param>
        /// <returns>读取成功返回 true；否则返回 false。</returns>
        internal bool TryGetComponent<T>(int globalIndex, out T component) where T : struct
        {
            component = default;
            if (!_componentTypes.TryGetEncodedPosition(ComponentType.Create<T>(), out int columnIndex) ||
                !_chunkManager.TryFindChunkForGlobalIndex(columnIndex, globalIndex, out var chuck, out int localIndex) ||
                chuck is not ArchetypeChunk<T> c)
                return false;

            component = c.GetComponent(localIndex);
            return true;
        }

        #endregion GetComponent

        #region Copy

        /// <summary>
        /// 将指定实体从当前 Archetype 复制到目标 Archetype 后，再从当前 Archetype 移除。
        /// </summary>
        /// <param name="globalIndex">当前 Archetype 中的实体全局索引。</param>
        /// <param name="newArchetype">目标 Archetype。</param>
        /// <param name="newGlobalIndex">目标 Archetype 中的实体全局索引。</param>
        internal bool TryCopyToAndRemove(int globalIndex, Archetype newArchetype, int newGlobalIndex)
        {
            const int CopyThreshold = 512;

            if (newArchetype == null)
                return false;

            var sameTypeCount = ComponentTypes.GetSameTypeCount(newArchetype.ComponentTypes);
            if (sameTypeCount == 0)
                return false;

            if (sameTypeCount <= CopyThreshold)
            {
                Span<int> oldIndexSpant = stackalloc int[sameTypeCount];
                Span<int> newIndexSpant = stackalloc int[sameTypeCount];
                return TryCopyToAndRemove(globalIndex, newArchetype, newGlobalIndex, oldIndexSpant, newIndexSpant);
            }

            var oldIndexArray = ArrayPool<int>.Shared.Rent(sameTypeCount);
            var newIndexArray = ArrayPool<int>.Shared.Rent(sameTypeCount);

            Span<int> oldIndexSpan = oldIndexArray.AsSpan(0, sameTypeCount);
            Span<int> newIndexSpan = newIndexArray.AsSpan(0, sameTypeCount);

            try
            {
                return TryCopyToAndRemove(globalIndex, newArchetype, newGlobalIndex, oldIndexSpan, newIndexSpan);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(oldIndexArray);
                ArrayPool<int>.Shared.Return(newIndexArray);
            }
        }

        /// <summary>
        /// 执行复制核心逻辑：先计算列索引映射，再逐列复制组件数据。
        /// </summary>
        /// <param name="globalIndex">当前 Archetype 中的实体全局索引。</param>
        /// <param name="newArchetype">目标 Archetype。</param>
        /// <param name="newGlobalIndex">目标 Archetype 中的实体全局索引。</param>
        /// <param name="oldIndexSpan">当前 Archetype 的列索引缓存。</param>
        /// <param name="newIndexSpan">目标 Archetype 的列索引缓存。</param>
        /// <returns>复制成功返回 true；否则返回 false。</returns>
        private bool TryCopyToAndRemove(int globalIndex, Archetype newArchetype, int newGlobalIndex, scoped Span<int> oldIndexSpan, scoped Span<int> newIndexSpan)
        {
            int copiedCount = 0;
            int oldColumnIndex = 0;

            foreach (var componentType in ComponentTypes)
            {
                if (!newArchetype.ComponentTypes.TryGetEncodedPosition(componentType, out var newColumnIndex))
                {
                    oldColumnIndex++;
                    continue;
                }

                oldIndexSpan[copiedCount] = oldColumnIndex;
                newIndexSpan[copiedCount] = newColumnIndex;
                copiedCount++;
                oldColumnIndex++;
            }

            if (copiedCount == 0)
                return false;

            return _chunkManager.TryCopyToAndRemove(
                globalIndex,
                newArchetype._chunkManager,
                newGlobalIndex,
                oldIndexSpan[..copiedCount],
                newIndexSpan[..copiedCount],
                newArchetype.ComponentTypes);
        }

        #endregion Copy

        #region Handle

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetComponentHandle(int globalIndex, out ComponentHandle handle)
        {
            if (_chunkManager.TryGetComponentHandle(globalIndex, out handle))
            {
                handle.ComponentTypes = ComponentTypes;
                return true;
            }
            return false;
        }

        #endregion Handle

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            for (int i = 0; i < _chunkManager.ChunkHeadCount; i++)
            {
                var head = _chunkManager.GetHead(i);
                head?.Dispose();
            }
        }

        #region Object Overrides & Equality

        /// <summary>
        /// 判断当前 Archetype 与另一个 Archetype 是否等价（按组件掩码比较）。
        /// </summary>
        /// <param name="other">要比较的 Archetype。</param>
        /// <returns>等价返回 true；否则返回 false。</returns>
        public bool Equals(Archetype? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return _componentTypes.Equals(other._componentTypes);
        }

        /// <summary>
        /// 判断当前对象是否与指定对象相等。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public override bool Equals(object? obj) => Equals(obj as Archetype);

        /// <summary>
        /// 获取当前 Archetype 的哈希码。
        /// </summary>
        /// <returns>哈希码值。</returns>
        public override int GetHashCode() => _componentTypes.GetHashCode();

        /// <summary>
        /// 返回当前 Archetype 的可读字符串。
        /// </summary>
        /// <returns>字符串表示。</returns>
        public override string ToString() => $"Archetype( _componentTypes = {_componentTypes} )";

        /// <summary>
        /// 比较两个 Archetype 是否相等。
        /// </summary>
        public static bool operator ==(Archetype? lhs, Archetype? rhs) => Equals(lhs, rhs);

        /// <summary>
        /// 比较两个 Archetype 是否不相等。
        /// </summary>
        public static bool operator !=(Archetype? lhs, Archetype? rhs) => !Equals(lhs, rhs);

        /// <summary>
        /// 将 Archetype 隐式转换为其组件掩码。
        /// </summary>
        /// <param name="archetype">源 Archetype。</param>
        public static implicit operator ComponentMask(Archetype archetype) => archetype._componentTypes;

        #endregion Object Overrides & Equality
    }
}