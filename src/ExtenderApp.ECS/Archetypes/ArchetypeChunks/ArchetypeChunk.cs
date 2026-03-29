using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// Archetype 块基类，负责底层 <see cref="Chunk" /> 的生命周期与槽位管理。 提供块初始化、追加、移除、交换以及链式扩展能力。
    /// </summary>
    internal abstract class ArchetypeChunk : DisposableObject
    {
        /// <summary>
        /// 当前原型块提供者。
        /// </summary>
        private readonly ArchetypeChunkProvider _provider;

        /// <summary>
        /// 链表中的下一块引用（用于 Archetype 内维护块链）。
        /// </summary>
        public ArchetypeChunk? Next { get; set; }

        /// <summary>
        /// 块对应的全局起始索引（用于将全局实体索引映射到块内局部索引）。 在创建时可指定，此值用于 AddEntity 返回带偏移的全局索引，以及将外部传入的全局索引映射为局部索引进行操作。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 当前块所属原型的版本号（由提供器维护），用于在外部访问时进行版本一致性检查。
        /// </summary>
        public ulong Version { get; set; }

        /// <summary>
        /// 当前块的容量（chunk 能容纳的元素数量）。 若底层 chunk 未初始化则返回 0。
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 当前已占用的槽位数量（实体数量）。
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// 获取当前块是否在对象池中。 该属性由提供器在租用时设置为 true，在归还时设置为 false。未初始化的块也被视为不活跃。外部 API 可通过此属性判断当前块是否可用或已被回收。
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 判断当前块是否已满（Count &gt;= Capacity）。
        /// </summary>
        public bool IsFull => Count >= Capacity;

        /// <summary>
        /// 构造函数：接收 ChunkPool 用于后续租用/归还操作，并可指定块的全局起始索引（默认 0）。
        /// </summary>
        /// <param name="chunkPool">chunk 对象池实例，不能为空。</param>
        public ArchetypeChunk(ArchetypeChunkProvider provider)
        {
            _provider = provider;
            Count = 0;
            StartIndex = 0;
        }

        /// <summary>
        /// 初始化当前 ArchetypeChunk：从池中租用一个 chunk 并调用派生类的受保护初始化逻辑。 可重复调用但仅在未初始化时生效。
        /// </summary>
        public void Initialize(int capacity)
        {
            Count = 0;
            Capacity = capacity;
            InitializeProtected();
            IsActive = true;
        }

        /// <summary>
        /// 尝试向当前块追加一个实体槽，返回全局索引（StartIndex + localIndex）。 若块已满或未初始化返回 false。
        /// </summary>
        /// <param name="index">输出分配到的全局索引（若成功）。</param>
        /// <returns>分配成功返回 true，否则 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd()
        {
            ThrowNotInitialize();
            if (IsFull)
                return false;

            Count++;
            return true;
        }

        /// <summary>
        /// 尝试批量追加槽位。
        /// </summary>
        /// <param name="count">请求追加的数量。</param>
        /// <param name="addCount">实际追加数量。</param>
        /// <returns>当前块可追加时返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdds(int count, out int addCount)
        {
            addCount = 0;
            ThrowNotInitialize();
            if (IsFull)
                return false;

            int free = Capacity - Count;
            addCount = Math.Min(count, free);
            Count += addCount;
            return addCount > 0;
        }

        /// <summary>
        /// 移除指定局部索引的元素，并通过尾部交换保持数据紧凑。
        /// </summary>
        /// <param name="localIndex">块内局部索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int localIndex)
        {
            RemoveAtProtected(localIndex);
            Count--;
        }

        /// <summary>
        /// 尝试获取指定全局索引是否在当前块的有效范围内（基于 StartIndex 与 Count）。
        /// </summary>
        /// <param name="globalIndex">指定全局索引。</param>
        /// <param name="localIndex">获取当前块内的局部索引（若返回 true）。</param>
        /// <returns>若索引位于 [StartIndex, StartIndex + Count) 范围内则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWithinChunk(int globalIndex, out int localIndex)
        {
            localIndex = globalIndex - StartIndex;
            return localIndex >= 0 && localIndex < Count;
        }

        /// <summary>
        /// 从当前提供器租用并链接下一块。
        /// </summary>
        /// <returns>已初始化的下一块实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeChunk RentAndSetNext(int capacity)
        {
            ArchetypeChunk chunk = _provider.Rent(StartIndex + Capacity);
            Next = chunk;
            Next.Initialize(capacity);
            return chunk;
        }

        /// <summary>
        /// 将当前 ArchetypeChunk 返回给其提供器。实现应执行必要的清理与回收逻辑（例如调用提供器的 Return）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            ArchetypeChunk? cur = this;
            while (cur != null)
            {
                var next = cur.Next;
                cur.IsActive = false;
                cur.Next = null;
                cur.ReturnChunkToPool();
                cur._provider.Return(cur);
                cur = next?.IsActive == true ? next : null;
            }
        }

        /// <summary>
        /// 释放非托管资源：归还底层 chunk 并调用基类释放逻辑。 同时递归释放 Next 链（如果存在）。
        /// </summary>
        protected override void DisposeUnmanagedResources()
        {
            ArchetypeChunk? cur = this;
            while (cur != null)
            {
                cur.ReturnChunkToPool();
                var next = cur.Next;
                cur.Next = null;
                cur = next;
            }

            base.DisposeUnmanagedResources();
        }

        #region Abstract Methods

        /// <summary>
        /// 派生类在此方法中应对底层 chunk 执行类型相关的初始化（例如调用 chunk.Initialize&lt;T1&gt;）。
        /// </summary>
        protected abstract void InitializeProtected();

        /// <summary>
        /// 尝试将当前块内指定全局索引的数据复制到目标 ArchetypeChunk 的指定全局索引位置。 该方法首先检查两个全局索引是否都在各自块的有效范围内，并且目标块类型是否兼容。 若检查通过则执行数据复制并返回 true；否则返回 false。
        /// </summary>
        /// <param name="globalIndex">当前块内的全局索引。</param>
        /// <param name="newArchetypeChunk">目标 ArchetypeChunk 实例。</param>
        /// <param name="newGlobalIndex">目标块内的全局索引。</param>
        public abstract bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex);

        /// <summary>
        /// 交换当前块内两个局部索引位置的数据。 注意：调用前应确保 localIndexA 与 localIndexB 都在 [0, Count) 范围内且块已初始化。 该方法直接调用底层 chunk 的 Swap 实现，适用于在移除元素时将最后一个元素移动到被移除位置以保持数据紧凑。
        /// </summary>
        /// <param name="localIndexA">要交换的第一个局部索引。</param>
        /// <param name="localIndexB">要交换的第二个局部索引。</param>
        public abstract void Swap(int localIndexA, int localIndexB);

        /// <summary>
        /// 删除指定局部索引处的数据，并通过尾部交换保持数据紧凑。
        /// </summary>
        /// <param name="localIndex">要删除的局部索引。</param>
        protected abstract void RemoveAtProtected(int localIndex);

        /// <summary>
        /// 复制指定数量的数据从源指针到当前块的底层 chunk 内存。 注意：调用前应确保块已初始化且 soure 指向的数据大小不超过当前块剩余容量。 该方法直接调用底层 chunk 的 CopiedUnsafe 实现，适用于在批量添加或迁移数据时执行高效的内存复制。
        /// </summary>
        /// <param name="localIndex">当前块内的局部索引，表示复制数据的起始位置。</param>
        /// <param name="soure">数据源。</param>
        /// <param name="count">元素个数。</param>
        public abstract void CopiedUnsafe(int localIndex, nint soure, int count);

        /// <summary>
        /// 将底层 chunk 归还到池中并清理状态（Count 重置）。
        /// </summary>
        protected abstract void ReturnChunkToPool();

        /// <summary>
        /// 抛出未初始化异常的辅助方法，供公有 API 在使用前调用检查。
        /// </summary>
        protected abstract void ThrowNotInitialize();

        #endregion Abstract Methods
    }

    /// <summary>
    /// 泛型 Archetype 块，用于以类型安全方式存储组件列数据。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal abstract class ArchetypeChunk<T> : ArchetypeChunk
    {
        /// <summary>
        /// 链表中的下一块引用（用于 Archetype 内维护块链）。
        /// </summary>
        public new ArchetypeChunk<T>? Next
        {
            get => base.Next as ArchetypeChunk<T>;
            set => base.Next = value;
        }

        /// <summary>
        /// 当前块的组件数据跨度。
        /// </summary>
        public abstract Span<T> Span { get; }

        /// <summary>
        /// 构造函数：接收所属的提供器与用于分配底层 chunk 的池引用。
        /// </summary>
        /// <param name="provider">该块所属的 <see cref="ArchetypeChunkProvider{T}" />。</param>
        /// <param name="chunkPool">底层 <see cref="ChunkPool" /> 实例。</param>
        public ArchetypeChunk(ArchetypeChunkProvider<T> provider) : base(provider)
        {
        }

        /// <summary>
        /// 返回当前块链的文本表示。
        /// </summary>
        /// <returns>块链内容字符串。</returns>
        public override string ToString() => $"ArchetypeChunk<{typeof(T).Name}> (StartIndex: {StartIndex}, Count: {Count}, Capacity: {Capacity})";

        /// <summary>
        /// 放入组件数据
        /// </summary>
        /// <param name="index">组件所在偏移位置。</param>
        /// <param name="value">需要被放入数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void SetComponent(int index, T value);

        /// <summary>
        /// 获取组件数据
        /// </summary>
        /// <param name="index">组件所在偏移位置。</param>
        /// <returns>组件数据。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract T GetComponent(int index);

        /// <summary>
        /// 获取指定索引处组件的引用（不产生值拷贝）。
        /// </summary>
        /// <param name="index">组件在块内的局部索引（0-based）。</param>
        /// <returns>组件引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract ref T GetComponentRef(int index);
    }
}