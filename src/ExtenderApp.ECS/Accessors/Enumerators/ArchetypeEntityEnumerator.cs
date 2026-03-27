namespace ExtenderApp.ECS.Accessors
{
    internal struct ArchetypeEntityEnumerator
    {
        private ArchetypeEntityAccessorEnumerator aEnumerator;
        private ChunkEntityAccessorEnumerator cEnumerator;
        private ComponentEntityEnumerator enumerator;

        public Entity Current => enumerator.Current;

        public ArchetypeEntityEnumerator(ArchetypeEntityAccessorEnumerator aEnumerator)
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