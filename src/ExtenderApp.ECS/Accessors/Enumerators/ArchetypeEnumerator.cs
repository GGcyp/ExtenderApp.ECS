namespace ExtenderApp.ECS.Accessors
{
    internal struct ArchetypeEnumerator<T>
    {
        private ArchetypeAccessorEnumerator<T> aEnumerator;
        private ChunkAccessorEnumerator<T> cEnumerator;
        private ComponentEnumerator<T> enumerator;

        public RefRW<T> Current => enumerator.Current;

        public ArchetypeEnumerator(ArchetypeAccessorEnumerator<T> aEnumerator)
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