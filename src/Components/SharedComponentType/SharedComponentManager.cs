using System.Runtime.CompilerServices;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 管理共享组件实例的容器。该管理器为每种共享组件类型维护一个容量为
    /// <see cref="SharedComponentRegistry.MaxSharedCount"/> 的块数组（<see cref="ArchetypeChunk"/>），
    /// 每个块用于存放该共享组件类型的单个实例。
    ///
    /// 约定：共享组件应为值类型（struct）并尽可能保持线程安全。对于写入操作（例如 Set/Remove），
    /// 需要外部同步；而读取操作（TryGet/Get/Has）可被并发使用。
    /// </summary>
    internal sealed class SharedComponentManager : DisposableObject
    {
        /// <summary>
        /// 存储每个共享组件类型对应的块。数组索引对应于
        /// <see cref="SharedComponentType.TypeIndex"/> - 1。
        /// 每个非空项为一个已租用并初始化、包含单个共享组件实例的 <see cref="ArchetypeChunk"/>。
        /// </summary>
        private readonly ArchetypeChunk?[] _chunks;

        /// <summary>
        /// 当前已注册的共享组件类型掩码，用于快速判断某个类型是否存在。
        /// </summary>
        private SharedComponentMask _mask;

        /// <summary>
        /// 初始化一个新的 <see cref="SharedComponentManager"/> 实例并分配内部数组与掩码。
        /// </summary>
        public SharedComponentManager()
        {
            _chunks = new ArchetypeChunk[SharedComponentRegistry.MaxSharedCount];
            _mask = new();
        }

        /// <summary>
        /// 尝试添加一个共享组件实例。
        /// 如果该类型尚未注册，则为其创建一个新的 <see cref="ArchetypeChunk"/>、初始化并存放该值；
        /// 如果该类型已存在，则调用 <see cref="TrySetComponent{T}(in T)"/> 来设置其值。
        /// </summary>
        /// <typeparam name="T">共享组件的类型（建议为 struct）。</typeparam>
        /// <param name="value">要存放的组件值。</param>
        /// <returns>添加或设置成功返回 true；否则返回 false。</returns>
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
        /// 尝试设置已存在的共享组件的值。若该类型未注册或对应块不存在则返回 false。
        /// </summary>
        /// <typeparam name="T">共享组件的类型。</typeparam>
        /// <param name="value">要设置的值。</param>
        /// <returns>设置成功返回 true；否则返回 false。</returns>
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
        /// 尝试获取指定类型的共享组件值。
        /// </summary>
        /// <typeparam name="T">共享组件的类型。</typeparam>
        /// <param name="value">当返回 true 时输出获取到的值；否则为默认值。</param>
        /// <returns>若存在则返回 true 并通过 out 参数输出值；否则返回 false。</returns>
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
        /// 设置共享组件的值。如果该类型尚未注册则先添加该组件类型，然后设置值。
        /// </summary>
        /// <typeparam name="T">共享组件的类型。</typeparam>
        /// <param name="value">要设置的值。</param>
        public void Set<T>(in T value)
        {
            if (!TryAddComponent(value))
            {
                TrySetComponent(value);
            }
        }

        /// <summary>
        /// 尝试获取共享组件，等同于 <see cref="TryGetComponent{T}(out T)"/>。
        /// </summary>
        public bool TryGet<T>(out T value) => TryGetComponent(out value);

        /// <summary>
        /// 获取指定类型的共享组件值；若不存在则抛出 <see cref="KeyNotFoundException"/>。
        /// </summary>
        /// <typeparam name="T">共享组件的类型。</typeparam>
        /// <returns>该共享组件的值。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的共享组件不存在时抛出。</exception>
        public T Get<T>()
        {
            if (TryGetComponent<T>(out var value))
                return value;

            throw new KeyNotFoundException($"������� {typeof(T).Name} �����ڡ�");
        }

        /// <summary>
        /// 判断是否存在指定类型的共享组件。
        /// </summary>
        public bool Has<T>() => _mask.On<T>();

        /// <summary>
        /// 移除指定类型的共享组件并释放其占用的块资源。
        /// </summary>
        /// <typeparam name="T">要移除的共享组件类型。</typeparam>
        /// <returns>若存在并成功移除返回 true；否则返回 false。</returns>
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
        /// 将共享组件类型的 TypeIndex 转换为内部数组索引（TypeIndex 从 1 开始，
        /// 因此数组索引为 TypeIndex - 1）。
        /// </summary>
        /// <param name="type">共享组件类型。</param>
        /// <returns>在内部数组中对应的索引。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetArrayIndex(SharedComponentType type) => type.TypeIndex - 1;

        /// <summary>
        /// 释放管理的托管资源：归还并清空所有已租用的块。
        /// </summary>
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