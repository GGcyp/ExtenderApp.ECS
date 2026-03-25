namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 代表对单个块的并行作业接口（行级/块级并行单元）。
    /// 系统并行实现应提供一个 IChunkJob 的实例用于在各个块上并行执行。
    /// </summary>
    public interface IChunkJob
    {
        /// <summary>
        /// 在单个数据块上执行工作。此方法将在并行任务中被多次调用（不同块）。
        /// </summary>
        /// <param name="chunkIndex">块索引（在当前匹配 Archetype 的块列表中的索引）。</param>
        /// <param name="state">并行系统的上下文状态（只读/只写视图由实现方保证）。</param>
        void Execute(int chunkIndex);
    }
}