using ExtenderApp.ECS.Abstract;
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
        /// 向默认每帧组注册系统。
        /// </summary>
        public void AddDefaultFrameSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddSystem<TSystem>(WorldSystemGroupNames.DefaultGroup);

        /// <summary>
        /// 向呈现每帧组（名 <see cref="WorldSystemGroupNames.PresentationGroup"/>）注册系统。
        /// </summary>
        public void AddRenderingFrameSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddSystem<TSystem>(WorldSystemGroupNames.PresentationGroup);

        /// <summary>
        /// 向固定步长组（名 <see cref="WorldSystemGroupNames.FixedGroup"/>）注册系统。
        /// </summary>
        public void AddFixedUpdateSystem<TSystem>() where TSystem : ISystem, new()
            => SGManager.AddFixedSystem<TSystem>();

        /// <summary>
        /// 在单帧内完整驱动所有每帧系统组（含模拟与渲染组），回放命令缓冲，并将内部时间与 <see cref="SystemUpdateContext.FrameIndex"/> 的权威值各推进一帧。
        /// </summary>
        /// <remarks>
        /// 等价于先执行模拟阶段再执行呈现阶段的合并路径；若宿主需要「多倍模拟步 + 单次呈现」，请改用 <see cref="UpdateSimulation"/> 与 <see cref="UpdatePresentation"/>。
        /// </remarks>
        /// <param name="deltaTime">本帧与上一帧之间的时间间隔（秒）。</param>
        public void Update(float deltaTime)
        {
            ThrowIfNotMainThread();
            ThrowIfSystemsRunning(nameof(Update));
            SGManager.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 驱动每帧逻辑组（<see cref="WorldSystemGroupNames.DefaultGroup"/>）；回放命令缓冲；推进世界累计时间与帧序号。
        /// </summary>
        /// <remarks>
        /// <see cref="SystemUpdateContext.FrameIndex"/> 与累计时间仅在此类「模拟 tick」调用后递增。典型宿主：同一墙钟帧内多次调用本方法进行固定步长或子步模拟，再调用一次 <see cref="UpdatePresentation"/>。
        /// </remarks>
        /// <param name="deltaTime">本模拟步的时间间隔（秒）。</param>
        public void UpdateSimulation(float deltaTime)
        {
            ThrowIfNotMainThread();
            ThrowIfSystemsRunning(nameof(UpdateSimulation));
            SGManager.OnUpdateSimulation(deltaTime);
        }

        /// <summary>
        /// 仅驱动渲染每帧系统组；回放命令缓冲；不推进累计时间与帧序号。
        /// </summary>
        /// <remarks>
        /// 呈现阶段使用的 <see cref="SystemUpdateContext.Time"/> 与 <see cref="SystemUpdateContext.FrameIndex"/> 为当前已模拟到的权威值，不会在呈现调用中额外递增。典型宿主：每个显示帧在若干次 <see cref="UpdateSimulation"/> 之后调用一次本方法。
        /// </remarks>
        /// <param name="deltaTime">本呈现步的时间间隔（秒），供渲染侧计算插值等用途。</param>
        public void UpdatePresentation(float deltaTime)
        {
            ThrowIfNotMainThread();
            ThrowIfSystemsRunning(nameof(UpdatePresentation));
            SGManager.OnUpdatePresentation(deltaTime);
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