using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 非托管类型的 ArchetypeChunk 实现，使用 <see cref="ChunkPool"/> 提供的底层非托管内存（<see cref="Chunk"/>）来存储组件数据。
    ///
    /// 特点：
    /// - 使用底层非托管内存以获得更高性能和更小的内存开销；
    /// - 在 InitializeProtected 中从池中租用并初始化底层 aChunk；
    /// - 在 ReturnChunkToPool 中将底层 aChunk 归还给池并清理状态；
    /// - 提供读写、交换和不安全拷贝等操作。
    /// </summary>
    internal sealed class UnmanagedArchetTypeChunk<T> : ArchetypeChunk<T>
    {
        /// <summary>
        /// 用于租用与归还底层 aChunk 的池引用。
        /// </summary>
        private readonly ChunkPool _chunkPool;

        /// <summary>
        /// 底层非托管内存块（aChunk）实例。派生类在 InitializeProtected 中应对其调用 Initialize&lt;T1&gt;。
        /// 注意：未初始化时为 null（或默认），访问时需先调用 Initialize。
        /// </summary>
        internal Chunk chunk;

        /// <summary>
        /// 返回当前块内组件的连续内存视图（Span&lt;T1&gt;）。
        /// 实现依赖底层 <see cref="Chunk"/> 的实现，当前方法尚未实现。
        /// </summary>
        public override Span<T> Span => chunk.GetSpan<T>();

        /// <summary>
        /// 使用默认共享池构造实例。
        /// </summary>
        public UnmanagedArchetTypeChunk(ArchetypeChunkProvider<T> provider) : this(provider, ChunkPool.Share)
        {
            _chunkPool = ChunkPool.Share;
            chunk = default!;
        }

        /// <summary>
        /// 使用指定的 <see cref="ChunkPool"/> 构造实例（用于测试或定制池）。
        /// </summary>
        public UnmanagedArchetTypeChunk(ArchetypeChunkProvider<T> provider, ChunkPool chunkPool) : base(provider)
        {
            _chunkPool = chunkPool;
            chunk = default!;
        }

        /// <summary>
        /// 将当前块中指定全局索引的组件复制到目标块的指定新索引。
        /// 返回是否复制成功。若目标块类型不匹配或索引不在块内则返回 false。
        /// </summary>
        public override bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex)
        {
            if (newArchetypeChunk is not ArchetypeChunk<T> aChunk ||
                !TryWithinChunk(globalIndex, out var localIndex) ||
                !aChunk.TryWithinChunk(newGlobalIndex, out var newLocalIndex))
                return false;

            ref T value = ref GetComponentRef(localIndex);
            aChunk.SetComponent(newLocalIndex, value);
            return true;
        }

        /// <summary>
        /// 受保护的初始化实现：从池中租用底层 aChunk 并对其调用 Initialize&lt;T1&gt;。
        /// 如果已初始化则直接返回。
        /// </summary>
        protected override void InitializeProtected()
        {
            if (chunk != null)
                throw new InvalidOperationException("当前原型块已初始化,无需重复初始化。");

            chunk = _chunkPool.Rent(Capacity);
            chunk.Initialize<T>();
        }

        /// <summary>
        /// 交换块内两个本地索引处的元素（委托给底层 aChunk 实现）。
        /// </summary>
        public override void Swap(int localIndexA, int localIndexB)
        {
            chunk.Swap(localIndexA, localIndexB);
        }

        /// <summary>
        /// 将不安全来源内存拷贝到块内（高性能内存拷贝），委托到底层 aChunk 实现。
        /// </summary>
        public override void CopiedUnsafe(int localIndex, nint soure, int count)
        {
            chunk.CopiedUnsafe(localIndex, soure, count);
        }

        /// <summary>
        /// 将底层 aChunk 归还到池中并清理本对象状态（将 aChunk 置为 null 并重置 Count）。
        /// </summary>
        protected override void ReturnChunkToPool()
        {
            if (chunk != null)
            {
                _chunkPool.Return(chunk);
                chunk = null!;
                Count = 0;
            }
        }

        /// <summary>
        /// 在未初始化时抛出异常的统一处理方法。
        /// </summary>
        protected override void ThrowNotInitialize()
        {
            if (chunk == null)
            {
                throw new InvalidOperationException("当前原型块未初始化,请先调用 Initialize。");
            }
        }

        /// <summary>
        /// 设置指定索引处的组件值（委托给底层 aChunk 的写入方法）。
        /// </summary>
        public override void SetComponent(int index, T value) => chunk.Write(index, value);

        /// <summary>
        /// 获取指定索引处的组件副本（委托给底层 aChunk 的读取方法）。
        /// </summary>
        public override T GetComponent(int index) => chunk.ReadUnsafe<T>(index);

        /// <summary>
        /// 以引用形式返回指定索引处的组件，允许直接修改底层数据（委托给 aChunk 的引用获取方法）。
        /// </summary>
        public override ref T GetComponentRef(int index) => ref chunk.GetElementRef<T>(index);

        ///<inheritdoc/>
        protected override void RemoveAtProtected(int localIndex)
        {
            int last = Count - 1;
            ref T lastElement = ref chunk.GetElementRef<T>(last);
            if (localIndex != last)
            {
                ref T element = ref chunk.GetElementRef<T>(localIndex);
                element = lastElement;
            }
            lastElement = default!;
        }
    }
}