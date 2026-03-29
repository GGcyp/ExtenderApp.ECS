namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 便利一个Archetype中的所有Chunk，并在每个Chunk中便利指定类型的组件数据。每次MoveNext都会返回当前Chunk中下一个组件数据的引用。
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal struct ChunkEnumerator<T>
    {
        private ChunkAccessorEnumerator<T> cEnumerator;
        private ComponentEnumerator<T> enumerator;

        public RefRW<T> Current => enumerator.Current;

        public ChunkEnumerator(ChunkAccessorEnumerator<T> cEnumerator)
        {
            this.cEnumerator = cEnumerator;
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

                return false;
            }
        }
    }
}