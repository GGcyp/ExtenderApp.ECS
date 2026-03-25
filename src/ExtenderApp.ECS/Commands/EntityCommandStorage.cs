using System.Diagnostics.CodeAnalysis;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 命令存储与段管理器。
    /// 
    /// 责任：
    /// - 为命令缓冲提供段（CommandBufferChunk）分配与保留接口；
    /// - 管理已封存（sealed）的段链表，供回放器按时间顺序弹出并回放；
    /// - 为虚拟实体（由并发写入使用的临时实体 id，负数）提供 ID 生成；
    /// - 提供在回放完成后重置 current 段游标以复用缓冲区的能力。
    /// 
    /// 线程语义：
    /// - 写入端（工作线程）通过 GetBufferChunk/TryReserve 向 currentChunk 并发写入；
    /// - 当写入端发现 currentChunk 空间不足时，会创建新段并通过原子交换切换到新段，
    ///   并将旧段推入封存链表；
    /// - 读取端（回放器）在主线程调用 TryPopChunk 获取待回放的段，回放完成后调用 ResetCurrentChunkAfterRead
    ///   来复用 currentChunk（仅在主线程上调用）。
    /// </summary>
    internal sealed unsafe class EntityCommandStorage : IDisposable
    {
        /// <summary>
        /// 默认段大小（字节）。注意：过小会导致频繁分段，过大浪费内存。
        /// </summary>
        private const int DefaultSegmentSize = 16 * 1024; // 32KB

        /// <summary>
        /// 链表节点：将已封存的 CommandBufferChunk 按链表维护，供回放器弹出。
        /// </summary>
        private sealed class ChunkNode
        {
            public readonly CommandBufferChunk Chunk;
            public ChunkNode? Next;

            public ChunkNode(CommandBufferChunk chunk)
            {
                Chunk = chunk;
            }
        }

        // 封存链表头（只有回放/写入切换路径会修改）
        private ChunkNode? _sealedHead;

        /// <summary>
        /// 标记当前 currentChunk 在一次读取周期中是否已经被弹出。
        /// 通过原子整型控制，避免多线程重复弹出同一 currentChunk。
        /// </summary>
        private int _currentPopped;

        // 用于产生虚拟实体 id（递减）。并发写入使用负 id 表示虚拟实体。
        private int virtualNumber;

        /// <summary>
        /// 当前虚拟 id 计数器，仅作观察用途（调试/诊断）。
        /// </summary>
        public int VirtualNumber => virtualNumber;

        // 当前段（供写入线程使用），通过原子方式替换以切换到新段。
        private CommandBufferChunk currentChunk;
        internal CommandBufferChunk CurrentChunk => Volatile.Read(ref currentChunk);

        /// <summary>
        /// 创建新的 EntityCommandStorage 并分配首段。
        /// </summary>
        public EntityCommandStorage()
        {
            currentChunk = CreateSegment(DefaultSegmentSize);
            _currentPopped = 0;
        }

        /// <summary>
        /// 为并发写入生成一个虚拟实体（负数 id）。
        /// 写入端可以使用该虚拟实体作为命令目标，回放阶段会在主线程上将其解析为真实实体。
        /// </summary>
        public Entity CreateEntity() => Entity.GetVirtualEntity(Interlocked.Decrement(ref virtualNumber));

        /// <summary>
        /// 创建一个指定容量的新段实例（内部工厂）。
        /// </summary>
        private static CommandBufferChunk CreateSegment(int capacity) => new(capacity);

        /// <summary>
        /// 尝试为写入保留 size 字节的空间并返回可写的段与偏移。
        /// 如果当前段空间不足会原子地切换到新段并将旧段加入封存链表，随后重试保留。
        /// 该方法线程安全且为阻塞重试实现。
        /// </summary>
        public CommandBufferChunk GetBufferChunk(int size, out int offset)
        {
            while (true)
            {
                var seg = Volatile.Read(ref currentChunk);
                if (TryReserve(seg, size, out offset))
                    return seg;

                var newSeg = CreateSegment(Math.Max(DefaultSegmentSize, size));
                if (Interlocked.CompareExchange(ref currentChunk, newSeg, seg) == seg)
                {
                    PushSealedChunk(seg);
                    continue;
                }

                newSeg.Dispose();
            }
        }

        /// <summary>
        /// 在指定 chunk 上尝试原子保留 size 字节，成功返回 true 并通过 out 返回偏移。
        /// 该方法仅包装底层 chunk 的 TryReserve 实现。
        /// </summary>
        public bool TryReserve(CommandBufferChunk chunk, int size, out int offset)
            => chunk.TryReserve(size, out offset);

        /// <summary>
        /// 将已满/已封存的段以原子方式压入封存链表，供回放线程消费。
        /// </summary>
        private void PushSealedChunk(CommandBufferChunk chunk)
        {
            var node = new ChunkNode(chunk);
            while (true)
            {
                var head = Volatile.Read(ref _sealedHead);
                node.Next = head;
                if (Interlocked.CompareExchange(ref _sealedHead, node, head) == head)
                    return;
            }
        }

        /// <summary>
        /// 弹出一个封存段供回放（主线程）使用：
        /// - 优先从封存链表弹出已封存段；
        /// - 若链表为空，则尝试返回 currentChunk（仅当其尚未被标记为 popped 且含有数据时）。
        /// </summary>
        public bool TryPopChunk([NotNullWhen(true)] out CommandBufferChunk chunk)
        {
            while (true)
            {
                var head = Volatile.Read(ref _sealedHead);
                if (head == null)
                    break;

                if (Interlocked.CompareExchange(ref _sealedHead, head.Next, head) == head)
                {
                    chunk = head.Chunk;
                    return true;
                }
            }

            var current = Volatile.Read(ref currentChunk);
            if (current == null || current.UsedBytes == 0)
            {
                chunk = default!;
                return false;
            }

            if (Interlocked.CompareExchange(ref _currentPopped, 1, 0) == 0)
            {
                chunk = current;
                return true;
            }

            chunk = default!;
            return false;
        }

        /// <summary>
        /// 回放器在完成对 currentChunk 的回放后应调用此方法以重置当前段的写入游标，
        /// 并将 popped 标记清零以允许下一轮弹出。该方法应仅由主线程调用。
        /// </summary>
        public void ResetCurrentChunkAfterRead()
        {
            var current = Volatile.Read(ref currentChunk);
            current?.ResetCursor();
            Volatile.Write(ref _currentPopped, 0);
        }

        /// <summary>
        /// 在回放周期结束时重置虚拟实体计数（可用于测试/诊断）。
        /// </summary>
        public void Clear()
        {
            virtualNumber = 0;
        }

        /// <summary>
        /// 释放所有剩余段资源。通常在 World/系统销毁时调用。
        /// </summary>
        public void Dispose()
        {
            var head = Interlocked.Exchange(ref _sealedHead, null);
            while (head != null)
            {
                head.Chunk.Dispose();
                head = head.Next;
            }

            Interlocked.Exchange(ref currentChunk, null!)?.Dispose();
        }
    }
}