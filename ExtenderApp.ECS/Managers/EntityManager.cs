using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Managers
{
    /// <summary>
    /// 管理实体的创建与生命周期。
    /// </summary>
    public class EntityManager : IEntityManager
    {
        /// <summary>
        /// 默认的版本段容量。
        /// </summary>
        private const int DefaultSegmentSize = 1024;

        /// <summary>
        /// 首个实体版本段。
        /// </summary>
        private readonly EntityVersionSegment _first;

        /// <summary>
        /// 同步锁对象，用于保证实体管理过程的线程安全。
        /// </summary>
        private readonly object _syncRoot = new();

        /// <summary>
        /// 初始化 <see cref="EntityManager" /> 的新实例。
        /// </summary>
        public EntityManager()
        {
            _first = new(0);
        }

        /// <summary>
        /// 创建新实体并返回其标识。
        /// </summary>
        /// <returns>新实体标识。</returns>
        public Entity CreateEntity()
        {
            lock (_syncRoot)
            {
                var current = _first;
                while (true)
                {
                    if (current.TryRentEntity(out var entity))
                    {
                        return entity;
                    }

                    if (current.Next is null)
                    {
                        current.SetNext(new EntityVersionSegment(current.EndIndex + 1));
                    }

                    current = current.Next!;
                }
            }
        }

        /// <summary>
        /// 销毁指定实体。
        /// </summary>
        /// <param name="entity">要销毁的实体。</param>
        /// <returns>
        /// 销毁成功返回 true；若实体不存在或已销毁则返回 false。
        /// </returns>
        public bool DestroyEntity(Entity entity)
        {
            if (entity == Entity.Empty)
            {
                return false;
            }

            lock (_syncRoot)
            {
                var current = _first;
                while (current is not null)
                {
                    if (current.ReleaseEntity(entity))
                    {
                        return true;
                    }

                    current = current.Next;
                }

                return false;
            }
        }

        /// <summary>
        /// 检查指定实体是否处于存活状态。
        /// </summary>
        /// <param name="entity">要检查的实体。</param>
        /// <returns>存活返回 true；否则返回 false。</returns>
        public bool IsAlive(Entity entity)
        {
            if (entity == Entity.Empty)
            {
                return false;
            }

            lock (_syncRoot)
            {
                var current = _first;
                while (current is not null)
                {
                    if (current.IsAlive(entity))
                    {
                        return true;
                    }

                    current = current.Next;
                }

                return false;
            }
        }

        /// <summary>
        /// 实体版本段，负责管理一段连续实体索引的版本与分配状态。
        /// </summary>
        private sealed class EntityVersionSegment
        {
            /// <summary>
            /// 版本数组，按段内局部索引存储。
            /// </summary>
            private readonly uint[] _versions;

            /// <summary>
            /// 空闲局部索引栈。
            /// </summary>
            private readonly Stack<int> _freeSlots;

            /// <summary>
            /// 段起始全局索引。
            /// </summary>
            public int StartIndex { get; }

            /// <summary>
            /// 段结束全局索引。
            /// </summary>
            public int EndIndex { get; }

            /// <summary>
            /// 下一段引用。
            /// </summary>
            public EntityVersionSegment? Next { get; private set; }

            /// <summary>
            /// 当前段内存活实体数量。
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// 使用指定起始索引初始化版本段。
            /// </summary>
            /// <param name="startIndex">段起始全局索引。</param>
            public EntityVersionSegment(int startIndex)
            {
                _versions = new uint[DefaultSegmentSize];
                _freeSlots = new(DefaultSegmentSize);
                StartIndex = startIndex;
                EndIndex = startIndex + DefaultSegmentSize - 1;
                Count = 0;

                for (var localIndex = DefaultSegmentSize - 1; localIndex >= 0; localIndex--)
                {
                    if (StartIndex == 0 && localIndex == 0)
                    {
                        continue;
                    }

                    _freeSlots.Push(localIndex);
                }
            }

            /// <summary>
            /// 设置下一段。
            /// </summary>
            /// <param name="next">下一段实例。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetNext(EntityVersionSegment next)
            {
                Next = next;
            }

            /// <summary>
            /// 尝试从当前段分配一个实体。
            /// </summary>
            /// <param name="entity">分配成功时返回实体句柄。</param>
            /// <returns>分配成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRentEntity(out Entity entity)
            {
                if (_freeSlots.TryPop(out var localIndex))
                {
                    if (_versions[localIndex] == 0)
                    {
                        _versions[localIndex] = 1;
                    }

                    Count++;
                    entity = new Entity(StartIndex + localIndex, _versions[localIndex]);
                    return true;
                }

                entity = Entity.Empty;
                return false;
            }

            /// <summary>
            /// 尝试释放指定实体。
            /// </summary>
            /// <param name="entity">要释放的实体。</param>
            /// <returns>释放成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ReleaseEntity(Entity entity)
            {
                if (!TryGetLocalIndex(entity.Id, out var localIndex))
                {
                    return false;
                }

                if (_versions[localIndex] != entity.Version)
                {
                    return false;
                }

                Count--;
                var nextVersion = _versions[localIndex] + 1;
                _versions[localIndex] = nextVersion == 0 ? 1 : nextVersion;
                _freeSlots.Push(localIndex);
                return true;
            }

            /// <summary>
            /// 检查指定实体在当前段是否存活。
            /// </summary>
            /// <param name="entity">要检查的实体。</param>
            /// <returns>存活返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsAlive(Entity entity)
            {
                if (!TryGetLocalIndex(entity.Id, out var localIndex))
                {
                    return false;
                }

                return _versions[localIndex] == entity.Version;
            }

            /// <summary>
            /// 尝试将全局索引转换为段内局部索引。
            /// </summary>
            /// <param name="globalIndex">全局索引。</param>
            /// <param name="localIndex">转换成功时返回局部索引。</param>
            /// <returns>转换成功返回 true；否则返回 false。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryGetLocalIndex(int globalIndex, out int localIndex)
            {
                localIndex = globalIndex - StartIndex;
                return localIndex >= 0 && localIndex < DefaultSegmentSize;
            }
        }
    }
}