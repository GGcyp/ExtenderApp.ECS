using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    public struct ChunkRowEnumerator
    {
        private ChunkEntityEnumerator entityEnumerator;

        public Entity Current => entityEnumerator.Current;

        internal ChunkRowEnumerator(ChunkEntityEnumerator entityEnumerator)
        {
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    public struct ChunkRowEnumerator<T1>
    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1> Current => new(entityEnumerator.Current, enum1.Current);

        internal ChunkRowEnumerator(ChunkEnumerator<T1> enum1, ChunkEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    public struct ChunkRowEnumerator<T1, T2>
    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current);

        internal ChunkRowEnumerator(ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    public struct ChunkRowEnumerator<T1, T2, T3>
    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current);

        internal ChunkRowEnumerator(ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3, ChunkEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    public struct ChunkRowEnumerator<T1, T2, T3, T4>
    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEnumerator<T4> enum4;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3, T4> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current);

        internal ChunkRowEnumerator(ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3, ChunkEnumerator<T4> enum4, ChunkEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    /// <typeparam name="T5">第五个组件类型。</typeparam>
    public struct ChunkRowEnumerator<T1, T2, T3, T4, T5>
    {
        private ChunkEnumerator<T1> enum1;
        private ChunkEnumerator<T2> enum2;
        private ChunkEnumerator<T3> enum3;
        private ChunkEnumerator<T4> enum4;
        private ChunkEnumerator<T5> enum5;
        private ChunkEntityEnumerator entityEnumerator;
        public EntityQueryRow<T1, T2, T3, T4, T5> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current, enum5.Current);

        internal ChunkRowEnumerator(ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3, ChunkEnumerator<T4> enum4, ChunkEnumerator<T5> enum5, ChunkEntityEnumerator entityEnumerator)
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