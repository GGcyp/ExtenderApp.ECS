using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Queries
{
    internal class EntityQueryManager
    {
        private readonly ArchetypeDictionary _archetypeDict;
        private readonly Dictionary<EntityQueryDesc, EntityQuery> _queries;

        public EntityQueryManager(ArchetypeDictionary archetypeDict)
        {
            _archetypeDict = archetypeDict;
            _queries = new();
        }

        public EntityQuery GetOrCreateQuery(EntityQueryDesc desc)
        {
            if (_queries.TryGetValue(desc, out var query))
                return query;

            //query = new(_archetypeDict, desc);
            _queries.Add(desc, query);
            return query;
        }
    }
}