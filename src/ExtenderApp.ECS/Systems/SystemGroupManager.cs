using System;
using ExtenderApp.Contracts;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统组管理器：按每帧组与固定组调度系统，并在每次调度末尾于当前线程回放命令缓冲。
    /// </summary>
    /// <remarks>
    /// OnUpdate 与 OnFixedUpdate 均由调用方线程同步执行；两者均在各自逻辑结束后调用 PlaybackRecordedCommands，
    /// 回放顺序与「仅主线程上的 ISystem」模型一致。若将来并行作业写入同一缓冲，须先增加完成屏障再回放。
    /// </remarks>
    internal sealed class SystemGroupManager : DisposableObject
    {
        private readonly List<SystemSchedulerGroup> _groupList;
        private SystemSchedulerGroup fixedGroup;
        private ulong frameIndex;
        private double time;
        private World? _world;

        public SystemGroupManager()
        {
            _groupList = new();
            fixedGroup = new(SystemGroupNames.FixedGroup);
        }

        public void AddSystem<TSystem>(string name) where TSystem : ISystem, new()
        {
            SystemSchedulerContext<TSystem> context = new(new TSystem());
            foreach (var group in _groupList)
            {
                if (group.Name == name)
                {
                    group.AddSystem(context);
                    return;
                }
            }

            throw new ArgumentException($"No system group named '{name}'.", nameof(name));
        }

        public void AddFixedSystem<TSystem>() where TSystem : ISystem, new()
        {
            SystemSchedulerContext<TSystem> context = new(new TSystem());
            fixedGroup.AddSystem(context);
        }

        public void AddGroup(string name)
        {
            _groupList.Add(new(name));
        }

        public void OnCreate(World world)
        {
            _world = world;
            SystemGroupCreateContext createContext = new(world);
            foreach (var group in _groupList)
            {
                group.OnCreate(ref createContext);
            }

            fixedGroup.OnCreate(ref createContext);
            frameIndex = 0;
            time = 0;
        }

        public void OnStart()
        {
            foreach (var group in _groupList)
            {
                group.OnStart();
            }

            fixedGroup.OnStart();
        }

        public void OnStop()
        {
            foreach (var group in _groupList)
            {
                group.OnStop();
            }

            fixedGroup.OnStop();
        }

        public void OnUpdate(float deltaTime)
        {
            SystemGroupUpdateContext updateContext = new(deltaTime, time, frameIndex);
            foreach (var group in _groupList)
            {
                group.OnUpdate(ref updateContext);
            }

            _world?.PlaybackRecordedCommands();
            time += deltaTime;
            frameIndex++;
        }

        public void OnFixedUpdate(float deltaTime)
        {
            SystemGroupUpdateContext updateContext = new(deltaTime, time, frameIndex);
            fixedGroup.OnUpdate(ref updateContext);
            _world?.PlaybackRecordedCommands();
        }

        public void OnDestroy()
        {
            foreach (var group in _groupList)
            {
                group.OnDestroy();
            }

            fixedGroup.OnDestroy();
            _world = null;
        }
    }
}
