namespace ExtenderApp.ECS.Worlds
{
    /// <summary>
    /// CurrentWorld 结构版本管理器。 用于跟踪世界级实体结构（组件集合/实体集合）变更版本， 以便查询或缓存系统在检测到版本变化时执行失效与重建，以及块级版本盖戳。
    /// </summary>
    internal class StructureVersionManager
    {
        /// <summary>
        /// CurrentWorld 的全局结构变更版本号。每次结构变化（实体创建、销毁、组件或关系增删导致的 Archetype 迁移）通过 <see cref="IncrementStructureVersion" /> 递增；块级打点把「当时的版本号」 写入
        /// <c>ArchetypeChunk.Version</c>，查询侧的 SkipUnchanged 据此跳过未变化块。 纯组件数据写入（SetComponent）只盖戳不递增；RefRW 等裸写不参与。 底层使用 long 存储以复用 <see
        /// cref="Interlocked.Add(ref long, long)" /> 的原子批量递增。
        /// </summary>
        private long _structureVersion;

        /// <summary>
        /// 获取 CurrentWorld 的全局结构变更版本号（只读快照，无符号视图）。
        /// </summary>
        public ulong StructureVersion => (ulong)Volatile.Read(ref _structureVersion);

        /// <summary>
        /// 递增 CurrentWorld 的全局结构变更版本号。默认每次递增 1；批量结构变化可一次传入实际发生变化的数量。 使用原子操作以保证命令回放等可能的多线程写入场景下的安全性。
        /// </summary>
        /// <param name="count">本次递增的步长（结构变化数量）。</param>
        public void IncrementStructureVersion(int count = 1)
        {
            Interlocked.Add(ref _structureVersion, count);
        }
    }
}