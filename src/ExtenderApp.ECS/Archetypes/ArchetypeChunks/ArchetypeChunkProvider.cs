using System.Collections.Concurrent;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 非泛型基类。按组件类型查找或创建对应的泛型提供器（ <see cref="ArchetypeChunkProvider{T}" />）。 同时定义租用（Rent）与归还（Return） <see cref="ArchetypeChunk" /> 的抽象接口，具体实现由泛型子类负责。
    /// </summary>
    internal abstract class ArchetypeChunkProvider
    {
        /// <summary>
        /// 缓存每种组件类型对应的提供器实例，避免重复通过反射创建。键为组件类型，值为对应的 <see cref="ArchetypeChunkProvider"/> 实例。
        /// </summary>
        private static ConcurrentDictionary<Type, ArchetypeChunkProvider> _providers = new();

        /// <summary>
        /// 根据组件类型（由 ComponentType 包装）查找或创建提供器。
        /// </summary>
        public static ArchetypeChunkProvider GetOrCreate(ComponentType componentType)
        {
            return GetOrCreate(componentType.TypeInstance);
        }

        /// <summary>
        /// 根据共享组件类型（由 SharedComponentType 包装）查找或创建提供器。
        /// </summary>
        /// <param name="componentType">共享组件类型。</param>
        /// <returns>对应的 ArchetypeChunkProvider 实例。</returns>
        public static ArchetypeChunkProvider GetOrCreate(SharedComponentType componentType)
        {
            return GetOrCreate(componentType.TypeInstance);
        }

        /// <summary>
        /// 根据组件的 System.Type 查找或创建对应的泛型提供器实例。 组件类型应为值类型（struct）并实现 <see cref="IComponent" />。
        /// </summary>
        public static ArchetypeChunkProvider GetOrCreate(Type componentType)
        {
            return _providers.GetOrAdd(componentType, static t =>
            {
                var providerType = typeof(ArchetypeChunkProvider<>).MakeGenericType(t);
                var provider = (ArchetypeChunkProvider)Activator.CreateInstance(providerType)!;
                provider.InitCreateFunc(false);
                return provider;
            });
        }

        /// <summary>
        /// 类型安全的泛型获取方法。
        /// </summary>
        public static ArchetypeChunkProvider<T> GetOrCreate<T>(bool isSingle)
        {
            var provider = (ArchetypeChunkProvider<T>)_providers.GetOrAdd(typeof(T), t => new ArchetypeChunkProvider<T>());
            provider.InitCreateFunc(isSingle);
            return provider;
        }

        /// <summary>
        /// 当前提供器管理的组件类型是否为空结构体（无字段）。
        /// </summary>
        public abstract bool IsEmptyComponent { get; }

        /// <summary>
        /// 从提供器中租用一个 <see cref="ArchetypeChunk" /> 实例（具体类型由子类返回）。
        /// </summary>
        /// <param name="startIndex">分配给返回块的全局起始索引（可选）。</param>
        public abstract ArchetypeChunk Rent(int startIndex = 0);

        /// <summary>
        /// 将不再使用的 <see cref="ArchetypeChunk" /> 实例归还到提供器（允许为 null）。
        /// </summary>
        public abstract void Return(ArchetypeChunk? chunk);

        /// <summary>
        /// 必须调用的初始化块生成函数。
        /// </summary>
        /// <param name="isSingle">指示是否为单例组件块。</param>
        public abstract void InitCreateFunc(bool isSingle);

        /// <summary>
        /// 创建一个新的空的 <see cref="ArchetypeChunkList" /> 实例（容量可预设）。
        /// </summary>
        public abstract ArchetypeChunkList CreateChunkList(int capacity);
    }

    /// <summary>
    /// 按组件类型 T1 管理的 <see cref="ArchetypeChunk{T}" /> 对象池与提供器。
    ///
    /// 主要特性：
    /// - 提供无锁的单元素快速槽（`lastChunk`）作为 fast-path；
    /// - 使用线程安全的 <see cref="ConcurrentQueue{T}" /> 作为后备缓冲；
    /// - 归还时会先把底层 <see cref="Chunk" /> 返回到 <see cref="ChunkPool" />，并把 <see cref="ArchetypeChunk{T}" /> 置为未初始化状态；
    /// - 当池达到上限时，多余的对象会被释放以避免无限增长。
    /// </summary>
    /// <typeparam name="T">组件类型，要求为 struct 并实现 <see cref="IComponent" />。</typeparam>
    internal sealed class ArchetypeChunkProvider<T> : ArchetypeChunkProvider
    {
        private const int MaxPoolSize = 16;

        private readonly Lazy<ConcurrentQueue<ArchetypeChunk<T>>> _poolLazy;

        /// <summary>
        /// 实际的对象池队列，仅在 T1 非空组件时创建。使用 Lazy 初始化以避免不必要的分配。对于空组件类型，该属性永远为 null，且不会被访问。
        /// </summary>
        private ConcurrentQueue<ArchetypeChunk<T>> _pool => _poolLazy.Value;

        /// <summary>
        /// 上一块的无锁快速槽，允许单线程快速租用与归还。使用 Interlocked 操作确保线程安全。
        /// </summary>
        private ArchetypeChunk<T>? lastChunk;

        /// <summary>
        /// 创建新的 <see cref="ArchetypeChunk{T}" /> 实例的工厂方法，根据 T1 的类型选择托管或非托管实现。对于空组件类型，该函数不会被调用。
        /// </summary>
        private Func<ArchetypeChunkProvider<T>, ArchetypeChunk<T>> _createChunkFunc;

        /// <summary>
        /// 近似池中可用项数量（包含快速槽与队列中的项）。并发环境下为近似值，仅用于监控或调试。
        /// </summary>
        public int Count => (lastChunk != null ? 1 : 0) + (_pool?.Count ?? 0);

        public override bool IsEmptyComponent { get; }

        /// <summary>
        /// 构造函数：延迟创建队列，判断组件是否为空结构体，并根据 T1 类型选择托管或非托管的 ArchetypeChunk 实现。
        /// </summary>
        public ArchetypeChunkProvider() : base()
        {
            IsEmptyComponent = ComponentType.Create<T>().IsEmptyComponent;
            _poolLazy = IsEmptyComponent ? default! : new(() => new());
            _createChunkFunc = default!;
        }

        ///<inheritdoc/>
        public override void InitCreateFunc(bool isSingle)
        {
            if (_createChunkFunc != null)
                return;

            _createChunkFunc = IsEmptyComponent ? default! : CreateFunc(isSingle);
        }

        /// <summary>
        /// 创建新的 <see cref="ArchetypeChunk{T}" /> 实例的工厂方法，根据 T1 的类型选择托管或非托管实现。
        /// </summary>
        /// <param name="isSingle">指示是否为单例组件块。</param>
        /// <returns>返回一个用于创建 <see cref="ArchetypeChunk{T}" /> 实例的委托。</returns>
        private static Func<ArchetypeChunkProvider<T>, ArchetypeChunk<T>> CreateFunc(bool isSingle)
        {
            if (isSingle)
            {
                return static (p) => new SingleManagerArchetTypeChunk<T>(p);
            }
            else if (typeof(T).IsClass)
            {
                return static (p) => new ManagedArchetTypeChunk<T>(p);
            }
            else
            {
                return static (p) => new UnmanagedArchetTypeChunk<T>(p);
            }
        }

        /// <summary>
        /// 从池中租用一个 <see cref="ArchetypeChunk{T}" /> 实例：
        /// 1. 优先从无锁快速槽取出；
        /// 2. 其次从后备队列弹出；
        /// 3. 否则创建新的实例并返回。 返回对象的 <see cref="ArchetypeChunk{T}.StartIndex" /> 会设置为传入值，但不会自动初始化底层 <see cref="Chunk" />（由调用者决定何时 Initialize）。
        /// </summary>
        public override ArchetypeChunk<T> Rent(int startIndex = 0)
        {
            ArchetypeChunk<T>? item = Interlocked.Exchange(ref lastChunk, null);
            if (item != null)
            {
                // 复用前清理链表指针，避免带入旧链
                item.Next = null;
                item.StartIndex = startIndex;
                return item;
            }

            if (_pool != null && _pool.TryDequeue(out item))
            {
                // 复用前清理链表指针，避免带入旧链
                item.Next = null;
                item.StartIndex = startIndex;
                return item;
            }

            ArchetypeChunk<T> created = _createChunkFunc.Invoke(this);
            created.StartIndex = startIndex;
            return created;
        }

        /// <summary>
        /// 非泛型接口实现：尝试将传入的基类 <see cref="ArchetypeChunk" /> 转为 <see cref="ArchetypeChunk{T}" /> 并归还。 类型不匹配或为 null 时会被忽略。
        /// </summary>
        public override void Return(ArchetypeChunk? chunk)
        {
            Return(chunk as ArchetypeChunk<T>);
        }

        /// <summary>
        /// 将不再使用的 <see cref="ArchetypeChunk{T}" /> 实例归还到对象池：
        /// - 先断开链表（Next）并把底层 <see cref="Chunk" /> 返还给 <see cref="ChunkPool" />；
        /// - 优先尝试放入无锁快速槽；
        /// - 若快速槽被占用且池未满则入队；否则释放该对象（Dispose）。
        /// </summary>
        public void Return(ArchetypeChunk<T>? chunk)
        {
            if (chunk == null)
                return;

            // 归还前断开链表，避免把旧 Next 一并带入对象池
            chunk.Next = null;

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
        /// 清空池并释放其中所有 <see cref="ArchetypeChunk{T}" /> 实例（包括快速槽与队列）。
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
        /// 创建一个新的空的 <see cref="ArchetypeChunkList{T}" /> 实例，通常作为链表头使用。
        /// </summary>
        public override ArchetypeChunkList<T> CreateChunkList(int capacity) => new(capacity);
    }
}