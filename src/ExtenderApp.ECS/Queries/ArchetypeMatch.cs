using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 表示查询匹配到的某个 Archetype 及其为当前查询预计算的列索引映射。
    ///
    /// 语义：ArchetypeMatch 将一个具体的 <see cref="Archetype" /> 与按查询顺序排列的 列索引数组关联起来。列索引数组通常来自 <see cref="ArrayPool{T}.Shared.Rent" />， 在不再使用时需要通过 <see
    /// cref="Dispose" /> 将数组归还池中。
    /// </summary>
    internal readonly struct ArchetypeMatch : IDisposable
    {
        /// <summary>
        /// 匹配到的 Archetype 实例引用（只读）。
        /// </summary>
        public readonly Archetype Archetype;

        /// <summary>
        /// 与查询中组件顺序一一对应的列索引数组（由 <see cref="ArrayPool{T}.Shared.Rent" /> 分配）。 每个元素表示该查询位置在对应 Archetype 中的组件列位置（如果为 -1 表示缺失）。
        /// </summary>
        public readonly int[] ColumnIndices;

        /// <summary>
        /// 表示 ColumnIndices 中有效组件数量或查询组件数量的记录值。
        /// </summary>
        public readonly int ComponentCount;

        /// <summary>
        /// 使用指定的 Archetype 与列索引数组构建一个新的 ArchetypeMatch 实例。
        /// </summary>
        /// <param name="archetype">匹配到的 Archetype。</param>
        /// <param name="columnIndices">按查询顺序排列的列索引数组（来自 ArrayPool）。</param>
        /// <param name="componentCount">列索引数组中表示的组件数量（通常等于查询组件数）。</param>
        public ArchetypeMatch(Archetype archetype, int[] columnIndices, int componentCount)
        {
            Archetype = archetype;
            ColumnIndices = columnIndices;
            ComponentCount = componentCount;
        }

        /// <summary>
        /// 尝试根据查询内部的顺序位置获取对应列的 ArchetypeChunk{T} 头（若存在）。
        /// </summary>
        /// <typeparam name="T">期望的组件类型。</typeparam>
        /// <param name="index">查询组件的位置索引（0-based），对应于 <see cref="ColumnIndices" /> 的下标。</param>
        /// <param name="component">若返回 true，则输出对应类型的 <see cref="ArchetypeChunk{T}" /> 引用（非 null）。</param>
        /// <returns>若成功找到并类型匹配则返回 true，否则返回 false。</returns>
        public bool TryGetArchetypeChunkHead<T>(int index, [NotNullWhen(true)] out ArchetypeChunk<T> component) where T : struct
        {
            component = default!;
            if (index < 0 || index >= ComponentCount)
                return false;

            int columnIndex = ColumnIndices[index];
            return Archetype.TryGetHeadChunk(columnIndex, out component);
        }

        /// <summary>
        /// 将用于缓存列索引的数组归还给 <see cref="ArrayPool{T}.Shared" />。 调用方在不再使用此匹配项时必须调用此方法以避免内存泄漏或数组泄漏。
        /// </summary>
        public void Dispose() => ArrayPool<int>.Shared.Return(ColumnIndices);
    }
}