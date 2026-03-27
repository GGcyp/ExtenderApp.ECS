using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExtenderApp.Contracts;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 非托管内存块（chunk）封装。
    ///
    /// 说明：
    /// - 在构造函数中分配一段固定大小的非托管内存（当前为 DefaultChunkSize）;
    /// - 通过 Initialize 设置元素的字节大小与可容纳的元素数量（ElementSize / Capacity），但不重新分配内存;
    /// - 上层可以通过 GetElementPtr/GetRawPointer/GetSpan/Read/TryWrite 等方法直接读写底层内存;
    /// - 设计限制：单个元素最大为 MaxElementSize 字节（默认为 128），以便在交换时使用 stackalloc 临时缓冲，避免堆分配;
    /// - 该类型管理非托管资源并在 DisposeUnmanagedResources 中释放对应内存，应通过引用类型使用并避免复制（因此为 class）。
    /// </summary>
    internal sealed unsafe class Chunk : DisposableObject
    {
        /// <summary>
        /// 指向分配的非托管内存的基指针。
        /// </summary>
        private nint chunkPtr;

        /// <summary>
        /// 获取或设置初始化的容量（元素数量）。
        /// </summary>
        public int Capacity { get; internal set; }

        /// <summary>
        /// 每个元素的字节大小（由 Initialize 设置）。
        /// </summary>
        public int ElementSize { get; private set; }

        /// <summary>
        /// 构造函数：立即在非托管堆上分配一段固定大小的内存。 Initialize 之后请调用 Read/TryWrite/TryCopyToAndRemove/Swap 等方法来访问存储。
        /// </summary>
        public Chunk()
        {
            Capacity = 0;
            ElementSize = 0;
        }

        /// <summary>
        /// 初始化块以容纳指定数量的 T 型元素。 注意：T 不需要是 unmanaged 编译时限定，但应保证其实际布局大小等于 ElementSize 的期望。 Initialize 仅检查并设置 ElementSize/Capacity，不会清理或重新分配内存。
        /// </summary>
        /// <typeparam name="T">元素类型（建议为值类型、大小确定的 struct）。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize<T>()
        {
            ThrowIfDisposed();

            ElementSize = Marshal.SizeOf<T>();
            chunkPtr = (nint)NativeMemory.Alloc((nuint)Capacity * (uint)ElementSize);
        }

        #region Operations

        /// <summary>
        /// 将值按类型写入指定索引位置（包含边界与初始化检查）。 上层应确保传入的 T 实例大小与初始化时的 ElementSize 匹配（否则可能导致语义错误）。
        /// </summary>
        /// <typeparam name="T">要写入的类型（值类型）。</typeparam>
        /// <param name="index">元素索引（0 基）。</param>
        /// <param name="value">要写入的值引用。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(int index, in T value)
        {
            var dest = (T*)GetElementPtr(index);
            *dest = value;
        }

        /// <summary>
        /// 不做边界/初始化检查的写入版本，适合在已确保安全的内部热路径使用。 直接在底层内存上写入未对齐的结构体字节。
        /// </summary>
        /// <typeparam name="T">要写入的类型。</typeparam>
        /// <param name="index">元素索引（0 基）。</param>
        /// <param name="value">要写入的值引用。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUnsafe<T>(int index, in T value)
        {
            ref byte p = ref Unsafe.AsRef<byte>((void*)(chunkPtr + index * ElementSize));
            Unsafe.WriteUnaligned(ref p, value);
        }

        /// <summary>
        /// 读取指定索引的值（含检查）。
        /// </summary>
        /// <typeparam name="T">要读取的类型。</typeparam>
        /// <param name="index">元素索引（0 基）。</param>
        /// <returns>读取的值实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(int index)
        {
            var src = (T*)GetElementPtr(index);
            return *src;
        }

        /// <summary>
        /// 不做边界/初始化检查的读取版本，适合在已确保安全的内部热路径使用。 直接从底层内存按字节读取未对齐的数据并转换为目标类型。
        /// </summary>
        /// <typeparam name="T">要读取的类型。</typeparam>
        /// <param name="index">元素索引（0 基）。</param>
        /// <returns>读取的值实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadUnsafe<T>(int index)
        {
            ref byte p = ref Unsafe.AsRef<byte>((void*)(chunkPtr + index * ElementSize));
            return Unsafe.ReadUnaligned<T>(ref p);
        }

        /// <summary>
        /// 获取指定索引处元素的引用（包含边界与初始化检查）。 返回对内存中元素的 `ref`，可用于在不产生拷贝的情况下直接读取或写入该元素。 注意：在持有返回的引用期间，不要执行会改变底层内存布局的结构性操作（例如释放或重新初始化 chunk），否则会导致悬挂引用或未定义行为。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="index">元素索引（0-based）。</param>
        /// <returns>指向元素的引用（ref T）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetElementRef<T>(int index)
        {
            var src = (T*)GetElementPtr(index);
            return ref *src;
        }

        /// <summary>
        /// 在不进行边界和初始化检查的情况下返回指定索引处元素的引用（用于内部热路径以提高性能）。 该方法直接将底层字节视为目标类型并返回其引用，调用方必须确保 `chunkIndex` 在有效范围内且 `ElementSize` 与目标类型匹配。 严格禁止在持有此引用期间对 chunk 进行释放、归还或任何可能移动/重分配底层内存的操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="index">元素索引（0-based）。</param>
        /// <returns>指向元素的引用（ref T），基于未检查的内存视图。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ReadUnsafeRef<T>(int index)
        {
            ref byte p = ref Unsafe.AsRef<byte>((void*)(chunkPtr + index * ElementSize));
            return ref Unsafe.As<byte, T>(ref p);
        }

        /// <summary>
        /// 将 srcIndex 的字节数据复制到 dstIndex（字节级复制），包含检查。 该操作直接调用内存拷贝，不在堆上分配临时缓冲。
        /// </summary>
        /// <param name="srcIndex">源索引。</param>
        /// <param name="dstIndex">目标索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int srcIndex, int dstIndex)
        {
            EnsureInitialized();
            EnsureIndexInRange(srcIndex);
            EnsureIndexInRange(dstIndex);

            void* src = (void*)GetElementPtrUnsafe(srcIndex);
            void* dst = (void*)GetElementPtrUnsafe(dstIndex);
            Buffer.MemoryCopy(src, dst, ElementSize, ElementSize);
        }

        /// <summary>
        /// 将从 srcIndex 开始的 srcCount 个元素复制到以 dstIndex 开始的目标范围（同一 chunk 内部复制），包含边界检查。
        /// </summary>
        /// <param name="srcIndex">源起始索引。</param>
        /// <param name="srcCount">要复制的元素数量。</param>
        /// <param name="dstIndex">目标起始索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int srcIndex, int srcCount, int dstIndex)
        {
            EnsureInitialized();
            EnsureIndexInRange(srcIndex);
            EnsureIndexInRange(dstIndex);
            if (srcIndex + srcCount > Capacity || dstIndex + srcCount > Capacity)
                throw new IndexOutOfRangeException("复制范围超出容量。");

            void* src = (void*)GetElementPtrUnsafe(srcIndex);
            void* dst = (void*)GetElementPtrUnsafe(dstIndex);
            Buffer.MemoryCopy(src, dst, srcCount * ElementSize, srcCount * ElementSize);
        }

        /// <summary>
        /// 将从 srcIndex 开始的 srcCount 个元素复制到目标 chunk 的指定位置（跨 chunk 复制），包含类型大小与范围检查。
        /// </summary>
        /// <param name="srcIndex">源起始索引（在当前 chunk 中）。</param>
        /// <param name="srcCount">源元素数量。</param>
        /// <param name="dstChunk">目标 chunk 实例。</param>
        /// <param name="dstIndex">目标起始索引（在目标 chunk 中）。</param>
        /// <param name="dstCount">目标 chunk 中可用的目标槽数（用于边界检查）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int srcIndex, int srcCount, Chunk dstChunk, int dstIndex, int dstCount)
        {
            EnsureInitialized();
            if (dstChunk.ElementSize != ElementSize)
                throw new InvalidOperationException("目标块的元素大小与源块不匹配。");

            EnsureIndexInRange(srcIndex);
            dstChunk.EnsureIndexInRange(dstIndex);
            if (srcIndex + srcCount > Capacity || dstIndex + dstCount > dstChunk.Capacity)
                throw new IndexOutOfRangeException("复制范围超出容量。");

            void* src = (void*)GetElementPtrUnsafe(srcIndex);
            void* dst = (void*)dstChunk.GetElementPtrUnsafe(dstIndex);
            Buffer.MemoryCopy(src, dst, srcCount * ElementSize, srcCount * ElementSize);
        }

        /// <summary>
        /// 不做检查的字节复制版本，直接拷贝 srcIndex 到 dstIndex（同一 chunk 内部）。 适用于已知安全的内部热路径以减少检查开销。
        /// </summary>
        /// <param name="srcIndex">源索引。</param>
        /// <param name="dstIndex">目标索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToUnsafe(int srcIndex, int dstIndex)
        {
            // base ptr
            byte* basePtr = (byte*)chunkPtr;
            ref byte src = ref Unsafe.AsRef<byte>(basePtr + srcIndex * ElementSize);
            ref byte dst = ref Unsafe.AsRef<byte>(basePtr + dstIndex * ElementSize);

            Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)ElementSize);
        }

        /// <summary>
        /// 不做检查的字节复制版本（同一 chunk 内部），复制 srcCount 个元素到 dstIndex 开始的位置。 适用于已知安全的内部热路径以减少检查开销。
        /// </summary>
        /// <param name="srcIndex">源起始索引。</param>
        /// <param name="srcCount">要复制的元素数量。</param>
        /// <param name="dstIndex">目标起始索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToUnsafe(int srcIndex, int srcCount, int dstIndex)
        {
            byte* basePtr = (byte*)chunkPtr;
            ref byte src = ref Unsafe.AsRef<byte>(basePtr + srcIndex * ElementSize);
            ref byte dst = ref Unsafe.AsRef<byte>(basePtr + dstIndex * ElementSize);

            Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)(srcCount * ElementSize));
        }

        /// <summary>
        /// 不做检查的跨 chunk 字节复制版本，将 srcCount 个元素从当前 chunk 的 srcIndex 复制到 dstChunk 的 dstIndex。 适用于已知安全的内部热路径以减少检查开销，但必须保证目标 chunk 的 ElementSize 与当前一致。
        /// </summary>
        /// <param name="srcIndex">源起始索引（当前 chunk）。</param>
        /// <param name="srcCount">要复制的元素数量。</param>
        /// <param name="dstChunk">目标 chunk 实例。</param>
        /// <param name="dstIndex">目标起始索引（目标 chunk）。</param>
        /// <param name="dstCount">目标可用槽数量（未检查，仅用于语义对齐）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToUnsafe(int srcIndex, int srcCount, Chunk dstChunk, int dstIndex, int dstCount)
        {
            ref byte src = ref Unsafe.AsRef<byte>((byte*)chunkPtr + srcIndex * ElementSize);
            ref byte dst = ref Unsafe.AsRef<byte>((byte*)dstChunk.chunkPtr + dstIndex * ElementSize);

            Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)(srcCount * ElementSize));
        }

        /// <summary>
        /// 交换 left 与 right 两个槽的字节数据（含检查）。 为避免堆分配，使用 stackalloc 临时缓冲；因此单元素大小必须不超过 MaxElementSize。
        /// </summary>
        /// <param name="left">左索引。</param>
        /// <param name="right">右索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Swap(int left, int right)
        {
            if (left == right)
                return true;

            EnsureInitialized();
            EnsureIndexInRange(left);
            EnsureIndexInRange(right);

            byte* a = (byte*)GetElementPtrUnsafe(left);
            byte* b = (byte*)GetElementPtrUnsafe(right);
            int size = ElementSize;

            // ElementSize <= MaxElementSize (128) -> 总是可以使用 stackalloc，不产生堆分配
            byte* tmp = stackalloc byte[size];
            Buffer.MemoryCopy(a, tmp, size, size);
            Buffer.MemoryCopy(b, a, size, size);
            Buffer.MemoryCopy(tmp, b, size, size);

            return true;
        }

        /// <summary>
        /// 非检查版本的交换，用于已知安全的内部路径以获得更少的检查开销。 该方法使用一个固定大小的 stackalloc 缓冲区（MaxElementSize）并复制实际 ElementSize 的字节。
        /// </summary>
        /// <param name="left">左索引。</param>
        /// <param name="right">右索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SwapUnsafe(int left, int right)
        {
            if (left == right) return true;
            byte* basePtr = (byte*)chunkPtr;
            ref byte a = ref Unsafe.AsRef<byte>(basePtr + left * ElementSize);
            ref byte b = ref Unsafe.AsRef<byte>(basePtr + right * ElementSize);

            Span<byte> tmp = stackalloc byte[ElementSize];
            ref byte t = ref MemoryMarshal.GetReference(tmp.Slice(0, ElementSize));

            Unsafe.CopyBlockUnaligned(ref t, ref a, (uint)ElementSize);
            Unsafe.CopyBlockUnaligned(ref a, ref b, (uint)ElementSize);
            Unsafe.CopyBlockUnaligned(ref b, ref t, (uint)ElementSize);
            return true;
        }

        #endregion Operations

        #region Get

        /// <summary>
        /// 获取指定类型的 Span&lt;T&gt; 覆盖整个已初始化容量。 返回的 Span 长度为 Capacity。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>覆盖整个块的 Span&lt;T&gt;。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetSpan<T>()
        {
            ThrowIfDisposed();
            EnsureInitialized();
            return new Span<T>((void*)chunkPtr, Capacity);
        }

        /// <summary>
        /// 获取指定类型的 Span&lt;T&gt; 的非检查版本，适合在已知安全的内部路径使用以获得更少的检查开销。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>覆盖整个块的 Span&lt;T&gt;（不做初始化检查）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetSpanUnsafe<T>()
        {
            return new Span<T>((void*)chunkPtr, Capacity);
        }

        /// <summary>
        /// 返回底层原始指针（用于高级互操作）。
        /// </summary>
        /// <returns>指向块起始处的 IntPtr 原始指针。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetRawPointer()
        {
            ThrowIfDisposed();
            EnsureInitialized();
            return chunkPtr;
        }

        /// <summary>
        /// 返回指定元素索引的指针（IntPtr），上层可将其转换为相应类型指针并直接读写。 包含初始化与索引范围检查。
        /// </summary>
        /// <param name="index">元素索引（0 基）。</param>
        /// <returns>对应元素的指针（IntPtr）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetElementPtr(int index)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            EnsureIndexInRange(index);
            return IntPtr.Add(chunkPtr, index * ElementSize);
        }

        /// <summary>
        /// 不做检查的指针获取版本（用于内部热路径）。 返回指定索引的元素指针（不进行范围和初始化检查）。
        /// </summary>
        /// <param name="index">元素索引（0 基）。</param>
        /// <returns>元素在非托管内存中的指针（IntPtr）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IntPtr GetElementPtrUnsafe(int index)
            => IntPtr.Add(chunkPtr, index * ElementSize);

        #endregion Get

        #region Ensure

        /// <summary>
        /// 确保当前 chunk 已分配且已通过 Initialize 初始化 ElementSize 与 Capacity，否则抛出异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (chunkPtr == IntPtr.Zero)
                throw new InvalidOperationException("chunk 内存未分配或已释放。请先使用构造函数创建块并调用 Initialize 设置元素大小与容量。");

            if (ElementSize == 0 || Capacity == 0)
                throw new InvalidOperationException("chunk 未初始化元素大小或容量。请先调用 Initialize。");
        }

        /// <summary>
        /// 确保给定索引在已初始化的容量范围 [0, Capacity) 内，否则抛出 IndexOutOfRangeException。
        /// </summary>
        /// <param name="index">要检查的元素索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureIndexInRange(int index)
        {
            if (index < 0 || index >= Capacity)
                throw new IndexOutOfRangeException(nameof(index));
        }

        #endregion Ensure

        /// <summary>
        /// 将外部源内存复制到当前块的起始位置。 该方法直接调用内存拷贝，不在堆上分配临时缓冲。
        /// </summary>
        /// <param name="offset">目标偏移（位数）。</param>
        /// <param name="source">源内存指针。</param>
        /// <param name="count">要复制的字节数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopiedUnsafe(int offset, nint source, int count)
        {
            NativeMemory.Copy((void*)source, (void*)IntPtr.Add(chunkPtr, offset * ElementSize), (nuint)count);
        }

        public override string ToString() => $"chunk: Capacity={Capacity}, ElementSize={ElementSize}, IsDisposed={IsDisposed}";

        /// <summary>
        /// 释放非托管内存。
        /// </summary>
        protected override void DisposeUnmanagedResources()
        {
            if (chunkPtr != IntPtr.Zero)
            {
                NativeMemory.Free((void*)chunkPtr);
                chunkPtr = IntPtr.Zero;
            }

            ElementSize = 0;
            base.DisposeUnmanagedResources();
        }
    }
}