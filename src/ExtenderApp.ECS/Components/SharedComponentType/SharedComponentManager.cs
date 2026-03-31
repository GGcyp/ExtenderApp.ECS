using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 共享组件管理器（框架内部使用）。 使用非托管内存块 <see cref="Chunk" /> 为每个共享组件类型保存 1 个实例， 适用于全局共享、低频更新、高频读取的数据（例如输入快照、全局配置快照、时间缩放等）。
    ///
    /// 约束：共享组件类型必须为 struct。 线程模型：写入（Set/RemoveAt/Clear）仅允许主线程；读取（TryGet/Get/Has）可在任意线程调用。
    /// </summary>
    internal sealed class SharedComponentManager : DisposableObject
    {
        private readonly ArchetypeChunk?[] _chunks;
        private SharedComponentMask _mask;

        public SharedComponentManager()
        {
            _chunks = new ArchetypeChunk[SharedComponentRegistry.MaxSharedCount];
            _mask = new();
        }

        /// <summary>
        /// 尝试添加共享组件；若已存在则直接覆盖。
        /// </summary>
        public bool TryAddComponent<T>(in T value)
        {
            var type = SharedComponentType.Create<T>();
            if (_mask.On(type))
                return TrySetComponent(value);

            var chunk = ArchetypeChunkProvider.GetOrCreate<T>(true).Rent();
            chunk.Initialize(1);
            chunk.SetComponent(0, value);
            _chunks[GetArrayIndex(type)] = chunk;
            _mask.Add(type);
            return true;
        }

        /// <summary>
        /// 尝试覆盖已存在的共享组件。
        /// </summary>
        public bool TrySetComponent<T>(in T value)
        {
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            int index = GetArrayIndex(type);
            var chunk = _chunks[index];
            if (chunk == null)
                return false;

            var chunkT = chunk as ArchetypeChunk<T>;
            chunkT?.SetComponent(0, value);
            return chunkT != null;
        }

        /// <summary>
        /// 尝试读取共享组件。
        /// </summary>
        public bool TryGetComponent<T>(out T value)
        {
            value = default!;
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            var chunk = _chunks[GetArrayIndex(type)];
            if (chunk == null)
                return false;

            var chunkT = chunk as ArchetypeChunk<T>;
            if (chunkT == null)
                return false;

            value = chunkT.GetComponent(0);
            return true;
        }

        /// <summary>
        /// 设置共享组件：不存在则新增，存在则覆盖。
        /// </summary>
        public void Set<T>(in T value)
        {
            if (!TryAddComponent(value))
            {
                TrySetComponent(value);
            }
        }

        /// <summary>
        /// 尝试读取共享组件（兼容命名）。
        /// </summary>
        public bool TryGet<T>(out T value) => TryGetComponent(out value);

        /// <summary>
        /// 读取共享组件，不存在则抛出异常。
        /// </summary>
        public T Get<T>()
        {
            if (TryGetComponent<T>(out var value))
                return value;

            throw new KeyNotFoundException($"共享组件 {typeof(T).Name} 不存在。");
        }

        /// <summary>
        /// 是否存在指定共享组件。
        /// </summary>
        public bool Has<T>() => _mask.On<T>();

        /// <summary>
        /// 移除共享组件。
        /// </summary>
        public bool Remove<T>()
        {
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            int index = GetArrayIndex(type);
            var chunk = _chunks[index];
            if (chunk != null)
            {
                chunk.Return();
                _chunks[index] = null;
            }

            _mask.Remove(type);
            return true;
        }

        /// <summary>
        /// 获取共享组件在数组中的索引（从 0 开始）。由于 SharedComponentType 的 TypeIndex 从 1 开始，因此需要减 1。
        /// </summary>
        /// <param name="type">共享组件类型。</param>
        /// <returns>共享组件在数组中的索引。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetArrayIndex(SharedComponentType type) => type.TypeIndex - 1;

        protected override void DisposeManagedResources()
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = _chunks[i];
                if (chunk != null)
                {
                    chunk.Return();
                    _chunks[i] = null;
                }
            }

            base.DisposeManagedResources();
        }
    }
}