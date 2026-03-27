using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries.Rows
{
    public struct ArchetypeRowEnumerator
    {
        private ArchetypeEntityEnumerator entityEnumerator;

        public Entity Current => entityEnumerator.Current;

        internal ArchetypeRowEnumerator(ArchetypeEntityEnumerator entityEnumerator) => this.entityEnumerator = entityEnumerator;

        public bool MoveNext() => entityEnumerator.MoveNext();
    }

    public struct ArchetypeRowEnumerator<T1>
    {
        private ArchetypeEnumerator<T1> enum1;
        private ArchetypeEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1> Current => new(entityEnumerator.Current, enum1.Current);

        internal ArchetypeRowEnumerator(ArchetypeEnumerator<T1> enum1, ArchetypeEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && entityEnumerator.MoveNext();
    }

    public struct ArchetypeRowEnumerator<T1, T2>
    {
        private ArchetypeEnumerator<T1> enum1;
        private ArchetypeEnumerator<T2> enum2;
        private ArchetypeEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current);

        internal ArchetypeRowEnumerator(ArchetypeEnumerator<T1> enum1, ArchetypeEnumerator<T2> enum2, ArchetypeEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && entityEnumerator.MoveNext();
    }

    public struct ArchetypeRowEnumerator<T1, T2, T3>
    {
        private ArchetypeEnumerator<T1> enum1;
        private ArchetypeEnumerator<T2> enum2;
        private ArchetypeEnumerator<T3> enum3;
        private ArchetypeEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current);

        internal ArchetypeRowEnumerator(ArchetypeEnumerator<T1> enum1, ArchetypeEnumerator<T2> enum2, ArchetypeEnumerator<T3> enum3, ArchetypeEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && entityEnumerator.MoveNext();
    }

    public struct ArchetypeRowEnumerator<T1, T2, T3, T4>
    {
        private ArchetypeEnumerator<T1> enum1;
        private ArchetypeEnumerator<T2> enum2;
        private ArchetypeEnumerator<T3> enum3;
        private ArchetypeEnumerator<T4> enum4;
        private ArchetypeEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3, T4> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current);

        internal ArchetypeRowEnumerator(ArchetypeEnumerator<T1> enum1, ArchetypeEnumerator<T2> enum2, ArchetypeEnumerator<T3> enum3, ArchetypeEnumerator<T4> enum4, ArchetypeEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && entityEnumerator.MoveNext();
    }

    public struct ArchetypeRowEnumerator<T1, T2, T3, T4, T5>
    {
        private ArchetypeEnumerator<T1> enum1;
        private ArchetypeEnumerator<T2> enum2;
        private ArchetypeEnumerator<T3> enum3;
        private ArchetypeEnumerator<T4> enum4;
        private ArchetypeEnumerator<T5> enum5;
        private ArchetypeEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3, T4, T5> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current, enum5.Current);

        internal ArchetypeRowEnumerator(ArchetypeEnumerator<T1> enum1, ArchetypeEnumerator<T2> enum2, ArchetypeEnumerator<T3> enum3, ArchetypeEnumerator<T4> enum4, ArchetypeEnumerator<T5> enum5, ArchetypeEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.enum5 = enum5;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && enum5.MoveNext() && entityEnumerator.MoveNext();
    }
}