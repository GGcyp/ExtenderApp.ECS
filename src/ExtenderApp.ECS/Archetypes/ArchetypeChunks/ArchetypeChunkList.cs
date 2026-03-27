using System.Diagnostics.CodeAnalysis;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary> 非泛型的块链表容器，保存一列的各个 ArchetypeChunk 头或块实例。
    ///
    /// 语义：此类型作为基础容器使用，内部元素为通用的 `ArchetypeChunk` 引用。 注意：因为泛型集合在 .NET 中是不变的（invariant），所以不能将 `List<ArchetypeChunk>` 直接转换为 `List<ArchetypeChunk<T>>`。
    /// 若需要基于类型的访问，应显式逐项检查并转换或使用专用的泛型容器 `ArchetypeChunkList<T>` 来保存强类型块。 </summary>
    internal class ArchetypeChunkList : List<ArchetypeChunk>
    {
        public ArchetypeChunkList()
        {
        }

        public ArchetypeChunkList(int capacity) : base(capacity)
        {
        }
    }

    /// <summary> 泛型的块链表容器，类型参数 <c>T</c> 指定该列实际存放的组件类型。
    ///
    /// 语义：当确保某一列仅包含 `ArchetypeChunk<T>` 时，应使用此类型以避免运行时类型转换。 该类型继承自 <see cref="ArchetypeChunkList"/>，但不能在运行时将非泛型列表直接视为泛型列表；
    /// 若需要从非泛型列表构造泛型列表，请逐项转换并检查元素类型（例如使用 LINQ 的 <c>OfType&lt;ArchetypeChunk&lt;T&gt;&gt;()</c> 或手工筛选）。 </summary> <typeparam name="T">组件类型（值类型且实现
    /// <see cref="IComponent"/>）。</typeparam>
    internal class ArchetypeChunkList<T> : ArchetypeChunkList
    {
        public ArchetypeChunkList()
        {
        }

        public ArchetypeChunkList(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// 尝试按索引获取指定类型的块实例。若索引越界或目标块不是期望的泛型块则返回 false。
        /// </summary>
        /// <param name="index">块索引（0 表示列表第一个元素）。</param>
        /// <param name="chunk">输出的块实例（若返回 true）。</param>
        /// <returns>若成功获取块实例则返回 true，否则返回 false。</returns>
        public bool TryGetChunk(int index, [NotNullWhen(true)] out ArchetypeChunk<T> chunk)
        {
            chunk = default!;
            if (index < 0 || index >= Count) return false;
            chunk = (this[index] as ArchetypeChunk<T>)!;
            return chunk != null;
        }

        public override string ToString() => $"ArchetypeChunkList<{typeof(T).Name}> (Count: {Count})";
    }
}