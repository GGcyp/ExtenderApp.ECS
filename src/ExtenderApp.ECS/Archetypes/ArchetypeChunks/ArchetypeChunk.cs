using System.Runtime.CompilerServices;
using System.Text;
using ExtenderApp.Contracts;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// Archetype 块基类，负责底层 <see cref="Chunk" /> 的生命周期与槽位管理。 提供块初始化、追加、移除、交换以及链式扩展能力。
    /// </summary>
    internal abstract class ArchetypeChunk : DisposableObject
    {
        /// <summary>
        /// 用于租用与归还底层 Chunk 的池引用。
        /// </summary>
        private readonly ChunkPool _chunkPool;

        /// <summary>
        /// 当前原型块提供者。
        /// </summary>
        private readonly ArchetypeChunkProvider _provider;

        /// <summary>
        /// 底层非托管内存块（Chunk）实例，派生类在 InitializeProtected 中应对其调用 Initialize&lt;T&gt;。
        /// </summary>
        protected Chunk Chunk;

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
        /// 当前块的容量（Chunk 能容纳的元素数量）。 若底层 Chunk 未初始化则返回 0。
        /// </summary>
        public int Capacity => Chunk?.Capacity ?? 0;

        /// <summary>
        /// 当前已占用的槽位数量（实体数量）。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 判断当前块是否已满（Count &gt;= Capacity）。
        /// </summary>
        public bool IsFull => Count >= Capacity;

        /// <summary>
        /// 构造函数：接收 ChunkPool 用于后续租用/归还操作，并可指定块的全局起始索引（默认 0）。
        /// </summary>
        /// <param name="chunkPool">Chunk 对象池实例，不能为空。</param>
        public ArchetypeChunk(ArchetypeChunkProvider provider, ChunkPool chunkPool)
        {
            _provider = provider;
            _chunkPool = chunkPool;
            Chunk = default!;
            Count = 0;
            StartIndex = 0;
        }

        /// <summary>
        /// 初始化当前 ArchetypeChunk：从池中租用一个 Chunk 并调用派生类的受保护初始化逻辑。 可重复调用但仅在未初始化时生效。
        /// </summary>
        public void Initialize(int capacity)
        {
            if (Chunk != null)
                return;

            Chunk = _chunkPool.Rent(capacity);
            Count = 0;
            InitializeProtected();
        }

        /// <summary>
        /// 派生类在此方法中应对底层 Chunk 执行类型相关的初始化（例如调用 Chunk.Initialize&lt;T&gt;）。
        /// </summary>
        protected abstract void InitializeProtected();

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
        public void Remove(int localIndex)
        {
            int last = Count - 1;
            if (localIndex != last)
                Chunk.Swap(localIndex, last);

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
        /// 获取当前块的底层 Chunk 的原始指针，以便在派生类中执行类型安全的内存访问（例如通过 Span/Memory）。
        /// </summary>
        /// <returns>底层 Chunk 的原始指针。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint GetComponentChunkPointer()
        {
            ThrowNotInitialize();
            return Chunk.GetRawPointer();
        }

        /// <summary>
        /// 尝试将当前块内指定全局索引的数据复制到目标 ArchetypeChunk 的指定全局索引位置。 该方法首先检查两个全局索引是否都在各自块的有效范围内，并且目标块类型是否兼容。 若检查通过则执行数据复制并返回 true；否则返回 false。
        /// </summary>
        /// <param name="globalIndex">当前块内的全局索引。</param>
        /// <param name="newArchetypeChunk">目标 ArchetypeChunk 实例。</param>
        /// <param name="newGlobalIndex">目标块内的全局索引。</param>
        public abstract bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex);

        /// <summary>
        /// 交换当前块内两个局部索引位置的数据。 注意：调用前应确保 localIndexA 与 localIndexB 都在 [0, Count) 范围内且块已初始化。 该方法直接调用底层 Chunk 的 Swap 实现，适用于在移除元素时将最后一个元素移动到被移除位置以保持数据紧凑。
        /// </summary>
        /// <param name="localIndexA">要交换的第一个局部索引。</param>
        /// <param name="localIndexB">要交换的第二个局部索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Swap(int localIndexA, int localIndexB) => Chunk.Swap(localIndexA, localIndexB);

        /// <summary>
        /// 将底层 Chunk 归还到池中并清理状态（Count 重置）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReturnChunkToPool()
        {
            if (Chunk != null)
            {
                _chunkPool.Return(Chunk);
                Chunk = null!;
                Count = 0;
            }
        }

        /// <summary>
        /// 从当前提供器租用并链接下一块。
        /// </summary>
        /// <returns>已初始化的下一块实例。</returns>
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
        public void Return()
        {
            Next?.Return();
            _provider.Return(this);
        }

        /// <summary>
        /// 抛出未初始化异常的辅助方法，供公有 API 在使用前调用检查。
        /// </summary>
        protected void ThrowNotInitialize()
        {
            if (Chunk == null)
            {
                throw new InvalidOperationException("当前原型块未初始化,请先调用 Initialize。");
            }
        }

        /// <summary>
        /// 释放非托管资源：归还底层 Chunk 并调用基类释放逻辑。 同时递归释放 Next 链（如果存在）。
        /// </summary>
        protected override void DisposeUnmanagedResources()
        {
            ReturnChunkToPool();
            Next?.Dispose();
            Next = null;
            base.DisposeUnmanagedResources();
        }
    }

    /// <summary>
    /// 泛型 Archetype 块，用于以类型安全方式存储组件列数据。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal sealed class ArchetypeChunk<T> : ArchetypeChunk where T : struct
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
        /// 构造函数：接收所属的提供器与用于分配底层 Chunk 的池引用。
        /// </summary>
        /// <param name="provider">该块所属的 <see cref="ArchetypeChunkProvider{T}" />。</param>
        /// <param name="chunkPool">底层 <see cref="ChunkPool" /> 实例。</param>
        public ArchetypeChunk(ArchetypeChunkProvider<T> provider, ChunkPool chunkPool) : base(provider, chunkPool)
        {
        }

        /// <summary>
        /// 在派生类初始化阶段对底层 Chunk 执行 T 类型的初始化。
        /// </summary>
        protected override void InitializeProtected()
        {
            Chunk.Initialize<T>();
        }

        /// <summary>
        /// 返回当前块链的文本表示。
        /// </summary>
        /// <returns>块链内容字符串。</returns>
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("Chunk [");
            ArchetypeChunk<T>? current = this;
            while (current != null)
            {
                foreach (var item in current)
                {
                    sb.Append(item.ToString());
                    sb.Append(' ');
                }
                current = current.Next;
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// 放入组件数据
        /// </summary>
        /// <param name="index">组件所在偏移位置。</param>
        /// <param name="value">需要被放入数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent(int index, T value) => Chunk.WriteUnsafe(index, value);

        /// <summary>
        /// 获取组件数据
        /// </summary>
        /// <param name="index">组件所在偏移位置。</param>
        /// <returns>组件数据。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent(int index) => Chunk.ReadUnsafe<T>(index);

        /// <summary>
        /// 获取指定索引处组件的引用（不产生值拷贝）。
        /// </summary>
        /// <param name="index">组件在块内的局部索引（0-based）。</param>
        /// <returns>组件引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentRef(int index) => ref Chunk.ReadUnsafeRef<T>(index);

        /// <summary>
        /// 当前块的组件数据跨度。
        /// </summary>
        public Span<T> Span => Chunk.GetSpan<T>();

        ///<inheritdoc/>
        public override bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex)
        {
            if (newArchetypeChunk is not ArchetypeChunk<T> chunk ||
                !TryWithinChunk(globalIndex, out var localIndex) ||
                !chunk.TryWithinChunk(newGlobalIndex, out var newLocalIndex))
                return false;

            ref T value = ref GetComponentRef(localIndex);
            chunk.SetComponent(newLocalIndex, value);
            return true;
        }

        #region Enumerator

        /// <summary>
        /// 获取结构体枚举器，用于按块链遍历组件元素。
        /// </summary>
        public ArchetypeChunkEnumerator GetEnumerator() => new ArchetypeChunkEnumerator(this);

        /// <summary>
        /// 枚举器：按块链顺序遍历全部已占用槽位，并返回对每个元素的引用。 设计为 struct 以避免 foreach 时堆分配。
        /// </summary>
        public unsafe struct ArchetypeChunkEnumerator
        {
            /// <summary>
            /// 当前正在读取的 ArchetypeChunk 引用。
            /// </summary>
            private ArchetypeChunk current;

            /// <summary>
            /// 元素字节大小（单位：字节）。
            /// </summary>
            private int elementSize;

            /// <summary>
            /// 当前块内的局部读取位置（已读取的元素计数）。
            /// </summary>
            private int count;

            /// <summary>
            /// 当前块底层内存的起始指针。
            /// </summary>
            private nint currentPtr;

            /// <summary>
            /// 返回当前元素的全局索引（StartIndex + localIndex）。 注意：在 MoveNext 返回 true 之后有效。
            /// </summary>
            public int GlobalIndex => current.StartIndex + (count - 1);

            /// <summary>
            /// 使用指定 ArchetypeChunk 初始化枚举器状态。
            /// </summary>
            /// <param name="archetypeChunk">起始遍历的块实例（不能为空）。</param>
            public ArchetypeChunkEnumerator(ArchetypeChunk<T> archetypeChunk)
            {
                current = archetypeChunk;
                elementSize = archetypeChunk.Chunk.ElementSize;
                currentPtr = archetypeChunk.GetComponentChunkPointer();
                count = 0;
            }

            /// <summary>
            /// 移动到下一个元素。如果当前块耗尽则尝试切换到下一块直到找到可读取的元素或链尾。 返回 true 表示成功定位到下一个元素，调用者可通过 Current 获取其引用。
            /// </summary>
            public bool MoveNext()
            {
                // 缓存字段为局部变量以减少内存访问
                var cur = current;
                int elemSize = elementSize;
                nint ptr = currentPtr;
                int max = cur.Count;
                int i = count;

                // 若当前块内没有剩余元素，则尝试跳到下一个非空块
                while (i >= max)
                {
                    var next = cur.Next;
                    if (next == null)
                        return false;
                    cur = next;
                    ptr = next.GetComponentChunkPointer();
                    max = cur.Count;
                    i = 0;
                }

                // 还有元素可读，更新状态并返回 true
                count = i + 1;
                current = cur;
                currentPtr = ptr;
                elementSize = elemSize;
                return true;
            }

            /// <summary>
            /// 返回当前元素的引用。 注意：返回引用在枚举器未移动到下一个元素前有效。
            /// </summary>
            public ref T Current => ref Unsafe.AsRef<T>(IntPtr.Add(currentPtr, (count - 1) * elementSize).ToPointer());
        }

        #endregion Enumerator
    }
}