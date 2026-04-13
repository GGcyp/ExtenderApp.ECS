using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Systems.Parallels
{
    /// <summary>
    /// 并行系统扩展工具类，用于将实体查询核心展开为并行作业。
    /// </summary>
    internal static class ParallelSystemsExpansion
    {
        /// <summary>
        /// 预设合并段数量
        /// </summary>
        private static int PresetMergedSegmentCount => Settings.PresetArchetypeChunkSizeLength;

        #region Alignment helpers

        /// <summary>
        /// 检查单列是否与实体对齐，并获取对应块列表和总数
        /// </summary>
        /// <typeparam name="TCol">组件类型</typeparam>
        /// <param name="archetype">原型对象</param>
        /// <param name="chunkList">输出：块列表</param>
        /// <param name="total">输出：总数</param>
        private static bool TryGetAligned1<TCol>(Archetype archetype, out ArchetypeChunkList<TCol>? chunkList, out int total)
        {
            chunkList = null;
            total = 0;
            if (!archetype.ComponentMask.TryGetEncodedPosition<TCol>(out var col) ||
                !archetype.TryGetChunkList(col, out chunkList) ||
                chunkList == null)
                return false;
            total = chunkList.Count;
            return total == archetype.Entities.Count;
        }

        /// <summary>
        /// 检查两列是否与实体对齐，并获取对应块列表和总数
        /// </summary>
        /// <typeparam name="TA">第一个组件类型</typeparam>
        /// <typeparam name="TB">第二个组件类型</typeparam>
        /// <param name="archetype">原型对象</param>
        /// <param name="l1">输出：第一个块列表</param>
        /// <param name="l2">输出：第二个块列表</param>
        /// <param name="total">输出：总数</param>
        private static bool TryGetAligned2<TA, TB>(Archetype archetype, out ArchetypeChunkList<TA>? l1, out ArchetypeChunkList<TB>? l2, out int total)
        {
            l1 = null;
            l2 = null;
            total = 0;
            if (!TryGetAligned1(archetype, out l1, out total))
                return false;
            if (!archetype.ComponentMask.TryGetEncodedPosition<TB>(out var c2) ||
                !archetype.TryGetChunkList(c2, out l2) ||
                l2 == null ||
                l2.Count != total)
                return false;
            return true;
        }

        /// <summary>
        /// 检查三列是否与实体对齐，并获取对应块列表和总数
        /// </summary>
        /// <typeparam name="TA">第一个组件类型</typeparam>
        /// <typeparam name="TB">第二个组件类型</typeparam>
        /// <typeparam name="TC">第三个组件类型</typeparam>
        /// <param name="archetype">原型对象</param>
        /// <param name="l1">输出：第一个块列表</param>
        /// <param name="l2">输出：第二个块列表</param>
        /// <param name="l3">输出：第三个块列表</param>
        /// <param name="total">输出：总数</param>
        private static bool TryGetAligned3<TA, TB, TC>(Archetype archetype, out ArchetypeChunkList<TA>? l1, out ArchetypeChunkList<TB>? l2, out ArchetypeChunkList<TC>? l3, out int total)
        {
            l1 = null;
            l2 = null;
            l3 = null;
            total = 0;
            if (!TryGetAligned2(archetype, out l1, out l2, out total))
                return false;
            if (!archetype.ComponentMask.TryGetEncodedPosition<TC>(out var c3) ||
                !archetype.TryGetChunkList(c3, out l3) ||
                l3 == null ||
                l3.Count != total)
                return false;
            return true;
        }

        /// <summary>
        /// 检查四列是否与实体对齐，并获取对应块列表和总数
        /// </summary>
        /// <typeparam name="TA">第一个组件类型</typeparam>
        /// <typeparam name="TB">第二个组件类型</typeparam>
        /// <typeparam name="TC">第三个组件类型</typeparam>
        /// <typeparam name="TD">第四个组件类型</typeparam>
        /// <param name="archetype">原型对象</param>
        /// <param name="l1">输出：第一个块列表</param>
        /// <param name="l2">输出：第二个块列表</param>
        /// <param name="l3">输出：第三个块列表</param>
        /// <param name="l4">输出：第四个块列表</param>
        /// <param name="total">输出：总数</param>
        private static bool TryGetAligned4<TA, TB, TC, TD>(Archetype archetype, out ArchetypeChunkList<TA>? l1, out ArchetypeChunkList<TB>? l2, out ArchetypeChunkList<TC>? l3, out ArchetypeChunkList<TD>? l4, out int total)
        {
            l1 = null;
            l2 = null;
            l3 = null;
            l4 = null;
            total = 0;
            if (!TryGetAligned3(archetype, out l1, out l2, out l3, out total))
                return false;
            if (!archetype.ComponentMask.TryGetEncodedPosition<TD>(out var c4) ||
                !archetype.TryGetChunkList(c4, out l4) ||
                l4 == null ||
                l4.Count != total)
                return false;
            return true;
        }

        /// <summary>
        /// 检查五列是否与实体对齐，并获取对应块列表和总数
        /// </summary>
        /// <typeparam name="TA">第一个组件类型</typeparam>
        /// <typeparam name="TB">第二个组件类型</typeparam>
        /// <typeparam name="TC">第三个组件类型</typeparam>
        /// <typeparam name="TD">第四个组件类型</typeparam>
        /// <typeparam name="TE">第五个组件类型</typeparam>
        /// <param name="archetype">原型对象</param>
        /// <param name="l1">输出：第一个块列表</param>
        /// <param name="l2">输出：第二个块列表</param>
        /// <param name="l3">输出：第三个块列表</param>
        /// <param name="l4">输出：第四个块列表</param>
        /// <param name="l5">输出：第五个块列表</param>
        /// <param name="total">输出：总数</param>
        private static bool TryGetAligned5<TA, TB, TC, TD, TE>(Archetype archetype, out ArchetypeChunkList<TA>? l1, out ArchetypeChunkList<TB>? l2, out ArchetypeChunkList<TC>? l3, out ArchetypeChunkList<TD>? l4, out ArchetypeChunkList<TE>? l5, out int total)
        {
            l1 = null;
            l2 = null;
            l3 = null;
            l4 = null;
            l5 = null;
            total = 0;
            if (!TryGetAligned4(archetype, out l1, out l2, out l3, out l4, out total))
                return false;
            if (!archetype.ComponentMask.TryGetEncodedPosition<TE>(out var c5) ||
                !archetype.TryGetChunkList(c5, out l5) ||
                l5 == null ||
                l5.Count != total)
                return false;
            return true;
        }

        #endregion Alignment helpers

        /// <summary>
        /// 为没有参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                var entities = archetype.Entities;
                int total = entities.Count;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryEntitiesOnlyFull(archetype), systemUpdateContext, system));
                    continue;
                }

                int preset = PresetMergedSegmentCount;
                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryEntitiesOnlySlice(entities, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryEntitiesOnlySlice(entities, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryEntitiesOnlySlice(entities, i, 1), systemUpdateContext, system));
            }
        }

        /// <summary>
        /// 为带一个参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem, T1>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem<T1>
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem, T1>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                if (!TryGetAligned1<T1>(archetype, out var chunkList, out var total))
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT1FallbackFull<T1>(archetype), systemUpdateContext, system));
                    continue;
                }

                var entities = archetype.Entities;
                int preset = PresetMergedSegmentCount;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT1FallbackFull<T1>(archetype), systemUpdateContext, system));
                    continue;
                }

                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT1AlignedSlice(entities, chunkList!, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT1AlignedSlice(entities, chunkList!, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT1AlignedSlice(entities, chunkList!, i, 1), systemUpdateContext, system));
            }
        }

        /// <summary>
        /// 为带两个参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <typeparam name="T2">第二个参数类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem, T1, T2>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem<T1, T2>
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem, T1, T2>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                if (!TryGetAligned2<T1, T2>(archetype, out var l1, out var l2, out var total))
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT2FallbackFull<T1, T2>(archetype), systemUpdateContext, system));
                    continue;
                }

                var entities = archetype.Entities;
                int preset = PresetMergedSegmentCount;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT2FallbackFull<T1, T2>(archetype), systemUpdateContext, system));
                    continue;
                }

                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT2AlignedSlice(entities, l1!, l2!, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT2AlignedSlice(entities, l1!, l2!, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT2AlignedSlice(entities, l1!, l2!, i, 1), systemUpdateContext, system));
            }
        }

        /// <summary>
        /// 为带三个参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <typeparam name="T2">第二个参数类型</typeparam>
        /// <typeparam name="T3">第三个参数类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem, T1, T2, T3>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem<T1, T2, T3>
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem, T1, T2, T3>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                if (!TryGetAligned3<T1, T2, T3>(archetype, out var l1, out var l2, out var l3, out var total))
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT3FallbackFull<T1, T2, T3>(archetype), systemUpdateContext, system));
                    continue;
                }

                var entities = archetype.Entities;
                int preset = PresetMergedSegmentCount;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT3FallbackFull<T1, T2, T3>(archetype), systemUpdateContext, system));
                    continue;
                }

                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT3AlignedSlice(entities, l1!, l2!, l3!, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT3AlignedSlice(entities, l1!, l2!, l3!, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT3AlignedSlice(entities, l1!, l2!, l3!, i, 1), systemUpdateContext, system));
            }
        }

        /// <summary>
        /// 为带四个参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <typeparam name="T2">第二个参数类型</typeparam>
        /// <typeparam name="T3">第三个参数类型</typeparam>
        /// <typeparam name="T4">第四个参数类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem, T1, T2, T3, T4>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem, T1, T2, T3, T4>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                if (!TryGetAligned4<T1, T2, T3, T4>(archetype, out var l1, out var l2, out var l3, out var l4, out var total))
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT4FallbackFull<T1, T2, T3, T4>(archetype), systemUpdateContext, system));
                    continue;
                }

                var entities = archetype.Entities;
                int preset = PresetMergedSegmentCount;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT4FallbackFull<T1, T2, T3, T4>(archetype), systemUpdateContext, system));
                    continue;
                }

                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT4AlignedSlice(entities, l1!, l2!, l3!, l4!, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT4AlignedSlice(entities, l1!, l2!, l3!, l4!, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT4AlignedSlice(entities, l1!, l2!, l3!, l4!, i, 1), systemUpdateContext, system));
            }
        }

        /// <summary>
        /// 为带五个参数的并行系统添加工作项
        /// </summary>
        /// <typeparam name="TSystem">并行系统类型</typeparam>
        /// <typeparam name="T1">第一个参数类型</typeparam>
        /// <typeparam name="T2">第二个参数类型</typeparam>
        /// <typeparam name="T3">第三个参数类型</typeparam>
        /// <typeparam name="T4">第四个参数类型</typeparam>
        /// <typeparam name="T5">第五个参数类型</typeparam>
        /// <param name="manager">并行作业调度器</param>
        /// <param name="queryCore">实体查询核心</param>
        /// <param name="systemUpdateContext">系统更新上下文</param>
        /// <param name="system">系统实例</param>
        public static void AddWorkItem<TSystem, T1, T2, T3, T4, T5>(this ParallelJobManager manager, EntityQueryCore queryCore, SystemUpdateContext systemUpdateContext, TSystem system = default)
            where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
        {
            var head = queryCore.GetArchetypeSegmentHead();
            if (head == null)
                return;

            var provider = ParallelSystemProvider.Get<TSystem, T1, T2, T3, T4, T5>();

            foreach (var archetypeSegment in head)
            {
                var archetype = archetypeSegment.Archetype;
                if (!TryGetAligned5<T1, T2, T3, T4, T5>(archetype, out var l1, out var l2, out var l3, out var l4, out var l5, out var total))
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT5FallbackFull<T1, T2, T3, T4, T5>(archetype), systemUpdateContext, system));
                    continue;
                }

                var entities = archetype.Entities;
                int preset = PresetMergedSegmentCount;
                if (total == 0)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT5FallbackFull<T1, T2, T3, T4, T5>(archetype), systemUpdateContext, system));
                    continue;
                }

                if (total <= preset)
                {
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT5AlignedSlice(entities, l1!, l2!, l3!, l4!, l5!, 0, total), systemUpdateContext, system));
                    continue;
                }

                manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT5AlignedSlice(entities, l1!, l2!, l3!, l4!, l5!, 0, preset), systemUpdateContext, system));
                for (int i = preset; i < total; i++)
                    manager.AddWorkItem(provider.Rent(ParallelJobQueryBuilders.CreateJobQueryT5AlignedSlice(entities, l1!, l2!, l3!, l4!, l5!, i, 1), systemUpdateContext, system));
            }
        }
    }
}