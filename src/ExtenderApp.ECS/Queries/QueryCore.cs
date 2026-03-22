using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 查询核心对象。 负责管理查询描述、版本失效以及匹配到的 Archetype 集合。 组件列位置不在此处固定缓存，而是下沉到访问器层按 Archetype 动态计算。
    /// </summary>
    internal sealed class QueryCore
    {
        private readonly IReadOnlyList<Archetype> _allArchetypeList;
        private readonly WorldVersionManager _worldVersionManager;
        private readonly EntityQueryDesc _desc;
        private Archetype[] _archetypes;
        private ulong _version;

        internal QueryCore(
            IReadOnlyList<Archetype> allArchetypeList,
            WorldVersionManager worldVersionManager,
            EntityQueryDesc desc)
        {
            _allArchetypeList = allArchetypeList;
            _worldVersionManager = worldVersionManager;
            _desc = desc;
            _archetypes = Array.Empty<Archetype>();
            _version = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityQueryAccessor<T> CreateAccessor<T>(bool skipUnchanged) where T : struct
            => new(GetArchetypes(), ComponentType.Create<T>(), GetVersion(skipUnchanged));

        internal Archetype[] GetArchetypes()
        {
            EnsureMatches();
            return _archetypes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong GetVersion(bool skipUnchanged) => skipUnchanged ? _worldVersionManager.WorldVersion : 0;

        private void EnsureMatches()
        {
            if (_version == _worldVersionManager.ArchetypeVersion)
                return;

            RebuildMatches();
            _version = _worldVersionManager.ArchetypeVersion;
        }

        private void RebuildMatches()
        {
            List<Archetype> list = new();
            foreach (var archetype in _allArchetypeList)
            {
                if (MatchArchetype(archetype))
                    list.Add(archetype);
            }
            _archetypes = list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchArchetype(Archetype archetype)
        {
            if (!archetype.ComponentTypes.All(_desc.Query))
                return false;

            if (_desc.HasAll && !archetype.ComponentTypes.All(_desc.All))
                return false;

            if (_desc.HasAny && !archetype.ComponentTypes.Any(_desc.Any))
                return false;

            if (_desc.HasNone && !archetype.ComponentTypes.None(_desc.None))
                return false;

            if (_desc.HasRelation && !archetype.RelationTypes.On(_desc.Relation))
                return false;

            return true;
        }
    }
}