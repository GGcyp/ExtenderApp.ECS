namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统组接口，表示一组相关系统的集合。系统组可以包含多个系统，并且可以嵌套其他系统组。
    /// </summary>
    public interface ISystemGroup
    {
        /// <summary>
        /// 系统组创建阶段调用：用于初始化长期资源、构建查询或订阅事件。
        ///
        /// 调用约定：由框架在组创建或操作首次注册时调用，通常在主线程执行。若在创建阶段需要并行初始化，应仅调度任务并在组级同步点等待完成。
        /// </summary>
        /// <param name="context">通用的系统组上下文（组层面的容器或标识）。</param>
        /// <param name="createContext">系统创建上下文，包含对当前 World 的访问与其他创建期辅助数据（按 ref 传递以避免拷贝）。</param>
        void OnCreate(ref SystemGroupCreateContext createContext);

        /// <summary>
        /// 系统组启动/启用阶段调用：当操作从不可运行变为可运行时触发，用于执行一次性启动逻辑（例如订阅运行时事件）。
        /// </summary>
        /// <param name="context">通用的系统组上下文。</param>
        void OnStart();

        /// <summary>
        /// 系统组每帧更新阶段调用：执行主要逻辑或调度并行任务。
        ///
        /// 说明：
        /// - 此方法接收一个 <see cref=""/> 的引用（通过 ref 传递），便于在不产生额外分配的情况下向操作提供运行时快照（如 DeltaTime、Time、FrameIndex、CancellationToken 等）;
        /// - 在该方法内，操作应尽量采用非阻塞方式或调度并行任务；若需要对 World 进行结构性修改，应将修改记录到命令缓冲并由主线程回放，避免直接在工作线程修改底层数据结构;
        /// - 可在 updateContext 中登记并行任务或通过外部并行系统机制返回任务句柄，组会在合适时机统一等待并回放命令缓冲。
        /// </summary>
        /// <param name="context">通用的系统组上下文。</param>
        /// <param name="updateContext">更新时的运行时上下文，包含时间和取消令牌等信息（按 ref 传入以避免拷贝）。</param>
        void OnUpdate(ref SystemGroupUpdateContext updateContext);

        /// <summary>
        /// 系统组停止/禁用阶段调用：当操作被停用或从组中移除时触发，用于取消订阅或释放短期资源。
        /// </summary>
        /// <param name="context">通用的系统组上下文。</param>
        void OnStop();

        /// <summary>
        /// 系统组销毁阶段调用：在系统组被销毁或 World 清理时调用，用于释放长期资源与做最终清理。
        /// </summary>
        /// <param name="context">通用的系统组上下文。</param>
        void OnDestroy();
    }
}