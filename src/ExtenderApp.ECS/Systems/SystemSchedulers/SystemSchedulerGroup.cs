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
        /// <summary>
        /// 系统调度上下文列表
        /// </summary>
        private readonly List<SystemSchedulerContext> _groups;
        /// <summary>
        /// 并行作业调度器
        /// </summary>
        private ParallelJobManager parallelScheduler;
        /// <summary>
        /// 世界对象
        /// </summary>
        private World world;
        /// <summary>
        /// 调度组名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 构造函数，初始化调度组名称
        /// </summary>
        /// <param name="name">调度组名称</param>
        public SystemSchedulerGroup(string name)
        {
            _groups = new();
            parallelScheduler = default!;
            world = default!;
            Name = name;
        }

        /// <summary>
        /// 添加系统到调度组
        /// </summary>
        /// <param name="system">系统调度上下文</param>
        public void AddSystem(SystemSchedulerContext system)
        {
            _groups.Add(system);
            if (world is null)
                return;

            system.SetJobManager(parallelScheduler);
            SystemCreateContext systemCreateContext = new(world);
            system.OnCreate(ref systemCreateContext);
            system.OnStart();
        }

        /// <summary>
        /// 调度组创建时调用，初始化所有系统
        /// </summary>
        /// <param name="createContext">系统组创建上下文</param>
        public override void OnCreate(ref SystemGroupCreateContext createContext)
        {
            SystemCreateContext systemCreateContext = new(createContext.CurrentWorld);
            foreach (var group in _groups)
            {
                group.OnCreate(ref systemCreateContext);
            }

            world = createContext.CurrentWorld;
            parallelScheduler = world.PJManager;
            foreach (var group in _groups)
            {
                group.SetJobManager(parallelScheduler);
            }
        }

        /// <summary>
        /// 泛型添加系统到调度组
        /// </summary>
        /// <typeparam name="TSystem">系统类型</typeparam>
        /// <param name="system">系统实例</param>
        public void AddSystem<TSystem>(TSystem system) where TSystem : struct, ISystem
        {
            var ctx = new SystemSchedulerContext<TSystem>(system, typeof(TSystem).Name, parallelScheduler);
            _groups.Add(ctx);
        }

        /// <summary>
        /// 泛型添加系统到调度组（自定义名称）
        /// </summary>
        /// <typeparam name="TSystem">系统类型</typeparam>
        /// <param name="system">系统实例</param>
        /// <param name="name">系统名称</param>
        public void AddSystem<TSystem>(TSystem system, string name) where TSystem : struct, ISystem
        {
            var ctx = new SystemSchedulerContext<TSystem>(system, name, parallelScheduler);
            _groups.Add(ctx);
        }

        /// <summary>
        /// 调度组开始时调用
        /// </summary>
        public override void OnStart()
        {
            foreach (var group in _groups)
            {
                group.OnStart();
            }
        }

        /// <summary>
        /// 调度组更新时间
        /// </summary>
        /// <param name="updateContext">系统组更新时间上下文</param>
        public override void OnUpdate(ref SystemGroupUpdateContext updateContext)
        {
            SystemUpdateContext systemUpdateContext = new(world.SCManager,
                world.EQManager,
                world.CommandBuffer,
                parallelScheduler,
                updateContext.DeltaTime,
                updateContext.Time,
                updateContext.FrameIndex);
            foreach (var group in _groups)
            {
                group.OnUpdate(ref systemUpdateContext);
            }

            parallelScheduler.WaitUntilJobsCompleted();
        }

        /// <summary>
        /// 调度组停止时调用
        /// </summary>
        public override void OnStop()
        {
            foreach (var group in _groups)
            {
                group.OnStop();
            }
        }

        /// <summary>
        /// 调度组销毁时调用
        /// </summary>
        public override void OnDestroy()
        {
            foreach (var group in _groups)
            {
                group.OnDestroy();
            }
        }
    }
}
