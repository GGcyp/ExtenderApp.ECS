namespace ExtenderApp.ECS
{
    /// <summary>
    /// 控制 <see cref="World" /> 构造时的可选行为（并行调度、后续可扩展其它开关）。
    /// </summary>
    public sealed class WorldOptions
    {
        /// <summary>
        /// 默认选项：与历史行为一致（每个 World 启用后台并行 worker）。
        /// </summary>
        public static WorldOptions Default { get; } = new();

        /// <summary>
        /// 轻量 World：不创建并行作业后台线程，适合单元测试或极小场景。 此时若调用 <see cref="Abstract.SystemUpdateContext" /> 上的并行调度 API，将抛出 <see
        /// cref="System.InvalidOperationException" />。
        /// </summary>
        public static WorldOptions Lightweight { get; } = new() { ParallelJobs = WorldParallelJobsMode.Disabled };

        /// <summary>
        /// 是否为该 World 创建并常驻 <see cref="Systems.ParallelJobManager" /> 的后台 worker。
        /// </summary>
        public WorldParallelJobsMode ParallelJobs { get; init; } = WorldParallelJobsMode.PerWorldWorkers;
    }

    /// <summary>
    /// World 级并行作业策略。全局共享池可在后续版本扩展，当前仅区分「每 World」与「关闭」。
    /// </summary>
    public enum WorldParallelJobsMode
    {
        /// <summary>
        /// 不启动后台 worker，避免小 World 的线程与调度开销。
        /// </summary>
        Disabled,

        /// <summary>
        /// 每个 World 独立一组 worker（与改造前行为一致）。
        /// </summary>
        PerWorldWorkers,
    }
}