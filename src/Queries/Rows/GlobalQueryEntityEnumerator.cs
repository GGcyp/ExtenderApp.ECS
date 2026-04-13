using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 全局查询实体枚举器，用于在所有选中原型查询中枚举实体。
    /// </summary>
    internal struct GlobalQueryEntityEnumerator
    {
        private ArchetypeEntityAccessorEnumerator aEnumerator;
        private ChunkEntityAccessorEnumerator cEnumerator;
        private ComponentEntityEnumerator enumerator;

        public Entity Current => enumerator.Current;

        public GlobalQueryEntityEnumerator(ArchetypeEntityAccessorEnumerator aEnumerator)
        {
            this.aEnumerator = aEnumerator;
            this.cEnumerator = default;
            this.enumerator = default;
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (enumerator.MoveNext())
                    return true;

                if (cEnumerator.MoveNext())
                {
                    enumerator = cEnumerator.Current.GetEnumerator();
                    continue;
                }

                if (aEnumerator.MoveNext())
                {
                    cEnumerator = aEnumerator.Current.GetEnumerator();
                    continue;
                }

                return false;
            }
        }
    }
}