using System.Runtime.CompilerServices;

namespace ExtenderApp.Contracts
{
    /// <summary>
    /// 一个实现了IDisposable和IAsyncDisposable接口的抽象类，用于管理可释放资源。
    /// </summary>
    public abstract class DisposableObject : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 用于标记对象是否已被释放。
        /// </summary>
        [NonSerialized]
        private volatile int _disposed;

        /// <summary>
        /// 获取一个值，指示对象是否已被释放。
        /// </summary>
        public bool IsDisposed => _disposed == 1;

        /// <summary>
        /// 析构函数，用于在对象被垃圾回收时释放非托管资源。
        /// </summary>
        ~DisposableObject()
        {
            Dispose(false);
        }

        /// <summary>
        /// 如果对象已被释放，则抛出ObjectDisposedException异常。
        /// </summary>
        /// <exception cref="ObjectDisposedException">如果对象已被释放，则抛出此异常。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// 释放或重置由DisposableObject使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                Dispose(true); // 主动释放：托管+非托管资源
            }
            finally
            {
                GC.SuppressFinalize(this); // 抑制析构函数，避免重复释放
            }
        }

        /// <summary>
        /// 释放或重置由DisposableObject使用的资源。
        /// </summary>
        /// <param name="disposing">指示是否应释放托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // 主动释放：释放托管资源
                    DisposeManagedResources();
                }
            }
            finally
            {
                // 无论托管资源是否释放成功，都必须释放非托管资源
                DisposeUnmanagedResources();
            }
        }

        /// <summary>
        /// 异步释放或重置由DisposableObject使用的所有资源。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false); // 同步释放非托管资源
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 提供异步释放的核心实现。
        /// </summary>
        protected async ValueTask DisposeAsyncCore()
        {
            await DisposeAsyncManagedResources();
        }

        /// <summary>
        /// 子类应重写此方法以异步释放托管资源。
        /// </summary>
        protected virtual ValueTask DisposeAsyncManagedResources()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 子类应重写此方法以同步释放托管资源。
        /// </summary>
        protected virtual void DisposeManagedResources()
        {
            // 子类实现具体的托管资源释放逻辑
        }

        /// <summary>
        /// 子类应重写此方法以释放非托管资源。
        /// </summary>
        protected virtual void DisposeUnmanagedResources()
        {
            // 子类实现具体的非托管资源释放逻辑
        }
    }
}