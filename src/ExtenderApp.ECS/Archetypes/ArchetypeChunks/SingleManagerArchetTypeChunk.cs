using System.Runtime.InteropServices;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 单例托管堆块。用于存储单例托管组件（即每个组件类型仅有一个实例）的数据。 由于单例托管组件的特殊性（全局唯一、频繁访问），该块采用专门设计以优化性能和内存使用。 约束：单例托管组件类型必须为 class。 线程模型：写入（Set/Remove）仅允许主线程；读取（TryGet/Has）可在任意线程调用。
    /// </summary>
    internal sealed class SingleManagerArchetTypeChunk<T> : ArchetypeChunk<T>
    {
        private T component;

        public override Span<T> Span => MemoryMarshal.CreateSpan(ref component, 1);

        public SingleManagerArchetTypeChunk(ArchetypeChunkProvider<T> provider) : base(provider)
        {
            component = default!;
        }

        public override void CopiedUnsafe(int localIndex, nint soure, int count)
        {
            throw new NotImplementedException();
        }

        public override T GetComponent(int index)
        {
            return component;
        }

        public override ref T GetComponentRef(int index)
        {
            return ref component;
        }

        public override void SetComponent(int index, T value)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index), "单例托管组件块仅支持索引0。");

            component = value;
        }

        public override void Swap(int localIndexA, int localIndexB)
        {
        }

        public override bool TryCopyTo(int globalIndex, ArchetypeChunk newArchetypeChunk, int newGlobalIndex)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeProtected()
        {
        }

        protected override void RemoveAtProtected(int localIndex)
        {
            component = default!;
        }

        protected override void ReturnChunkToPool()
        {
            if (component is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        protected override void ThrowNotInitialize()
        {
        }
    }
}