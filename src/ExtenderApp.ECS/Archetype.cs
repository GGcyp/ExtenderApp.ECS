using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 表示具有相同组件签名的一组实体集合（原型/类型槽）。
    /// 一个 Archetype 包含若干个按组件列打包的 `ArchetypeChunk`，以及描述该原型组件集合的 `ComponentMask`。
    /// </summary>
    [DebuggerDisplay("Archetype( _componentTypes = {_componentTypes} )")]
    public sealed class Archetype : DisposableObject, IEquatable<Archetype>
    {
        /// <summary>
        /// 当前 Archetype 所属的世界版本管理器引用。
        /// </summary>
        private readonly WorldVersionManager _wvManager;

        /// <summary>
        /// 管理每个组件列的块头与缓存。
        /// </summary>
        private readonly ArchetypeChunkManager _chunkManager;

        /// <summary>
        /// 表示该 Archetype 包含的组件类型掩码。
        /// </summary>
        private readonly ComponentMask _componentTypes;

        /// <summary>
        /// 获取当前 Archetype 的组件类型掩码引用。调用方可通过该引用查询包含的组件类型信息。
        /// </summary>
        public ref readonly ComponentMask ComponentTypes => ref _componentTypes;

        /// <summary>
        /// 当前 Archetype 中的实体数量（即所有组件列中槽位总数）。
        /// </summary>
        public int EntityCount { get; private set; }

        /// <summary>
        /// 获取当前 Archetype 中的组件数量。
        /// </summary>
        public int ComponentCount => _chunkManager.ChunkHeadCount;

        /// <summary>
        /// 内部构造函数：创建一个包含指定块列表和组件掩码的新 Archetype。
        /// </summary>
        /// <param name="providers">按组件编码位置排列的 ArchetypeChunkProvider 列表。</param>
        /// <param name="componentTypes">描述组件集合的 ComponentMask。</param>
        internal Archetype(ArchetypeChunkProvider[] providers, ComponentMask componentTypes, WorldVersionManager worldVersionManager)
        {
            _wvManager = worldVersionManager;
            _chunkManager = new(providers);
            _componentTypes = componentTypes;
        }

        #region Operations

        /// <summary>
        /// 向 Archetype 添加一个新的实体槽。方法会遍历所有组件列对应的块并尝试在某个块中追加槽位。
        /// 返回值为分配到的实体全局索引（由块的 StartIndex + 局部索引确定），失败时返回 -1。
        /// 注意：调用方需在调用后对每个组件列的内存进行初始化写入。
        /// </summary>
        /// <param name="entity">要添加的实体实例（仅用于获取 EntityId 以便分配槽位）。</param>
        /// <returns>分配到的实体全局索引，若未能分配则返回 -1。</returns>
        internal int AddEntity(Entity entity)
        {
            int globalIndex = -1;
            for (int i = 0; i < _chunkManager.ChunkHeadCount; i++)
            {
                if (_chunkManager.TryAddToColumn(entity, i, _wvManager.WorldVersion, out var idx))
                {
                    globalIndex = idx; // last assigned index will be returned
                }
            }
            EntityCount++; // 更新实体数量
            return globalIndex;
        }

        /// <summary>
        /// 从 Archetype 中移除指定实体索引对应的槽位。
        /// 方法会在每个组件列的块链中查找并移除对应的槽（若存在）。
        /// </summary>
        /// <param name="globalIndex">要移除的实体全局索引。</param>
        /// <param name="changedEntity">输出参数：如果移除操作导致当前 Archetype 中的某个实体索引被移动（即最后一个实体索引被移除后填补了被移除的槽位），则返回该被移动的实体；否则返回 -1。</param>
        /// <param name="newIndex">输出参数：如果移除成功且导致某个实体索引被移动，则返回被移动实体的新全局索引；否则返回 -1。</param>
        internal bool TryRemoveEntity(int globalIndex, out Entity changedEntity, out int newIndex)
        {
            changedEntity = Entity.Empty;
            newIndex = globalIndex;
            for (int i = 0; i < _chunkManager.ChunkHeadCount; i++)
            {
                if (!_chunkManager.TryRemoveFromColumn(i, globalIndex, _wvManager.WorldVersion, out changedEntity, out newIndex))
                {
                    return false; // 只要有一个组件列未找到对应槽位就认为移除失败
                }
            }
            EntityCount--; // 更新实体数量
            return true;
        }

        /// <summary>
        /// 尝试获取指定位置的组件列块列表（即该组件类型在 Archetype 中的所有块）。如果该位置没有对应的块链则返回 false。
        /// </summary>
        /// <typeparam name="T">指定的组件类型。</typeparam>
        /// <param name="index">要查询的索引位置。</param>
        /// <param name="chunks">若返回 true，则输出对应的 ArchetypeChunkList 引用（非 null）。</param>
        /// <returns>若成功找到并类型匹配则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetChunkList<T>(int index, [NotNullWhen(true)] out ArchetypeChunkList<T> chunks) where T : struct, IComponent
        {
            chunks = default!;
            if (_chunkManager.TryGetChunkListForColumn(index, out var chunkList) && chunkList is ArchetypeChunkList<T> archetypeChunks)
            {
                return (chunks = archetypeChunks) != null;
            }
            return false;
        }

        /// <summary>
        /// 尝试获取指定位置的 ArchetypeChunk。
        /// </summary>
        /// <param name="index">要查询的索引位置。</param>
        /// <param name="component">若返回 true，则输出对应的 ArchetypeChunk 引用（非 null）。</param>
        /// <returns>若成功找到则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>(int index, [NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct, IComponent
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
        /// 尝试获取指定位置的 ArchetypeChunk。
        /// </summary>
        /// <param name="index">要查询的索引位置。</param>
        /// <param name="component">若返回 true，则输出对应的 ArchetypeChunk 引用（非 null）。</param>
        /// <returns>若成功找到则返回 true，否则返回 false。</returns>
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
        /// 尝试获取指定组件类型在本 Archetype 中对应的 `ArchetypeChunk{T}`。
        /// 如果该组件类型不在 _componentTypes 中或对应位置不是 T 类型，则返回 false。
        /// </summary>
        /// <param name="componentType">要查询的组件类型描述。</param>
        /// <param name="component">若返回 true，则输出对应的 ArchetypeChunk; 引用（非 null）。</param>
        /// <returns>若成功找到并类型匹配则返回 true，否则 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk(ComponentType componentType, [NotNullWhen(true)] out ArchetypeChunk component)
        {
            component = default!;
            if (!_componentTypes.TryGetEncodedPosition(componentType, out int index))
                return false;

            // 边界防护：确保编码位置在 providers 列表长度范围内
            if (index < 0 || index >= _chunkManager.ChunkHeadCount)
                return false;

            component = _chunkManager.GetHead(index)!;
            return component != null;
        }

        /// <summary>
        /// 尝试获取指定组件类型在本 Archetype 中对应的 `ArchetypeChunk{T}`。
        /// 如果该组件类型不在 _componentTypes 中或对应位置不是 T 类型，则返回 false。
        /// </summary>
        /// <typeparam name="T">期望的组件类型。</typeparam>
        /// <param name="componentType">要查询的组件类型描述。</param>
        /// <param name="component">若返回 true，则输出对应的 ArchetypeChunk&lt;T&gt; 引用（非 null）。</param>
        /// <returns>若成功找到并类型匹配则返回 true，否则 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>(ComponentType componentType, [NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct, IComponent
        {
            return TryGetHeadChunk(componentType, out ArchetypeChunk chunk) && chunk is ArchetypeChunk<T> typedChunk ? (component = typedChunk) != null : (component = default!) != null;
        }

        /// <summary>
        /// 尝试根据泛型类型在本 Archetype 中获取对应的 `ArchetypeChunk{T}`。
        /// </summary>
        /// <typeparam name="T">期望的组件类型。</typeparam>
        /// <param name="component">若返回 true，则输出对应的 ArchetypeChunk&lt;T&gt; 引用。</param>
        /// <returns>若成功找到并类型匹配则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetHeadChunk<T>([NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct, IComponent
        {
            return TryGetHeadChunk(ComponentType.Create<T>(), out component);
        }

        /// <summary>
        /// 将指定实体的某一组件字段设置为指定值；若未找到对应组件列或槽位则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">要写入的组件值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetComponent<T>(int globalIndex, T component) where T : struct, IComponent
        {
            if (!TrySetComponent(globalIndex, component))
                throw new InvalidOperationException($"未找到指定类型的块更新 {globalIndex} : {typeof(T)}");
        }

        /// <summary>
        /// 获取指定实体的组件值，若未找到则抛出异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <returns>指定组件的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetComponent<T>(int globalIndex) where T : struct, IComponent
        {
            if (TryGetComponent(globalIndex, out T component))
                return component;

            throw new InvalidOperationException($"未找到指定实例ID组件块更新 {globalIndex} : {typeof(T)}");
        }

        /// <summary>
        /// 尝试设置指定实体的组件值（若实体存在且组件列存在则返回 true）。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">要写入的组件值。</param>
        /// <returns>设置成功返回 true，否则返回 false。</returns>
        internal bool TrySetComponent<T>(int globalIndex, T component) where T : struct, IComponent
        {
            if (!_componentTypes.TryGetEncodedPosition(ComponentType.Create<T>(), out int columnIndex) ||
                !_chunkManager.TryFindChunkForGlobalIndex(columnIndex, globalIndex, out var chuck, out int localIndex) ||
                chuck is not ArchetypeChunk<T> c)
                return false;

            c.Version = _wvManager.WorldVersion; // 更新块版本以触发系统更新
            c.SetComponent(localIndex, component);
            return true;
        }

        /// <summary>
        /// 尝试读取指定实体的组件值（若实体存在且组件列存在则返回 true 并通过 out 返回该组件）。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="globalIndex">实体全局索引。</param>
        /// <param name="component">输出组件值（若返回 true）。</param>
        /// <returns>读取成功返回 true，否则返回 false。</returns>
        internal bool TryGetComponent<T>(int globalIndex, out T component) where T : struct, IComponent
        {
            component = default;
            if (!_componentTypes.TryGetEncodedPosition(ComponentType.Create<T>(), out int columnIndex) ||
                !_chunkManager.TryFindChunkForGlobalIndex(columnIndex, globalIndex, out var chuck, out int localIndex) ||
                chuck is not ArchetypeChunk<T> c)
                return false;

            component = c.GetComponent(localIndex);
            return true;
        }

        /// <summary>
        /// 将指定实体（由 globalIndex 指定）在当前 Archetype 中的组件数据复制到目标 Archetype 中（仅复制两者共有的组件列），
        /// 然后从当前 Archetype 中移除该实体的数据。
        /// 复制使用反射调用 Archetype 上的泛型读/写方法（GetComponent/SetComponent）。
        /// </summary>
        /// <param name="newArchetype">目标 Archetype。</param>
        /// <param name="globalIndex">要复制并移除的实体全局索引。</param>
        /// <param name="newGlobalIndex">在目标 Archetype 中分配到的实体全局索引。</param>
        /// <param name="changedEntity">输出参数：如果移除操作导致当前 Archetype 中的某个实体索引被移动（即最后一个实体索引被移除后填补了被移除的槽位），则返回该被移动的实体；否则返回 -1。</param>
        /// <param name="newIndex">输出参数：如果移除成功且导致某个实体索引被移动，则返回被移动实体的新全局索引；否则返回 -1。</param>
        internal void CopyToAndRemoveEntity(int globalIndex, Archetype newArchetype, int newGlobalIndex, out Entity changedEntity, out int newIndex)
        {
            ArgumentNullException.ThrowIfNull(newArchetype, nameof(newGlobalIndex));
            changedEntity = Entity.Empty;
            newIndex = globalIndex;
            foreach (var type in newArchetype.ComponentTypes)
            {
                if (_componentTypes.TryGetEncodedPosition(type, out int oldColumnIndex) &&
                    newArchetype.ComponentTypes.TryGetEncodedPosition(type, out int newColumnIndex) &&
                    _chunkManager.TryFindChunkForGlobalIndex(oldColumnIndex, globalIndex, out var chuck, out int localIndex) &&
                    newArchetype._chunkManager.TryFindChunkForGlobalIndex(newColumnIndex, newGlobalIndex, out var newChuck, out int newLocalIndex) &&
                    chuck != null && newChuck != null)
                {
                    chuck.CopyTo(globalIndex, newChuck, newGlobalIndex);
                    chuck.Remove(localIndex, out changedEntity);
                }
            }
        }

        #endregion Operations

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
        /// 判断当前 Archetype 是否与另一个 Archetype 等价（组件掩码相等）。
        /// </summary>
        /// <param name="other">要比较的另一个 Archetype。</param>
        /// <returns>等价返回 true，否则返回 false。</returns>
        public bool Equals(Archetype? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return _componentTypes.Equals(other._componentTypes);
        }

        /// <summary>
        /// 重写 Object.Equals，尝试按 Archetype 相等性进行比较。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        public override bool Equals(object? obj) => Equals(obj as Archetype);

        /// <summary>
        /// 获取当前 Archetype 的哈希码（基于组件掩码）。
        /// </summary>
        public override int GetHashCode() => _componentTypes.GetHashCode();

        /// <summary>
        /// 返回当前 Archetype 的可读字符串表示，用于调试与日志。
        /// </summary>
        public override string ToString() => $"Archetype( _componentTypes = {_componentTypes} )";

        /// <summary>
        /// 等号运算符重载，按 Archetype 等价性比较。
        /// </summary>
        public static bool operator ==(Archetype? lhs, Archetype? rhs) => Equals(lhs, rhs);

        /// <summary>
        /// 不等号运算符重载，按 Archetype 等价性比较。
        /// </summary>
        public static bool operator !=(Archetype? lhs, Archetype? rhs) => !Equals(lhs, rhs);

        /// <summary>
        /// 隐式转换：将 Archetype 转换为其 ComponentMask 表示。
        /// </summary>
        /// <param name="archetype">源 Archetype 实例。</param>
        public static implicit operator ComponentMask(Archetype archetype) => archetype._componentTypes;

        #endregion Object Overrides & Equality
    }
}