using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    public partial class World : DisposableObject
    {
        private const string DefaultWorldName = "DefaultWorld";

        internal EntityManager EntityManager { get; }
        internal ArchetypeManager ArchetypeManager { get; }
        internal WorldVersionManager VersionManager { get; }
        internal EntityQueryManager EntityQueryManager { get; }

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
            EntityManager = new();
            VersionManager = new();

            ArchetypeRepository archetypeRepository = new();
            ArchetypeManager = new(archetypeRepository, VersionManager);
            EntityQueryManager = new(archetypeRepository.Values, VersionManager);
        }

        /// <summary>
        /// 校验当前线程为主线程，否则抛出异常。
        /// </summary>
        private void ThrowIfNotMainThread() => MainThreadDetector.ThrowIfNotMainThread();

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            ArchetypeManager.Dispose();
        }
    }
}