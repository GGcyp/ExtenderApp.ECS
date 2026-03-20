using ExtenderApp.Contracts;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS
{
    public partial class World : DisposableObject
    {
        private const string DefaultWorldName = "DefaultWorld";

        private readonly ArchetypeDictionary _archetypeDict;

        internal EntityManager EntityManager { get; }
        internal ArchetypeManager ArchetypeManager { get; }
        internal WorldVersionManager VersionManager { get; }

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
            _archetypeDict = new();

            EntityManager = new();
            VersionManager = new();
            ArchetypeManager = new(VersionManager, _archetypeDict);
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