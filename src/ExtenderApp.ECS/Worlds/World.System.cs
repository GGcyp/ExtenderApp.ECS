using ExtenderApp.ECS.Systems;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 上系统注册、显式生命周期与主循环入口。
    /// </summary>
    public partial class World
    {
        private bool _systemsCreated;
        private bool _systemsStarted;

        /// <summary>
        /// 触发系统创建阶段。
        /// </summary>
        public void InitializeSystems()
        {
            ThrowIfNotMainThread();
            if (_systemsCreated)
                throw new InvalidOperationException("系统已初始化，勿重复调用 InitializeSystems。");

            SGManager.OnCreate(this);
            _systemsCreated = true;
        }

        /// <summary>
        /// 触发系统启动阶段。
        /// </summary>
        public void StartSystems()
        {
            ThrowIfNotMainThread();
            if (!_systemsCreated)
                throw new InvalidOperationException("请先调用 InitializeSystems。");

            if (_systemsStarted)
                throw new InvalidOperationException("系统已处于启动状态，勿重复调用 StartSystems。");

            SGManager.OnStart();
            _systemsStarted = true;
        }

        /// <summary>
        /// 触发系统停止阶段。
        /// </summary>
        public void StopSystems()
        {
            ThrowIfNotMainThread();
            if (!_systemsStarted)
                return;

            SGManager.OnStop();
            _systemsStarted = false;
        }

        /// <summary>
        /// 触发系统销毁阶段。
        /// </summary>
        public void DestroySystems()
        {
            ThrowIfNotMainThread();
            if (!_systemsCreated)
                return;

            if (_systemsStarted)
            {
                SGManager.OnStop();
                _systemsStarted = false;
            }

            SGManager.OnDestroy();
            _systemsCreated = false;
        }

        /// <summary>
        /// 注册自定义每帧系统组。
        /// </summary>
        public void AddCustomSystemGroup(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name == SystemGroupNames.DefaultGroup || name == SystemGroupNames.RenderingSystem || name == SystemGroupNames.FixedGroup)
                throw new ArgumentException("该名称为内置组保留。");

            SGManager.AddGroup(name);
        }

        /// <summary>
        /// 向默认每帧组注册系统。
        /// </summary>
        public void AddDefaultFrameSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddSystem<TSystem>(SystemGroupNames.DefaultGroup);

        /// <summary>
        /// 向渲染每帧组注册系统。
        /// </summary>
        public void AddRenderingFrameSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddSystem<TSystem>(SystemGroupNames.RenderingSystem);

        /// <summary>
        /// 向自定义每帧组注册系统。
        /// </summary>
        public void AddFrameSystemToCustomGroup<TSystem>(string customGroupName) where TSystem : ISystem, new()
            => SGManager.AddSystem<TSystem>(customGroupName);

        /// <summary>
        /// 向固定步长组注册系统。
        /// </summary>
        public void AddFixedUpdateSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddFixedSystem<TSystem>();

        /// <summary>
        /// 驱动每帧系统组并回放命令缓冲。
        /// </summary>
        public void Update(float deltaTime)
        {
            ThrowIfNotMainThread();
            ThrowIfSystemsRunning(nameof(Update));
            SGManager.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 驱动固定步长系统组并回放命令缓冲。
        /// </summary>
        public void FixedUpdate(float deltaTime)
        {
            ThrowIfNotMainThread();
            ThrowIfSystemsRunning(nameof(FixedUpdate));
            SGManager.OnFixedUpdate(deltaTime);
        }

        private void ThrowIfSystemsRunning(string operationName)
        {
            if (!_systemsCreated || !_systemsStarted)
            {
                throw new InvalidOperationException(
                    $"{operationName} 前须先依次调用 InitializeSystems 与 StartSystems。");
            }
        }
    }
}