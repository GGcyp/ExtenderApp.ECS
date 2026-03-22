using System.Collections.Concurrent;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 非泛型基类：用于按组件类型查找或创建对应的 <see cref="ArchetypeChunkProvider{T}" /> 实例。 定义了租用与归还 <see cref="ArchetypeChunk" /> 的抽象接口，具体实现由泛型子类负责。
    /// </summary>
    internal abstract class ArchetypeChunkProvider
    {
        /// <summary>
        /// 缓存每个组件类型对应的提供器实例，避免反射重复创建。 键为组件类型（struct 类型实现 IComponent），值为对应的 ArchetypeChunkProvider 实例。
        /// </summary>
        private static ConcurrentDictionary<Type, ArchetypeChunkProvider> _providers = new();

        /// <summary>
        /// 根据组件类型查找或创建对应的 ArchetypeChunkProvider 实例。
        /// </summary>
        /// <param name="componentType">组件类型（例如 typeof(MyComponent)）。</param>
        /// <returns>对应的 ArchetypeChunkProvider 实例（不能为 null）。</returns>
        public static ArchetypeChunkProvider GetOrCreate(ComponentType componentType)
        {
            return GetOrCreate(componentType.TypeInstance);
        }

        /// <summary>
        /// 根据组件类型查找或创建对应的 ArchetypeChunkProvider 实例。 组件类型必须为值类型（struct）并实现 <see cref="IComponent" /> 接口。 返回的实例为对应的泛型子类（ <see
        /// cref="ArchetypeChunkProvider" />）。
        /// </summary>
        /// <param name="componentType">组件类型（例如 typeof(MyComponent)）。</param>
        /// <returns>对应的 ArchetypeChunkProvider 实例（不能为 null）。</returns>
        public static ArchetypeChunkProvider GetOrCreate(Type componentType)
        {
            return _providers.GetOrAdd(componentType, static t =>
            {
                var providerType = typeof(ArchetypeChunkProvider<>).MakeGenericType(t);
                return (ArchetypeChunkProvider)Activator.CreateInstance(providerType)!;
            });
        }

        /// <summary>
        /// 类型安全的泛型获取或创建方法。
        /// </summary>
        /// <typeparam name="T">组件类型（struct）。</typeparam>
        /// <returns>对应的 <see cref="ArchetypeChunkProvider{T}" /> 单例实例。</returns>
        public static ArchetypeChunkProvider<T> GetOrCreate<T>() where T : struct
        {
            return (ArchetypeChunkProvider<T>)_providers.GetOrAdd(typeof(T), static t => new ArchetypeChunkProvider<T>());
        }

        /// <summary>
        /// 从提供器中租用一个 <see cref="ArchetypeChunk" /> 实例（抽象方法，由子类实现具体类型返回）。
        /// </summary>
        /// <param name="startIndex">分配给返回块的全局起始索引（可选）。</param>
        /// <returns>可用的 <see cref="ArchetypeChunk" /> 实例（可能为具体的 ArchetypeChunk{T}）。</returns>
        public abstract ArchetypeChunk Rent(int startIndex = 0);

        /// <summary>
        /// 将不再使用的 <see cref="ArchetypeChunk" /> 实例归还到提供器（抽象方法，由子类实现具体回收逻辑）。
        /// </summary>
        /// <param name="chunk">要归还的块实例，允许为 null（将被忽略）。</param>
        public abstract void Return(ArchetypeChunk? chunk);

        /// <summary>
        /// 生成一个新的空的 <see cref="ArchetypeChunkList" /> 实例。
        /// </summary>
        /// <param name="capacity">预设列表容量。</param>
        /// <returns>返回空的 <see cref="ArchetypeChunkList" /> 实例</returns>
        public abstract ArchetypeChunkList CreateChunkList(int capacity);
    }

    /// <summary>
    /// 按组件类型 T 管理的 <see cref="ArchetypeChunk{T}" /> 对象池与提供器。
    ///
    /// 特性：
    /// - 提供无锁的单元素快速槽（ <c>lastChunk</c>）作为 fast-path；
    /// - 使用线程安全的 <see cref="ConcurrentQueue{T}" /> 作为后备缓冲；
    /// - 在归还时会先将底层 <see cref="Chunk" /> 返回至 <see cref="ChunkPool" />，并将 <see cref="ArchetypeChunk{T}" /> 对象置为未初始化状态；
    /// - 当池达到上限时，多余的对象会被释放（Dispose），避免无限增长。
    /// </summary>
    /// <typeparam name="T">组件类型，要求为 struct 且实现 <see cref="IComponent" />。</typeparam>
    internal sealed class ArchetypeChunkProvider<T> : ArchetypeChunkProvider where T : struct
    {
        /// <summary>
        /// 池允许保留的最大 ArchetypeChunk 实例数（含快速槽与队列中的项）。 超过该数量后归还的实例将被释放。
        /// </summary>
        private const int MaxPoolSize = 16;

        /// <summary>
        /// 后备线程安全队列的延迟初始化包装。
        /// </summary>
        private readonly Lazy<ConcurrentQueue<ArchetypeChunk<T>>> _poolLazy;

        /// <summary>
        /// 后备队列引用（延迟获取）。
        /// </summary>
        private ConcurrentQueue<ArchetypeChunk<T>> _pool => _poolLazy.Value;

        /// <summary>
        /// 无锁快速槽（单元素缓存），用于减少频繁入/取操作的开销。
        /// </summary>
        private ArchetypeChunk<T>? lastChunk;

        /// <summary>
        /// 用于创建/初始化 ArchetypeChunk 的底层 ChunkPool 引用（可替换以便测试）。
        /// </summary>
        private readonly ChunkPool _chunkPool;

        /// <summary>
        /// 获取池中近似可用的 ArchetypeChunk 数量（包括快速槽与队列中的项）。 并发环境下为近似值，仅用于监控或调试。
        /// </summary>
        public int Count => (lastChunk != null ? 1 : 0) + (_pool?.Count ?? 0);

        /// <summary>
        /// 创建默认的提供器，使用共享的 <see cref="ChunkPool.Share" />。
        /// </summary>
        public ArchetypeChunkProvider() : this(ChunkPool.Share)
        {
        }

        /// <summary>
        /// 使用指定的 <see cref="ChunkPool" /> 创建提供器实例（用于测试或替代默认池）。
        /// </summary>
        /// <param name="chunkPool">用于分配底层 Chunk 的池，不可为 null。</param>
        public ArchetypeChunkProvider(ChunkPool chunkPool)
        {
            _poolLazy = new(() => new ConcurrentQueue<ArchetypeChunk<T>>());
            _chunkPool = chunkPool ?? throw new ArgumentNullException(nameof(chunkPool));
        }

        /// <summary>
        /// 从池中租用一个 <see cref="ArchetypeChunk{T}" /> 实例。 行为：
        /// 1. 尝试从无锁快速槽取出；
        /// 2. 若无则从队列弹出；
        /// 3. 若队列也为空则创建新的实例并返回。 返回的对象 <see cref="ArchetypeChunk{T}.StartIndex" /> 会被设置为传入的 <paramref name="startIndex" /> 值， 但不会自动 Initialize 底层
        /// Chunk（由调用者决定何时调用 Initialize）。
        /// </summary>
        /// <param name="startIndex">为返回的 ArchetypeChunk 设置的全局起始索引（默认 0）。</param>
        /// <returns>可用的 <see cref="ArchetypeChunk{T}" /> 实例。</returns>
        public override ArchetypeChunk<T> Rent(int startIndex = 0)
        {
            ArchetypeChunk<T>? item = Interlocked.Exchange(ref lastChunk, null);
            if (item != null)
            {
                item.StartIndex = startIndex;
                return item;
            }

            if (_pool != null && _pool.TryDequeue(out item))
            {
                item.StartIndex = startIndex;
                return item;
            }

            ArchetypeChunk<T> created = new(this, _chunkPool);
            created.StartIndex = startIndex;
            return created;
        }

        /// <summary>
        /// 非泛型实现：将传入的 <see cref="ArchetypeChunk" /> 转换为具体的 <see cref="ArchetypeChunk{T}" /> 并归还。
        /// </summary>
        /// <param name="chunk">要归还的块实例，若类型不匹配或为 null 则忽略。</param>
        public override void Return(ArchetypeChunk? chunk)
        {
            Return(chunk as ArchetypeChunk<T>);
        }

        /// <summary>
        /// 将不再使用的 <see cref="ArchetypeChunk{T}" /> 实例归还到池中。 行为：
        /// - 先调用 <see cref="ArchetypeChunk{T}.ReturnChunkToPool" /> 将其内部的底层 <see cref="Chunk" /> 归还到 <see cref="ChunkPool" />；
        /// - 然后优先尝试放入快速槽；
        /// - 若快速槽被占用，再判断池大小：若已达上限则释放该对象（Dispose），否则将其入队作为后备。
        /// </summary>
        /// <param name="chunk">要归还的实例，若为 null 则忽略。</param>
        public void Return(ArchetypeChunk<T>? chunk)
        {
            if (chunk == null)
                return;

            // 先归还底层 Chunk，这样保存在池中的 ArchetypeChunk 对象为未初始化状态
            try
            {
                chunk.ReturnChunkToPool();
            }
            catch
            {
                // 忽略归还过程中可能抛出的异常，继续尝试回收对象或释放
            }

            if (Interlocked.CompareExchange(ref lastChunk, chunk, null) == null)
            {
                return;
            }

            if (Count >= MaxPoolSize)
            {
                // 池已满，释放该对象
                chunk.Dispose();
                return;
            }

            _pool.Enqueue(chunk);
        }

        /// <summary>
        /// 清空池并释放其中所有的 <see cref="ArchetypeChunk{T}" /> 实例（包括快速槽与队列）。
        /// </summary>
        public void Clear()
        {
            // 释放快速槽
            var fast = Interlocked.Exchange(ref lastChunk, null);
            if (fast != null)
            {
                fast.Dispose();
            }

            // 清空队列
            if (_pool != null)
            {
                while (_pool.TryDequeue(out var c))
                {
                    c.Dispose();
                }
            }
        }

        /// <summary>
        /// 生成一个新的空的 <see cref="ArchetypeChunkList{T}" /> 实例，作为链表头使用。
        /// </summary>
        /// <returns>返回空的 <see cref="ArchetypeChunkList{T}" /> 实例</returns>
        public override ArchetypeChunkList<T> CreateChunkList(int capacity) => new(capacity);
    }
}