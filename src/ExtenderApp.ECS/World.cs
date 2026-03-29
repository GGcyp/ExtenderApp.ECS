using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Commands;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Systems;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    public partial class World : DisposableObject
    {
        private const string DefaultWorldName = "DefaultWorld";
        internal DateTime CreationTime { get; }

        internal EntityManager EManager { get; }
        internal ArchetypeManager AManager { get; }
        internal WorldVersionManager WVManager { get; }
        internal EntityQueryManager EQManager { get; }
        internal SharedComponentManager SCManager { get; }
        internal EntityCommandStorage ECStorage { get; }
        internal ArchetypeRepository ArchetypeRepository { get; }
        internal ParallelJobManager PJManager { get; }
        internal SystemGroupManager SGManager { get; }

        /// <summary>
        /// 主线程命令回放器：将 <see cref="CommandBuffer"/> 中已记录的结构变更应用到实体/原型。
        /// </summary>
        internal EntityCommandReader CommandReader { get; }

        public EntityCommandBuffer CommandBuffer { get; }

        public string Name { get; }

        /// <summary>
        /// 构造时使用的选项（并行策略等）。
        /// </summary>
        public WorldOptions Options { get; }

        static World()
        {
            MainThreadDetector.Initialize();
        }

        public World() : this(DefaultWorldName, null)
        {
        }

        public World(string name) : this(name, null)
        {
        }

        public World(WorldOptions? options) : this(DefaultWorldName, options)
        {
        }

        public World(string name, WorldOptions? options)
        {
            options ??= WorldOptions.Default;
            Options = options;
            Name = name;
            EManager = new();
            WVManager = new();

            ArchetypeRepository = new();
            AManager = new(ArchetypeRepository, WVManager);
            EQManager = new(ArchetypeRepository, WVManager);
            SCManager = new();

            ECStorage = new();
            CommandBuffer = new(ECStorage);
            CommandReader = new EntityCommandReader(ECStorage, EManager, AManager, EQManager);

            bool parallelWorkers = options.ParallelJobs == WorldParallelJobsMode.PerWorldWorkers;
            PJManager = new ParallelJobManager(parallelWorkers);
            SGManager = new();

            InitSystemGroups();
            CreationTime = DateTime.UtcNow;
        }

        private void InitSystemGroups()
        {
            SGManager.AddGroup(SystemGroupNames.DefaultGroup);
            SGManager.AddGroup(SystemGroupNames.RenderingSystem);
        }

        /// <summary>
        /// 将本段调度中写入命令缓冲的结构变更应用到实体与原型。
        /// </summary>
        internal void PlaybackRecordedCommands()
        {
            CommandReader.ReadCommands();
        }

        private void ThrowIfNotMainThread() => MainThreadDetector.ThrowIfNotMainThread();

        protected override void DisposeManagedResources()
        {
            if (_systemsStarted)
            {
                SGManager.OnStop();
                _systemsStarted = false;
            }

            if (_systemsCreated)
            {
                SGManager.OnDestroy();
                _systemsCreated = false;
            }

            PJManager.Dispose();
            base.DisposeManagedResources();
            SCManager.Dispose();
            AManager.Dispose();
        }
    }
}
