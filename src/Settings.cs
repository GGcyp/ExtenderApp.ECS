namespace ExtenderApp.ECS
{
    /// <summary>
    /// Ecs全局设置
    /// </summary>
    internal static class Settings
    {
        #region Archetype Settings

        /// <summary>
        /// 列块列表的默认初始容量。
        /// </summary>
        internal const int DefaultArchetypeChunkListSize = 16;

        /// <summary>
        /// 超出预设增长表后的固定段容量。
        /// </summary>
        internal const int FixedArchetypeChunkSegmentSize = 2048;

        /// <summary>
        /// 预设段容量增长表。 在索引范围内按该数组递增，超出后使用 <see cref="FixedArchetypeChunkSegmentSize" />。
        /// </summary>
        private static readonly int[] ArchetypeChunkSizeArray = { 16, 32, 64, 128, 256, 512, 1024 };

        /// <summary>
        /// 获取预设段容量数组的只读视图。 在索引范围内按该数组递增，超出后使用 <see cref="FixedArchetypeChunkSegmentSize" />。
        /// </summary>
        internal static ReadOnlySpan<int> PresetArchetypeChunkSizeArray => ArchetypeChunkSizeArray;

        /// <summary>
        /// 预设段容量数组的长度。
        /// </summary>
        internal static readonly int PresetArchetypeChunkSizeLength = ArchetypeChunkSizeArray.Length;

        /// <summary>
        /// 所有预设段容量的总和。 该值用于计算在预设范围内可以容纳的最大实体数量，以优化内存分配和性能。
        /// </summary>
        internal static readonly int AllPresetArchetypeChunkSizeLength = GetAllPresetArchetypeChunkSize();

        /// <summary>
        /// 按段序号获取下一段容量。
        /// </summary>
        /// <param name="index"> 段序号。 </param>
        /// <returns> 预设容量或固定容量。 </returns>
        internal static int GetNextArchetypeChunkSize(int index) => index < ArchetypeChunkSizeArray.Length ? ArchetypeChunkSizeArray[index] : FixedArchetypeChunkSegmentSize;

        /// <summary>
        /// 按段序号获取上一段容量。
        /// </summary>
        /// <param name="index"> 段序号。 </param>
        /// <returns> 上一段的预设容量或固定容量。 </returns>
        internal static int GetPreviousArchetypeChunkSize(int index) => index < ArchetypeChunkSizeArray.Length ? ArchetypeChunkSizeArray[index] : FixedArchetypeChunkSegmentSize;

        /// <summary>
        /// 获取是否可以移除空段的条件。 当段数为0时允许移除；当段数在预设范围内时，允许移除小于1024的段；当段数超过预设范围时，允许移除小于固定容量的一半的段。
        /// </summary>
        /// <param name="chunkCount"> 块数 </param>
        /// <param name="count"> 当前段的容量 </param>
        /// <returns> 是否可以移除空段 </returns>
        internal static bool TryCanRemoveEmptySegmentsSize(int chunkCount, int count)
        {
            if (chunkCount <= 0)
                return true;

            if (chunkCount <= PresetArchetypeChunkSizeLength)
                return count <= 1024;
            else
                return count <= AllPresetArchetypeChunkSizeLength + (FixedArchetypeChunkSegmentSize * (chunkCount - PresetArchetypeChunkSizeLength));
        }

        /// <summary>
        /// 获取所有预设段容量的总和。 该值用于计算在预设范围内可以容纳的最大实体数量，以优化内存分配和性能。
        /// </summary>
        /// <returns> 所有预设段容量的总和 </returns>
        internal static int GetAllPresetArchetypeChunkSize()
        {
            int sum = 0;
            foreach (var size in ArchetypeChunkSizeArray)
            {
                sum += size;
            }
            return sum;
        }

        #endregion Archetype Settings

        #region Parallel System Settings

        /// <summary>
        /// 获取系统可并行执行的最大线程数，默认为处理器核心数。 该值用于限制并行系统的线程池大小，以避免过度线程切换和资源竞争。
        /// </summary>
        public static readonly int MaxParallelProcessorCount = Environment.ProcessorCount - 1;

        #endregion Parallel System Settings
    }
}