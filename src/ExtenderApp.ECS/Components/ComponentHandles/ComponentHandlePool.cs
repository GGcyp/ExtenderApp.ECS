using System.Collections.Concurrent;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// ComponentHandle 的简单对象池。 设计上采用一个快速单槽与一个并发队列，减少频繁分配开销。
    /// </summary>
    internal sealed class ComponentHandlePool
    {
        /// <summary>
        /// 全局共享池实例。
        /// </summary>
        public static readonly ComponentHandlePool Share = new();

        /// <summary>
        /// 池中允许保留的最大对象数量（包含快速槽与队列）。
        /// </summary>
        private const int MaxPoolSize = 256;

        /// <summary>
        /// 后备线程安全队列。
        /// </summary>
        private ConcurrentQueue<ComponentHandle>? _queue;

        /// <summary>
        /// 无锁快速槽。
        /// </summary>
        private ComponentHandle? _lastHandle;

        /// <summary>
        /// 当前池中近似可用数量。
        /// </summary>
        public int Count => (_lastHandle != null ? 1 : 0) + (_queue?.Count ?? 0);

        /// <summary>
        /// 租用一个 ComponentHandle。
        /// </summary>
        /// <returns>可用的 ComponentHandle 实例。</returns>
        public ComponentHandle Rent()
        {
            var handle = Interlocked.Exchange(ref _lastHandle, null);
            if (handle != null)
            {
                return handle;
            }

            if (_queue != null && _queue.TryDequeue(out handle))
            {
                return handle;
            }

            return new(this);
        }

        /// <summary>
        /// 归还一个 ComponentHandle 到池中。
        /// </summary>
        /// <param name="handle">待归还实例。</param>
        public void Return(ComponentHandle? handle)
        {
            if (handle == null)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _lastHandle, handle, null) == null)
            {
                return;
            }

            if (Count >= MaxPoolSize)
            {
                return;
            }

            _queue ??= new ConcurrentQueue<ComponentHandle>();
            _queue.Enqueue(handle);
        }
    }
}