using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    public struct JobEntityQuery
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
        }

        public ChunkRowEnumerator GetEnumerator() => new(_chunkEntityEnumerator);
    }

    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件的类型。</typeparam>
    public readonly struct JobEntityQuery<T1>
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;
        private readonly ChunkEnumerator<T1> _enum1;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator, ChunkEnumerator<T1> enum1)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
            _enum1 = enum1;
        }

        public ChunkRowEnumerator<T1> GetEnumerator() => new(_enum1, _chunkEntityEnumerator);
    }

    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件的类型。</typeparam>
    /// <typeparam name="T2">第二个组件的类型。</typeparam>
    public readonly struct JobEntityQuery<T1, T2>
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;
        private readonly ChunkEnumerator<T1> _enum1;
        private readonly ChunkEnumerator<T2> _enum2;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator, ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
            _enum1 = enum1;
            _enum2 = enum2;
        }

        public ChunkRowEnumerator<T1, T2> GetEnumerator() => new(_enum1, _enum2, _chunkEntityEnumerator);
    }

    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件的类型。</typeparam>
    /// <typeparam name="T2">第二个组件的类型。</typeparam>
    /// <typeparam name="T3">第三个组件的类型。</typeparam>
    public readonly struct JobEntityQuery<T1, T2, T3>
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;
        private readonly ChunkEnumerator<T1> _enum1;
        private readonly ChunkEnumerator<T2> _enum2;
        private readonly ChunkEnumerator<T3> _enum3;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator, ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
        }

        public ChunkRowEnumerator<T1, T2, T3> GetEnumerator() => new(_enum1, _enum2, _enum3, _chunkEntityEnumerator);
    }

    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件的类型。</typeparam>
    /// <typeparam name="T2">第二个组件的类型。</typeparam>
    /// <typeparam name="T3">第三个组件的类型。</typeparam>
    /// <typeparam name="T4">第四个组件的类型。</typeparam>
    public readonly struct JobEntityQuery<T1, T2, T3, T4>
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;
        private readonly ChunkEnumerator<T1> _enum1;
        private readonly ChunkEnumerator<T2> _enum2;
        private readonly ChunkEnumerator<T3> _enum3;
        private readonly ChunkEnumerator<T4> _enum4;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator, ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3, ChunkEnumerator<T4> enum4)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
        }

        public ChunkRowEnumerator<T1, T2, T3, T4> GetEnumerator() => new(_enum1, _enum2, _enum3, _enum4, _chunkEntityEnumerator);
    }

    /// <summary>
    /// 多线程查询结果的枚举器。对于一个Chunk中的每一行，提供一个枚举器。每一行包含一个Entity和与之相关的组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件的类型。</typeparam>
    /// <typeparam name="T2">第二个组件的类型。</typeparam>
    /// <typeparam name="T3">第三个组件的类型。</typeparam>
    /// <typeparam name="T4">第四个组件的类型。</typeparam>
    /// <typeparam name="T5">第五个组件的类型。</typeparam>
    public readonly struct JobEntityQuery<T1, T2, T3, T4, T5>
    {
        private readonly ChunkEntityEnumerator _chunkEntityEnumerator;
        private readonly ChunkEnumerator<T1> _enum1;
        private readonly ChunkEnumerator<T2> _enum2;
        private readonly ChunkEnumerator<T3> _enum3;
        private readonly ChunkEnumerator<T4> _enum4;
        private readonly ChunkEnumerator<T5> _enum5;

        internal JobEntityQuery(ChunkEntityEnumerator chunkEntityEnumerator, ChunkEnumerator<T1> enum1, ChunkEnumerator<T2> enum2, ChunkEnumerator<T3> enum3, ChunkEnumerator<T4> enum4, ChunkEnumerator<T5> enum5)
        {
            _chunkEntityEnumerator = chunkEntityEnumerator;
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
            _enum5 = enum5;
        }

        public ChunkRowEnumerator<T1, T2, T3, T4, T5> GetEnumerator() => new(_enum1, _enum2, _enum3, _enum4, _enum5, _chunkEntityEnumerator);
    }
}