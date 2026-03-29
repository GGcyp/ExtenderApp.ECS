using ExtenderApp.ECS;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Systems.Parallels
{
    /// <summary>
    /// 集中创建并行作业使用的 <see cref="ChunkEntityEnumerator"/>、各组件列 <see cref="ChunkEnumerator{T}"/> 以及 <see cref="JobEntityQuery"/>，
    /// 与 <see cref="ParallelSystemsExpansion"/> 中的分段策略解耦，便于维护。
    /// </summary>
    internal static class ParallelJobQueryBuilders
    {
        private const ulong ChunkAccessVersion = 0;

        #region ChunkEntityEnumerator

        /// <summary>
        /// 遍历指定原型下的全部实体段（无列切片）。
        /// </summary>
        internal static ChunkEntityEnumerator CreateChunkEntityEnumeratorFullArchetype(Archetype archetype)
            => new(new ChunkEntityAccessorEnumerator(archetype));

        /// <summary>
        /// 在实体段列表上按 <paramref name="startSegmentIndex"/> 起连续 <paramref name="segmentSpan"/> 段做切片，与列块列表下标对齐时使用。
        /// </summary>
        internal static ChunkEntityEnumerator CreateChunkEntityEnumeratorSegmentSlice(
            ArchetypeEntitySegmentInfoList entities,
            int startSegmentIndex,
            int segmentSpan)
            => new(new ChunkEntityAccessorEnumerator(entities, startSegmentIndex, segmentSpan));

        #endregion

        #region ChunkEnumerator（单列）

        /// <summary>
        /// 从原型解析列并遍历该列全部块（无列表切片）。
        /// </summary>
        internal static ChunkEnumerator<T> CreateChunkEnumeratorColumnFull<T>(Archetype archetype)
            => new(new ChunkAccessorEnumerator<T>(archetype, ChunkAccessVersion));

        /// <summary>
        /// 在已解析的列块列表上按 <paramref name="startChunkIndex"/> 起连续 <paramref name="chunkSpan"/> 个列表项切片。
        /// </summary>
        internal static ChunkEnumerator<T> CreateChunkEnumeratorColumnSlice<T>(
            ArchetypeChunkList<T> chunkList,
            int startChunkIndex,
            int chunkSpan)
            => new(new ChunkAccessorEnumerator<T>(chunkList, ChunkAccessVersion, startChunkIndex, chunkSpan));

        #endregion

        #region JobEntityQuery（仅实体）

        internal static JobEntityQuery CreateJobQueryEntitiesOnlyFull(Archetype archetype)
            => new(CreateChunkEntityEnumeratorFullArchetype(archetype));

        internal static JobEntityQuery CreateJobQueryEntitiesOnlySlice(
            ArchetypeEntitySegmentInfoList entities,
            int startSegmentIndex,
            int segmentSpan)
            => new(CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan));

        #endregion

        #region JobEntityQuery T1

        internal static JobEntityQuery<T1> CreateJobQueryT1FallbackFull<T1>(Archetype archetype)
            => new(CreateChunkEntityEnumeratorFullArchetype(archetype), CreateChunkEnumeratorColumnFull<T1>(archetype));

        internal static JobEntityQuery<T1> CreateJobQueryT1AlignedSlice<T1>(
            ArchetypeEntitySegmentInfoList entities,
            ArchetypeChunkList<T1> chunkList,
            int startSegmentIndex,
            int segmentSpan)
            => new(
                CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(chunkList, startSegmentIndex, segmentSpan));

        #endregion

        #region JobEntityQuery T1 T2

        internal static JobEntityQuery<T1, T2> CreateJobQueryT2FallbackFull<T1, T2>(Archetype archetype)
            => new(
                CreateChunkEntityEnumeratorFullArchetype(archetype),
                CreateChunkEnumeratorColumnFull<T1>(archetype),
                CreateChunkEnumeratorColumnFull<T2>(archetype));

        internal static JobEntityQuery<T1, T2> CreateJobQueryT2AlignedSlice<T1, T2>(
            ArchetypeEntitySegmentInfoList entities,
            ArchetypeChunkList<T1> list1,
            ArchetypeChunkList<T2> list2,
            int startSegmentIndex,
            int segmentSpan)
            => new(
                CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list1, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list2, startSegmentIndex, segmentSpan));

        #endregion

        #region JobEntityQuery T1 T2 T3

        internal static JobEntityQuery<T1, T2, T3> CreateJobQueryT3FallbackFull<T1, T2, T3>(Archetype archetype)
            => new(
                CreateChunkEntityEnumeratorFullArchetype(archetype),
                CreateChunkEnumeratorColumnFull<T1>(archetype),
                CreateChunkEnumeratorColumnFull<T2>(archetype),
                CreateChunkEnumeratorColumnFull<T3>(archetype));

        internal static JobEntityQuery<T1, T2, T3> CreateJobQueryT3AlignedSlice<T1, T2, T3>(
            ArchetypeEntitySegmentInfoList entities,
            ArchetypeChunkList<T1> list1,
            ArchetypeChunkList<T2> list2,
            ArchetypeChunkList<T3> list3,
            int startSegmentIndex,
            int segmentSpan)
            => new(
                CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list1, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list2, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list3, startSegmentIndex, segmentSpan));

        #endregion

        #region JobEntityQuery T1 T2 T3 T4

        internal static JobEntityQuery<T1, T2, T3, T4> CreateJobQueryT4FallbackFull<T1, T2, T3, T4>(Archetype archetype)
            => new(
                CreateChunkEntityEnumeratorFullArchetype(archetype),
                CreateChunkEnumeratorColumnFull<T1>(archetype),
                CreateChunkEnumeratorColumnFull<T2>(archetype),
                CreateChunkEnumeratorColumnFull<T3>(archetype),
                CreateChunkEnumeratorColumnFull<T4>(archetype));

        internal static JobEntityQuery<T1, T2, T3, T4> CreateJobQueryT4AlignedSlice<T1, T2, T3, T4>(
            ArchetypeEntitySegmentInfoList entities,
            ArchetypeChunkList<T1> list1,
            ArchetypeChunkList<T2> list2,
            ArchetypeChunkList<T3> list3,
            ArchetypeChunkList<T4> list4,
            int startSegmentIndex,
            int segmentSpan)
            => new(
                CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list1, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list2, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list3, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list4, startSegmentIndex, segmentSpan));

        #endregion

        #region JobEntityQuery T1 T2 T3 T4 T5

        internal static JobEntityQuery<T1, T2, T3, T4, T5> CreateJobQueryT5FallbackFull<T1, T2, T3, T4, T5>(Archetype archetype)
            => new(
                CreateChunkEntityEnumeratorFullArchetype(archetype),
                CreateChunkEnumeratorColumnFull<T1>(archetype),
                CreateChunkEnumeratorColumnFull<T2>(archetype),
                CreateChunkEnumeratorColumnFull<T3>(archetype),
                CreateChunkEnumeratorColumnFull<T4>(archetype),
                CreateChunkEnumeratorColumnFull<T5>(archetype));

        internal static JobEntityQuery<T1, T2, T3, T4, T5> CreateJobQueryT5AlignedSlice<T1, T2, T3, T4, T5>(
            ArchetypeEntitySegmentInfoList entities,
            ArchetypeChunkList<T1> list1,
            ArchetypeChunkList<T2> list2,
            ArchetypeChunkList<T3> list3,
            ArchetypeChunkList<T4> list4,
            ArchetypeChunkList<T5> list5,
            int startSegmentIndex,
            int segmentSpan)
            => new(
                CreateChunkEntityEnumeratorSegmentSlice(entities, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list1, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list2, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list3, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list4, startSegmentIndex, segmentSpan),
                CreateChunkEnumeratorColumnSlice(list5, startSegmentIndex, segmentSpan));

        #endregion
    }
}
