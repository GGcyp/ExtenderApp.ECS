using ExtenderApp.ECS.Systems;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 并行系统接口：系统会在并行任务中分片执行，接收 <see cref="ParallelSystemState"/>。
    /// 系统实现需实现 <see cref="IChunkJob"/> 以提供对单个块（chunk/ChunkList）工作的封装。
    /// </summary>
    /// <typeparam name="T">系统类型本身（struct）</typeparam>
    public interface IParallelSystem<T> : ISystem
    {
        void OnCreate(ref SystemUpdateContext context);

        void OnDestroy(ref SystemUpdateContext context);

        void OnUpdate(ref SystemUpdateContext context);
    }
}