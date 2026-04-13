namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统创建阶段传入的上下文结构体（只读）。
    ///
    /// 语义与使用建议：
    /// - 在系统注册或首次启用时由框架创建并传入系统的 OnCreate 方法；通常在主线程上调用；
    /// - 提供对当前运行的 World 的只读访问入口，允许系统在创建阶段构建查询、注册资源或缓存对 World 的引用；
    /// - 该类型是轻量的只读值类型，按值或按 ref 传递均安全；不建议将此上下文保存为长期状态，系统应在 OnCreate 中将需要的引用（例如查询或句柄）另行缓存；
    /// - 若在创建逻辑中需要进行异步或并行初始化，应仅调度任务并在组级别的同步点等待其完成，避免在 OnCreate 内执行长时阻塞操作。
    /// </summary>
    public readonly ref struct SystemGroupCreateContext
    {
        /// <summary>
        /// 当前被调度的 World 实例。请注意：对 World 的写操作通常要求在主线程执行，具体 API 的线程安全性请参考对应方法文档。
        /// </summary>
        public readonly World CurrentWorld;

        /// <summary>
        /// 创建新的 SystemCreateContext 实例。
        /// </summary>
        /// <param name="world">当前 World 实例（不得为 null）。</param>
        public SystemGroupCreateContext(World world)
        {
            CurrentWorld = world;
        }
    }
}