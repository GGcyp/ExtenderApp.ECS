using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExtenderApp.ECS.Native
{
    /// <summary>
    /// 简单的受限 NativeArray 实现（托管侧的轻量非托管数组）
    /// - 约束 T1 为 unmanaged，以便安全地在非托管内存中存储
    /// - 提供索引器、Span 视图、Fill/CopyFrom、以及直接指针访问
    /// - 需显式 Dispose 以释放非托管内存
    /// </summary>
    [DebuggerDisplay("NativeArray<{typeof(T1).Name}> Length = {_length}")]
    public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
    {
        private void* _ptr;
        private bool _disposed;

        /// <summary>
        /// 元素数量
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsCreated => !_disposed && _ptr != null;

        /// <summary>
        /// 创建指定长度的 NativeArray（元素未初始化）
        /// </summary>
        public NativeArray(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            Length = length;
            _disposed = false;

            nuint size = (nuint)Unsafe.SizeOf<T>() * (nuint)length;
            _ptr = NativeMemory.Alloc(size);
        }

        /// <summary>
        /// 索引器（读/写）。
        /// </summary>
        public ref T this[int index]
        {
            get
            {
                EnsureNotDisposed();
                if ((uint)index >= (uint)Length)
                    throw new IndexOutOfRangeException();
                return ref Unsafe.Add(ref Unsafe.AsRef<T>(_ptr), index);
            }
        }

        /// <summary>
        /// 获取 Span 视图，便于与 API 交互。
        /// </summary>
        public Span<T> AsSpan()
        {
            EnsureNotDisposed();
            if (_ptr == null) return Span<T>.Empty;
            return new Span<T>(_ptr, Length);
        }

        /// <summary>
        /// 获取只读 Span。
        /// </summary>
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            EnsureNotDisposed();
            if (_ptr == null) return ReadOnlySpan<T>.Empty;
            return new ReadOnlySpan<T>(_ptr, Length);
        }

        /// <summary>
        /// 将所有元素设置为指定值。
        /// </summary>
        public void Fill(T value)
        {
            var span = AsSpan();
            span.Fill(value);
        }

        /// <summary>
        /// 从只读 Span 复制数据（长度必须匹配或源长度小于目标长度）。
        /// </summary>
        public void CopyTo(Span<T> dst)
        {
            EnsureNotDisposed();
            AsSpan().CopyTo(dst);
        }

        /// <summary>
        /// 返回底层非托管指针（供 unsafe 操作使用）。
        /// </summary>
        public void* GetUnsafePtr()
        {
            EnsureNotDisposed();
            return _ptr;
        }

        /// <summary>
        /// 释放底层非托管内存。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            if (_ptr != null)
            {
                NativeMemory.Free(_ptr);
                _ptr = null;
            }
            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("NativeArray");
        }
    }
}