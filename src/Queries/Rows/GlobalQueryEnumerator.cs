using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 全局查询组件枚举器，用于在所有选中原型查询中枚举组件数据。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal struct GlobalQueryEnumerator<T>
    {
        private ArchetypeAccessorEnumerator<T> aEnumerator;
        private ChunkAccessorEnumerator<T> cEnumerator;
        private ComponentEnumerator<T> enumerator;

        public RefRW<T> Current => enumerator.Current;

        public GlobalQueryEnumerator(ArchetypeAccessorEnumerator<T> aEnumerator)
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