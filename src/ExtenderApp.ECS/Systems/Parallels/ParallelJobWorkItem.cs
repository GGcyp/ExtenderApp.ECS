namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 多线程并行任务项（ParallelJobWorkItem）：每个并行任务的输入数据和执行逻辑封装在一个 ParallelJobWorkItem 实例中，由调度器分发到工作线程执行。
    /// </summary>
    internal abstract class ParallelJobWorkItem
    {
        /// <summary>
        /// 获取当前 ParallelJobWorkItem 是否处于激活状态。
        /// </summary>
        public bool IsActive { get; protected set; }

        /// <summary>
        /// 执行并行任务的核心方法。
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// 回收当前 ParallelJobCache 实例所占用的资源。
        /// </summary>
        public abstract void Retrun();
    }
}