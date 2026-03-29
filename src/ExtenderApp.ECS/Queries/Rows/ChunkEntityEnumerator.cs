namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 对 <see cref="ChunkEntityAccessorEnumerator" /> 的包装，提供了一个扁平化的实体枚举器，允许直接枚举所有实体，而不需要关心它们所在的Chunk。
    /// </summary>
    internal struct ChunkEntityEnumerator
    {
        private ChunkEntityAccessorEnumerator cEnumerator;
        private ComponentEntityEnumerator enumerator;

        public Entity Current => enumerator.Current;

        public ChunkEntityEnumerator(ChunkEntityAccessorEnumerator cEnumerator)
        {
            this.cEnumerator = cEnumerator;
            enumerator = default;
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