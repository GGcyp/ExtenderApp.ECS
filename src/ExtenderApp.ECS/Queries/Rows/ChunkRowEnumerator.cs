using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    {
        private ChunkEntityEnumerator entityEnumerator;

        public Entity Current => entityEnumerator.Current;


        public bool MoveNext() => entityEnumerator.MoveNext();
    }

    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1> Current => new(entityEnumerator.Current, enum1.Current);

        {
            this.enum1 = enum1;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && entityEnumerator.MoveNext();
    }

    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current);

        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && entityEnumerator.MoveNext();
    }

    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current);

        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && entityEnumerator.MoveNext();
    }

    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEnumerator<T4> enum4;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3, T4> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current);

        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && entityEnumerator.MoveNext();
    }

    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEnumerator<T4> enum4;
        private ChunkEnumerator<T5> enum5;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3, T4, T5> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current, enum5.Current);

        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && enum5.MoveNext() && entityEnumerator.MoveNext();
    }
}