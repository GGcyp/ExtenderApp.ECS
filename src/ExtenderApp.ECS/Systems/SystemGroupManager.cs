using ExtenderApp.Contracts;
using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统组调度器：仅包含三个固定组（每帧逻辑、每帧呈现、固定步长）；每次调度末尾在当前线程回放命令缓冲。
    /// </summary>
    /// <remarks>
    /// <see cref="OnUpdate" /> 与 <see cref="OnFixedUpdate" /> 须由调用方在同一线程同步执行，并遵守宿主逻辑线程约束，末尾调用 <see cref="World.PlaybackRecordedCommands" />。
    /// </remarks>
    internal sealed class SystemGroupManager : DisposableObject
    {
        /// <summary>
        /// 每帧逻辑组，对应 <see cref="WorldSystemGroupNames.DefaultGroup" />；由 <see cref="OnUpdateSimulation" /> 或完整 <see cref="OnUpdate" /> 的前半段调度。
        /// </summary>
        private readonly SystemSchedulerGroup _defaultFrameGroup;

        /// <summary>
        /// 每帧呈现组，对应 <see cref="WorldSystemGroupNames.PresentationGroup" />；由 <see cref="OnUpdatePresentation" /> 或完整 <see cref="OnUpdate" /> 的后半段调度。
        /// </summary>
        private readonly SystemSchedulerGroup _presentationFrameGroup;

        /// <summary>
        /// 固定步长组，对应 <see cref="WorldSystemGroupNames.FixedGroup" />；仅由 <see cref="OnFixedUpdate" /> 调度。
        /// </summary>
        private readonly SystemSchedulerGroup _fixedGroup;

        /// <summary>
        /// 自世界开始以来的仿真帧序号，供 <see cref="SystemGroupUpdateContext" /> 传入系统；在「每帧逻辑」类调用末尾递增（见各更新路径说明）。
        /// </summary>
        private ulong frameIndex;

        /// <summary>
        /// 自世界开始以来的累计仿真时间（秒），供 <see cref="SystemGroupUpdateContext" /> 传入系统。
        /// </summary>
        private double time;

        /// <summary>
        /// 所属 <see cref="World" />，用于回放命令缓冲；在 <see cref="OnCreate" /> 中赋值，在 <see cref="OnDestroy" /> 中清空。
        /// </summary>
        private World? _world;

        /// <summary>
        /// 构造三个内置系统组实例，组名分别对应 <see cref="WorldSystemGroupNames" /> 中的常量。
        /// </summary>
        public SystemGroupManager()
        {
            _defaultFrameGroup = new(WorldSystemGroupNames.DefaultGroup);
            _presentationFrameGroup = new(WorldSystemGroupNames.PresentationGroup);
            _fixedGroup = new(WorldSystemGroupNames.FixedGroup);
        }

        /// <summary>
        /// 按组名将系统注册到对应的内置组之一。
        /// </summary>
        /// <typeparam name="TSystem">实现 <see cref="ISystem" /> 的系统类型。</typeparam>
        /// <param name="name">须为 <see cref="WorldSystemGroupNames" /> 中之一。</param>
        /// <exception cref="ArgumentException">当 <paramref name="name" /> 不是已知的内置组名时抛出。</exception>
        public void AddSystem<TSystem>(string name) where TSystem : ISystem, new()
        {
            SystemSchedulerContext<TSystem> context = new(new TSystem());
            if (name == WorldSystemGroupNames.DefaultGroup)
                _defaultFrameGroup.AddSystem(context);
            else if (name == WorldSystemGroupNames.PresentationGroup)
                _presentationFrameGroup.AddSystem(context);
            else if (name == WorldSystemGroupNames.FixedGroup)
                _fixedGroup.AddSystem(context);
            else
                throw new ArgumentException($"不存在名为“{name}”的系统组。", nameof(name));
        }

        /// <summary>
        /// 向固定步长组注册系统（与按名注册到 <see cref="WorldSystemGroupNames.FixedGroup" /> 等价，供 <see cref="World.AddFixedUpdateSystem{TSystem}" /> 使用）。
        /// </summary>
        /// <typeparam name="TSystem">实现 <see cref="ISystem" /> 的系统类型。</typeparam>
        public void AddFixedSystem<TSystem>() where TSystem : ISystem, new()
        {
            var system = new TSystem();
            SystemSchedulerContext<TSystem> context = new(in system);
            _fixedGroup.AddSystem(context);
        }

        /// <summary>
        /// 在所属世界上执行各组的 <c>OnCreate</c>，并将累计时间与帧序号重置为零。
        /// </summary>
        /// <param name="world">当前 <see cref="World" /> 实例。</param>
        public void OnCreate(World world)
        {
            _world = world;
            SystemGroupCreateContext createContext = new(world);
            _defaultFrameGroup.OnCreate(ref createContext);
            _presentationFrameGroup.OnCreate(ref createContext);
            _fixedGroup.OnCreate(ref createContext);
            frameIndex = 0;
            time = 0;
        }

        /// <summary>
        /// 按固定顺序触发三个内置组的 <c>OnStart</c>：逻辑、呈现、固定步长。
        /// </summary>
        public void OnStart()
        {
            _defaultFrameGroup.OnStart();
            _presentationFrameGroup.OnStart();
            _fixedGroup.OnStart();
        }

        /// <summary>
        /// 按固定顺序触发三个内置组的 <c>OnStop</c>：逻辑、呈现、固定步长。
        /// </summary>
        public void OnStop()
        {
            _defaultFrameGroup.OnStop();
            _presentationFrameGroup.OnStop();
            _fixedGroup.OnStop();
        }

        /// <summary>
        /// 在单轮调度中依次执行每帧逻辑组与每帧呈现组，回放命令缓冲，并将 <see cref="time" /> 与 <see cref="frameIndex" /> 各推进一帧。
        /// </summary>
        /// <param name="deltaTime">本帧与上一帧之间的时间间隔（秒）。</param>
        public void OnUpdate(float deltaTime)
        {
            SystemGroupUpdateContext updateContext = new(deltaTime, time, frameIndex);
            _defaultFrameGroup.OnUpdate(ref updateContext);
            _presentationFrameGroup.OnUpdate(ref updateContext);
            _world?.PlaybackRecordedCommands();
            time += deltaTime;
            frameIndex++;
        }

        /// <summary>
        /// 仅调度每帧逻辑组，回放命令缓冲，并将 <see cref="time" /> 与 <see cref="frameIndex" /> 各推进一帧。
        /// </summary>
        /// <param name="deltaTime">本模拟步的时间间隔（秒）。</param>
        public void OnUpdateSimulation(float deltaTime)
            => RunSingleFrameGroup(_defaultFrameGroup, deltaTime, advanceTimeAndFrame: true);

        /// <summary>
        /// 仅调度每帧呈现组并回放命令缓冲；不修改 <see cref="time" /> 与 <see cref="frameIndex" />。
        /// </summary>
        /// <param name="deltaTime">本呈现步的时间间隔（秒）。</param>
        public void OnUpdatePresentation(float deltaTime)
            => RunSingleFrameGroup(_presentationFrameGroup, deltaTime, advanceTimeAndFrame: false);

        /// <summary>
        /// 对单个每帧组执行一次更新并回放命令缓冲；按标志决定是否推进累计时间与帧序号。
        /// </summary>
        /// <param name="group">要调度的每帧组。</param>
        /// <param name="deltaTime">本步时间间隔（秒）。</param>
        /// <param name="advanceTimeAndFrame">为 true 时在回放后将 <see cref="time" /> 增加 <paramref name="deltaTime" /> 且将 <see cref="frameIndex" /> 加一。</param>
        private void RunSingleFrameGroup(SystemSchedulerGroup group, float deltaTime, bool advanceTimeAndFrame)
        {
            SystemGroupUpdateContext updateContext = new(deltaTime, time, frameIndex);
            group.OnUpdate(ref updateContext);
            _world?.PlaybackRecordedCommands();
            if (advanceTimeAndFrame)
            {
                time += deltaTime;
                frameIndex++;
            }
        }

        /// <summary>
        /// 仅调度固定步长组并回放命令缓冲；不推进 <see cref="time" /> 与 <see cref="frameIndex" />（与每帧仿真时钟解耦）。
        /// </summary>
        /// <param name="deltaTime">固定步长时间间隔（秒）。</param>
        public void OnFixedUpdate(float deltaTime)
        {
            SystemGroupUpdateContext updateContext = new(deltaTime, time, frameIndex);
            _fixedGroup.OnUpdate(ref updateContext);
            _world?.PlaybackRecordedCommands();
        }

        /// <summary>
        /// 按固定顺序触发三个内置组的 <c>OnDestroy</c>，并解除对 <see cref="World" /> 的引用。
        /// </summary>
        public void OnDestroy()
        {
            _defaultFrameGroup.OnDestroy();
            _presentationFrameGroup.OnDestroy();
            _fixedGroup.OnDestroy();
            _world = null;
        }
    }
}