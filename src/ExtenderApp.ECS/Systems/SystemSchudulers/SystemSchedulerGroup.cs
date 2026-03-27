using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统调度组（主线程管理层）：
    /// - 负责系统注册与生命周期管理；
    /// - 每帧由主线程提交任务到并行调度器；
    /// - 并在主线程等待本帧完成后再继续后续流程。
    /// </summary>
    internal sealed class SystemSchedulerGroup : BaseSystemGroup
    {
        private readonly Dictionary<string, SystemSchedulerGroup> _subGroups;
        private readonly ChunkJobScheduler _parallelScheduler;

        /// <summary>
        /// 初始化系统调度组。
        /// </summary>
        public SystemSchedulerGroup()
        {
            _subGroups = new();
            _parallelScheduler = new();
        }

        /// <summary>
        /// 创建阶段回调：创建当前组系统并递归创建子组。
        /// </summary>
        /// <param name="createContext">创建阶段上下文。</param>
        public override void OnCreate(ref SystemGroupCreateContext createContext)
        {
        }

        /// <summary>
        /// 启动阶段回调：先启动并行调度器，再启动系统与子组。
        /// </summary>
        public override void OnStart()
        {
        }

        /// <summary>
        /// 帧更新回调：主线程提交本组任务并等待完成，然后更新子组。
        /// </summary>
        /// <param name="updateContext">更新阶段上下文。</param>
        public override void OnUpdate(ref SystemGroupUpdateContext updateContext)
        {
        }

        /// <summary>
        /// 停止阶段回调：先停止系统与子组，再停止并行调度器。
        /// </summary>
        public override void OnStop()
        {
        }

        /// <summary>
        /// 销毁阶段回调：停止调度器并销毁系统与子组。
        /// </summary>
        public override void OnDestroy()
        {
        }
    }
}