using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Commands;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    public partial class World : DisposableObject
    {
        private const string DefaultWorldName = "DefaultWorld";

        internal EntityManager Entities { get; }
        internal ArchetypeManager ArchetypeManager { get; }
        internal WorldVersionManager VersionManager { get; }
        internal EntityQueryManager QueryManager { get; }
        internal SharedComponentManager SharedComponentManager { get; }
        public EntityCommandBuffer CommandBuffer { get; }

        public string Name { get; }

        static World()
        {
            MainThreadDetector.Initialize();
        }

        public World() : this(DefaultWorldName)
        {
        }

        public World(string name)
        {
            Name = name;
            Entities = new();
            VersionManager = new();

            ArchetypeRepository archetypeRepository = new();
            ArchetypeManager = new(archetypeRepository, VersionManager);
            QueryManager = new(archetypeRepository, VersionManager);
            SharedComponentManager = new();

            EntityCommandStorage commandStorage = new();
            CommandBuffer = new(commandStorage);
        }

        /// <summary>
        /// 校验当前线程为主线程，否则抛出异常。
        /// </summary>
        private void ThrowIfNotMainThread() => MainThreadDetector.ThrowIfNotMainThread();

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            SharedComponentManager.Dispose();
            ArchetypeManager.Dispose();
        }
    }
}