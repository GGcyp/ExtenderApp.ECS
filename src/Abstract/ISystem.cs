using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统基础接口（生命周期钩子）。
    ///
    /// 语义：实现该接口的类型表示一个可被世界（World）调度的系统。系统应只包含业务逻辑或调度作业的代码， 不应直接承担跨系统的同步或回放命令缓冲的职责。生命周期方法由框架在适当的线程/阶段调用， 并通过上下文对象传入运行时所需的最小资源（例如创建时的服务、更新时的时间信息与命令缓冲等）。
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 系统创建阶段调用（主线程）：用于初始化长期资源、构建查询与注册依赖。 注意：该方法在主线程执行；若需要并行准备，应只调度任务并通过组级同步在后续阶段等待其完成。
        /// </summary>
        /// <param name="createContext">系统创建时传入的上下文，按 ref 传递以避免拷贝开销。</param>
        void OnCreate(ref SystemCreateContext createContext);

        /// <summary>
        /// 系统启用/开始调用（可用于在系统激活时执行一次性逻辑）。
        /// </summary>
        void OnStart();

        /// <summary>
        /// 每帧更新入口：在该方法中执行系统的核心逻辑或调度并行作业。
        ///
        /// 说明：
        /// - updateContext 通常包含时间（DeltaTime/Time）、帧索引、对 World 或命令缓冲的安全访问入口等；
        /// - 并行系统应避免直接进行结构性修改，而是将修改写入命令缓冲并在组边界由主线程回放。
        /// - 调用方会根据系统组策略决定是否并行调用或按顺序调用该方法。
        /// </summary>
        /// <param name="updateContext">每帧更新时传入的上下文，按 ref 传入以避免值拷贝。</param>
        void OnUpdate(ref SystemUpdateContext updateContext);

        /// <summary>
        /// 系统停用/停止调用（可用于释放短期资源或取消订阅）。
        /// </summary>
        void OnStop();

        /// <summary>
        /// 系统销毁阶段调用（主线程）：用于释放长期资源与执行最终清理。 注意：释放涉及主线程资源的操作应在该方法内完成。
        /// </summary>
        void OnDestroy();
    }
}