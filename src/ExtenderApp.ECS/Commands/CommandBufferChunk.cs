using System.Runtime.InteropServices;
using ExtenderApp.Contracts;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 段数据结构：持有非托管内存并维护原子游标。
    /// </summary>
    internal sealed unsafe class CommandBufferChunk : DisposableObject
    {
        /// <summary>
        /// 非托管内存指针（字节）。
        /// </summary>
        public readonly byte* Ptr;

        /// <summary>
        /// 段容量（字节）。
        /// </summary>
        public readonly int Capacity;

        /// <summary>
        /// 当前已使用字节数（游标），通过原子操作更新以支持并发写入。
        /// </summary>
        private int _cursor;

        /// <summary>
        /// 当前已写入字节数。 使用 volatile 读取以确保与写入方的可见性。
        /// </summary>
        public int UsedBytes => Volatile.Read(ref _cursor);

        public CommandBufferChunk(int capacity)
        {
            Capacity = capacity;
            Ptr = (byte*)NativeMemory.Alloc((nuint)capacity);
            _cursor = 0;
        }

        public bool TryReserve(int size, out int offset)
        {
            offset = -1;
            while (true)
            {
                int old = Volatile.Read(ref _cursor);
                int desired = old + size;
                if (desired > Capacity)
                    return false;

                if (Interlocked.CompareExchange(ref _cursor, desired, old) == old)
                {
                    offset = old;
                    return true;
                }
            }
        }

        public void ResetCursor() => Interlocked.Exchange(ref _cursor, 0);

        protected override void DisposeManagedResources()
        {
            if (Ptr != null)
            {
                NativeMemory.Free(Ptr);
            }

            base.DisposeManagedResources();
        }
    }
}