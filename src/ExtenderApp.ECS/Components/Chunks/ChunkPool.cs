using System.Collections.Concurrent;
using ExtenderApp.Contracts;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 非托管内存块（Chunk）的简单对象池。
    ///
    /// 设计目标：
    /// - 复用 Chunk 实例以减少频繁分配/释放非托管内存的成本；
    /// - 使用一个快速的单元素槽（lastChunk）作为无锁 fast-path；
    /// - 使用一个线程安全的 ConcurrentQueue 作为备用缓冲区，容量受 MaxPoolSize 限制；
    /// - 当池已满时，归还的 Chunk 会被立即释放（Dispose），防止内存无限增长。
    /// </summary>
    internal sealed class ChunkPool : DisposableObject
    {
        /// <summary>
        /// 全局共享的默认池实例（单例便捷访问）。
        /// </summary>
        public static readonly ChunkPool Share = new();

        /// <summary>
        /// 池中允许保留的最大块数量（包含 lastChunk 与 queue 中的项）。 超过该数量后，归还的 Chunk 将被释放而不是入池。
        /// </summary>
        private const int MaxPoolSize = 10;

        /// <summary>
        /// 后备的线程安全队列，存储空闲的 Chunk 实例。 延迟初始化以减少不必要分配。
        /// </summary>
        private ConcurrentQueue<Chunk> queue;

        /// <summary>
        /// 一个无锁快速路径的单槽缓存（可由任一线程原子交换），用于减少入/取操作的开销。 当有新请求时先尝试从该槽获取；归还时优先放入该槽。
        /// </summary>
        private Chunk? lastChunk;

        /// <summary>
        /// 当前池中近似的可用 Chunk 数量（包括 lastChunk 与队列中的数量）。 仅供监控/调试使用，不保证严格精确性（并发条件下为近似值）。
        /// </summary>
        public int Count => (lastChunk != null ? 1 : 0) + (queue?.Count ?? 0);

        /// <summary>
        /// 构造函数：不立即分配任何 Chunk，仅准备延迟初始化的队列字段。
        /// </summary>
        public ChunkPool()
        {
            queue = default!;
        }

        /// <summary>
        /// 从池中租用一个 Chunk。 行为：
        /// 1. 尝试从快速槽（lastChunk）无锁取出一个实例；
        /// 2. 若无，则确保队列存在并从队列中尝试弹出；
        /// 3. 若队列也没有可用实例，则创建新的 Chunk 并返回（此时 lastChunk 指向新创建的实例以便下一次快速获得）。
        /// </summary>
        /// <returns>可用的 Chunk 实例（调用者负责 Initialize/使用/最终归还）。</returns>
        public Chunk Rent(int capacity)
        {
            ThrowIfDisposed();
            Chunk? chunk = Interlocked.Exchange(ref lastChunk, null);
            if (chunk != null)
            {
                chunk.Capacity = capacity;
                return chunk;
            }

            if (queue != null && queue!.TryDequeue(out chunk))
            {
                chunk.Capacity = capacity;
                return chunk;
            }

            return new(capacity);
        }

        /// <summary>
        /// 将一个已不再使用的 Chunk 归还到池中。 行为：
        /// 1. 优先尝试将 Chunk 放入快速槽（lastChunk）；若该槽为空则放入并返回；
        /// 2. 若快速槽已被占用，则根据池的当前大小决定：若池已达上限则直接释放该 Chunk，否则将其入队作为后备。
        /// </summary>
        /// <param name="chunk">要归还的 Chunk 实例。</param>
        public void Return(Chunk chunk)
        {
            ThrowIfDisposed();
            if (chunk == null)
                return;

            if (Interlocked.CompareExchange(ref lastChunk, chunk, null) == null)
            {
                return;
            }

            if (Count >= MaxPoolSize)
            {
                chunk.Dispose();
                return;
            }

            if (queue == null)
                queue = new();
            queue!.Enqueue(chunk);
        }

        /// <summary>
        /// 释放池中所有资源（若存在）。 该方法会释放队列中的所有 Chunk 实例并清理队列引用。
        /// </summary>
        protected override void DisposeUnmanagedResources()
        {
            if (queue != null)
            {
                while (queue.TryDequeue(out var c))
                {
                    c.Dispose();
                }

                queue = null!;
            }

            if (lastChunk != null)
            {
                lastChunk.Dispose();
                lastChunk = null;
            }

            base.DisposeUnmanagedResources();
        }
    }
}