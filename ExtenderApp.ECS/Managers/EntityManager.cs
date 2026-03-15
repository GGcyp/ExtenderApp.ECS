using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Managers
{
    /// <summary>
    /// 管理实体的创建与生命周期。
    /// </summary>
    public class EntityManager : IEntityManager
    {
        private const int DefaultSegmentSize = 1024;

        /// <summary>
        /// 下一个可用的实体标识。
        /// </summary>
        private uint nextId;

        private EntityVersionSegment first;

        public EntityManager()
        {
            nextId = 0;
            first = new();
        }

        /// <summary>
        /// 创建新实体并返回其标识。
        /// </summary>
        /// <returns>新实体标识。</returns>
        public Entity CreateEntity()
        {
            EntityVersionSegment current = first;
            while (true)
            {
                if (current.TryGetIdAndVersion(out int id, out uint version))
                    return new(id, version);

                if (current.Next == null)
                    current.SetNext(new EntityVersionSegment());

                current = current.Next!;
            }
        }

        public bool DestroyEntity(Entity entity)
        {
            EntityVersionSegment? current = first;
            int id = entity.Id;
            while (current != null)
            {
                if (current.ContainsId(id))
                {
                    current.ReleaseId(id);
                    return true;
                }
                else
                    current = current.Next;
            }
            return false;
        }

        public bool IsAlive(Entity entity)
        {
            EntityVersionSegment? current = first;
            int id = entity.Id;
            while (current != null)
            {
                if (current.TryGetVersion(id, out uint version))
                    return version == entity.Version;
                else
                    current = current.Next;
            }
            return false;
        }

        public IEnumerable<Entity> GetAliveEntities()
        {
            throw new NotImplementedException();
        }

        private sealed class EntityVersionSegment
        {
            private readonly uint[] _versions;
            private readonly Stack<int> _freeId;

            public int StartIndex;
            public int EndIndex;
            public EntityVersionSegment? Next { get; private set; }
            public int Count => DefaultSegmentSize - _freeId.Count;

            public EntityVersionSegment()
            {
                _versions = new uint[DefaultSegmentSize];
                _freeId = new(DefaultSegmentSize);

                Next = null;
                StartIndex = 0;
                EndIndex = DefaultSegmentSize - 1;

                for (int i = EndIndex; i > StartIndex; i--)
                    _freeId.Push(i);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetNext(EntityVersionSegment next)
            {
                Next = next;
                Next.StartIndex = EndIndex + 1;
                Next.EndIndex = Next.StartIndex + DefaultSegmentSize - 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetIdAndVersion(out int id, out uint version)
            {
                version = 0;
                if (_freeId.TryPop(out id))
                {
                    version = _versions[id];
                    return true;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReleaseId(int id)
            {
                _versions[id]++;
                _freeId.Push(id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ContainsId(int id) => id >= StartIndex && id <= EndIndex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetVersion(int id, out uint version)
            {
                version = 0;
                if (ContainsId(id))
                {
                    version = _versions[id];
                    return true;
                }
                return false;
            }
        }
    }
}