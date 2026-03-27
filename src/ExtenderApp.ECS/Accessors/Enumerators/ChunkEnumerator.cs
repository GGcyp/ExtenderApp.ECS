namespace ExtenderApp.ECS.Accessors
{
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