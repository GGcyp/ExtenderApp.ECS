using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统缓存抽象基类（框架内部使用）。
    /// </summary>
    internal abstract class SystemSchedulerContext : ISystem
    {
        /// <summary>
        /// 并行作业管理器引用，供子类在需要时调度并行任务。支持在宿主组创建阶段后注入。
        /// </summary>
        protected ParallelJobManager JobManager { get; private set; }

        /// <summary>
        /// 系统名称，用于调试与日志输出。
        /// </summary>
        internal string Name { get; }

        protected SystemSchedulerContext(string name, ParallelJobManager? jobManager = null)
        {
            Name = name;
            JobManager = jobManager!;
        }

        /// <summary>
        /// 在宿主组的 OnCreate 阶段注入实际的 <see cref="ParallelJobManager"/> 实例。
        /// </summary>
        internal void SetJobManager(ParallelJobManager jobManager) => JobManager = jobManager;

        public void OnCreate(ref SystemCreateContext createContext)
            => OnCreateProtected(ref createContext);

        public void OnStart()
            => OnStartProtected();

        public void OnUpdate(ref SystemUpdateContext updateContext)
            => OnUpdateProtected(ref updateContext);

        public void OnStop()
            => OnStopProtected();

        public void OnDestroy()
            => OnDestroyProtected();

        protected abstract void OnCreateProtected(ref SystemCreateContext createContext);

        protected abstract void OnStartProtected();

        protected abstract void OnUpdateProtected(ref SystemUpdateContext updateContext);

        protected abstract void OnStopProtected();

        protected abstract void OnDestroyProtected();
    }

    /// <summary>
    /// 泛型系统缓存：为某个具体的值类型系统提供缓存与生命周期委托。
    /// </summary>
    internal sealed class SystemSchedulerContext<TSystem> : SystemSchedulerContext where TSystem : ISystem
    {
        private TSystem _system;

        public SystemSchedulerContext(TSystem system) : this(system, typeof(TSystem).Name, null)
        {
        }

        public SystemSchedulerContext(TSystem system, string name) : this(system, name, null)
        {
        }

        public SystemSchedulerContext(TSystem system, ParallelJobManager? jobManager) : this(system, typeof(TSystem).Name, jobManager)
        {
        }

        public SystemSchedulerContext(TSystem system, string name, ParallelJobManager? jobManager) : base(name, jobManager)
            => _system = system;

        protected override void OnCreateProtected(ref SystemCreateContext createContext)
            => _system.OnCreate(ref createContext);

        protected override void OnDestroyProtected()
            => _system.OnDestroy();

        protected override void OnStartProtected()
            => _system.OnStart();

        protected override void OnStopProtected()
            => _system.OnStop();

        protected override void OnUpdateProtected(ref SystemUpdateContext updateContext)
            => _system.OnUpdate(ref updateContext);
    }
}
