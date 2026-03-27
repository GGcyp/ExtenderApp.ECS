using System.Buffers;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 托管类型的 ArchetypeChunk 实现，用于存储托管引用或托管结构体的组件数据。
    ///
    /// 说明：此实现使用托管数组作为底层存储并可从 <see cref="ArrayPool{T}"/> 获取缓冲区以减少分配开销。
    /// 它提供对单个组件的读写与引用访问，并实现块内交换与跨块复制等操作。
    /// </summary>
    internal sealed class ManagedArchetTypeChunk<T> : ArchetypeChunk<T>
    {
        /// <summary>
        /// 用于租用/归还数组的数组池引用（默认使用 <see cref="ArrayPool{T}.Shared"/>）。
        /// </summary>
        private readonly ArrayPool<T> _pool;

        /// <summary>
        /// 承载组件数据的托管数组。初始为空数组，实际容量在初始化时由 <see cref="InitializeProtected"/> 分配。
        /// </summary>
        private T[] _components;

        /// <summary>
        /// 返回当前块内组件的连续内存视图（Span&lt;T&gt;）。
        /// 视图直接基于内部数组，访问需保证块已初始化并且索引在有效范围内。
        /// </summary>
        public override Span<T> Span => _components.AsSpan();

        /// <summary>
        /// 使用共享数组池构造实例。
        /// </summary>
        public ManagedArchetTypeChunk(ArchetypeChunkProvider<T> provider) : this(provider, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// 构造函数：允许指定用于分配数组的 <see cref="ArrayPool{T}"/>。
        /// 初始状态下内部数组为空数组，实际缓冲区在初始化时从池租用。
        /// </summary>
        public ManagedArchetTypeChunk(ArchetypeChunkProvider<T> provider, ArrayPool<T> pool) : base(provider)
        {
            _components = default!;
            _pool = pool;
        }

        /// <summary>
        /// 将外部内存的不安全复制（native 来源）复制到本块指定位置，用于高性能拷贝场景。
        /// 当前尚未实现，调用将抛出 <see cref="NotImplementedException"/>。
        /// </summary>
        public override void CopiedUnsafe(int localIndex, nint soure, int count) => throw new NotImplementedException();

        /// <summary>
        /// 获取指定本地索引处的组件副本（按值返回）。
        /// </summary>
        public override T GetComponent(int index) => Span[index];

        /// <summary>
        /// 以引用形式返回指定本地索引处的组件，允许对内部元素进行就地修改。
        /// </summary>
        public override ref T GetComponentRef(int index) => ref _components[index];

        /// <summary>
        /// 将底层数组归还到数组池并清理内部状态（当对象被回收或需要释放资源时调用）。
        /// 当前实现为空，按需可调用 <see cref="ArrayPool{T}.Return(T[])"/> 以释放缓冲区。
        /// </summary>
        public override void ReturnChunkToPool()
        {
            _pool.Return(_components, true);
            _components = default!;
        }

        /// <summary>
        /// 设置指定本地索引处的组件值。
        /// </summary>
        public override void SetComponent(int index, T value) => Span[index] = value;

        /// <summary>
        /// 在块内交换两个本地索引处的组件值。
        /// 已实现为通过元组交换数组元素。
        /// </summary>
        public override void Swap(int localIndexA, int localIndexB)
        {
            (Span[localIndexA], Span[localIndexB]) = (Span[localIndexB], Span[localIndexA]);
        }

        /// <summary>
        /// 尝试将当前块中 globalIndex 对应的组件复制到目标 ArchetypeChunk 的 newGlobalIndex。
        /// 当目标块类型匹配且对应索引均在各自块内时执行复制并返回 true，否则返回 false。
        /// </summary>
        public override bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex)
        {
            if (newArchetypeChunk is not ArchetypeChunk<T> chunk ||
                !TryWithinChunk(globalIndex, out var localIndex) ||
                !chunk.TryWithinChunk(newGlobalIndex, out var newLocalIndex))
                return false;

            T value = GetComponentRef(localIndex);
            chunk.SetComponent(newLocalIndex, value);
            return true;
        }

        /// <summary>
        /// 受保护的初始化实现：当内部数组尚未分配时从数组池租用足够容量的数组并赋值给内部字段。
        /// </summary>
        protected override void InitializeProtected()
        {
            if (_components != null)
                return;

            _components = _pool.Rent(Capacity);
        }

        /// <summary>
        /// 未初始化时的统一异常处理。当前实现以内部数组为准进行检查并抛出异常。
        /// </summary>
        protected override void ThrowNotInitialize()
        {
            if (_components == null)
                throw new InvalidOperationException("当前原型块未初始化,请先调用 Initialize。");
        }

        ///<inheritdoc/>
        protected override void RemoveAtProtected(int localIndex)
        {
            int last = Count - 1;
            if (localIndex != last)
                Span[localIndex] = Span[last];
            else
                Span[localIndex] = default!;
        }
    }
}