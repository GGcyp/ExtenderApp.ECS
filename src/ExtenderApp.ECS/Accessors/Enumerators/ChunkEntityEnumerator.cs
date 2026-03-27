namespace ExtenderApp.ECS.Accessors
{
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