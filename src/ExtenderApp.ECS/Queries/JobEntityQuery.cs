using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    public struct JobEntityQuery
    {
        private Archetype _archetype;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype)
        {
            this._archetype = archetype;
        }

        public ChunkRowEnumerator GetEnumerator() => new(GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));
    }

    public readonly struct JobEntityQuery<T1>
    {
        private readonly Archetype _archetype;
        private readonly ulong _version;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype, ulong version)
        {
            _archetype = archetype;
            _version = version;
        }

        public ChunkRowEnumerator<T1> GetEnumerator() => new(GetChunkEnumerator<T1>(), GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));

        private ChunkEnumerator<T> GetChunkEnumerator<T>() => new(new(_archetype, _version));
    }

    public readonly struct JobEntityQuery<T1, T2>
    {
        private readonly Archetype _archetype;
        private readonly ulong _version;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype, ulong version)
        {
            _archetype = archetype;
            _version = version;
        }

        public ChunkRowEnumerator<T1, T2> GetEnumerator() => new(GetChunkEnumerator<T1>(), GetChunkEnumerator<T2>(), GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));

        private ChunkEnumerator<T> GetChunkEnumerator<T>() => new(new(_archetype, _version));
    }

    public readonly struct JobEntityQuery<T1, T2, T3>
    {
        private readonly Archetype _archetype;
        private readonly ulong _version;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype, ulong version)
        {
            _archetype = archetype;
            _version = version;
        }

        public ChunkRowEnumerator<T1, T2, T3> GetEnumerator() => new(GetChunkEnumerator<T1>(), GetChunkEnumerator<T2>(), GetChunkEnumerator<T3>(), GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));

        private ChunkEnumerator<T> GetChunkEnumerator<T>() => new(new(_archetype, _version));
    }

    public readonly struct JobEntityQuery<T1, T2, T3, T4>
    {
        private readonly Archetype _archetype;
        private readonly ulong _version;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype, ulong version)
        {
            _archetype = archetype;
            _version = version;
        }

        public ChunkRowEnumerator<T1, T2, T3, T4> GetEnumerator() => new(GetChunkEnumerator<T1>(), GetChunkEnumerator<T2>(), GetChunkEnumerator<T3>(), GetChunkEnumerator<T4>(), GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));

        private ChunkEnumerator<T> GetChunkEnumerator<T>() => new(new(_archetype, _version));
    }

    public readonly struct JobEntityQuery<T1, T2, T3, T4, T5>
    {
        private readonly Archetype _archetype;
        private readonly ulong _version;

        public bool IsEmpty => _archetype == null;

        internal JobEntityQuery(Archetype archetype, ulong version)
        {
            _archetype = archetype;
            _version = version;
        }

        public ChunkRowEnumerator<T1, T2, T3, T4, T5> GetEnumerator() => new(GetChunkEnumerator<T1>(), GetChunkEnumerator<T2>(), GetChunkEnumerator<T3>(), GetChunkEnumerator<T4>(), GetChunkEnumerator<T5>(), GetEntityEnumerator());

        private ChunkEntityEnumerator GetEntityEnumerator() => new(new(_archetype));

        private ChunkEnumerator<T> GetChunkEnumerator<T>() => new(new(_archetype, _version));
    }
}