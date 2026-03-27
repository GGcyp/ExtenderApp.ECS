using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExtenderApp.ECS.Archetypes;
using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 命令回放器：负责将线程安全的命令缓冲区中的命令按时间顺序回放到主线程的实体系统中。
    ///
    /// 设计要点：
    /// - 命令由工作线程写入为二进制 chunk，回放在主线程执行以保证实体/原型操作的线程安全性；
    /// - 支持 "虚拟实体"（负 id）的映射：回放过程中会将虚拟实体映射为真实实体并维护映射表；
    /// - 在回放迁移时通过先在目标 Archetype 分配槽位、复制组件数据、再更新 EntityManager 映射的顺序，保证不会留下残留槽位或错位索引；
    /// - 对于并发产生的命令流，回放应尽量容错（忽略已删除的虚拟实体、跳过不完整命令）。
    /// </summary>
    internal class EntityCommandReader
    {
        // 最大单个命令数据长度（用于 ParseChunk 时的读取边界检查)
        private const int MaxCommandDataSize = 2048; // 可根据实际需求调整

        // Entity/Archetype 管理入口
        private readonly EntityManager _entityManager;

        private readonly ArchetypeManager _archetypeManager;
        private readonly EntityQueryManager _entityQueryManager;
        private readonly EntityCommandStorage _storage;

        // 用于临时收集待回放的 chunk
        private readonly List<CommandBufferChunk> _tempList;

        // 虚拟实体 -> 真实实体 映射表（仅在一轮回放内有效）
        private readonly SortedList<Entity, Entity> _virtualToRealities;

        /// <summary>
        /// 构造一个命令回放器实例。
        /// </summary>
        /// <param name="storage">命令缓冲存储（线程安全写入端）。</param>
        /// <param name="entityManager">实体管理器。</param>
        /// <param name="archetypeManager">Archetype 管理器。</param>
        public EntityCommandReader(EntityCommandStorage storage, EntityManager entityManager, ArchetypeManager archetypeManager, EntityQueryManager entityQueryManager)
        {
            _storage = storage;
            _entityManager = entityManager;
            _archetypeManager = archetypeManager;
            _entityQueryManager = entityQueryManager;

            _tempList = new(MaxCommandDataSize);
            _virtualToRealities = new(MaxCommandDataSize);
        }

        /// <summary>
        /// 从命令缓冲读取并回放所有已封存的命令。该方法必须在主线程上执行。
        /// 回放流程：收集 chunk -> 反向解析（恢复写入时间顺序）-> 逐条执行命令 -> 清理临时状态。
        /// </summary>
        public void ReadCommands()
        {
            _tempList.Clear();
            while (_storage.TryPopChunk(out var chunk))
            {
                _tempList.Add(chunk);
            }

            var current = _storage.CurrentChunk;
            bool hasCurrent = false;

            // sealed 是栈结构，反向解析以恢复时间顺序
            for (int i = _tempList.Count - 1; i >= 0; i--)
            {
                var c = _tempList[i];
                if (ReferenceEquals(c, current))
                {
                    hasCurrent = true;
                    continue;
                }

                ParseChunk(c, c.UsedBytes);
            }

            // current 最后解析
            if (hasCurrent)
            {
                ParseChunk(current, current.UsedBytes);
            }

            // 释放 sealed，重置 current
            bool resetCurrent = false;
            for (int i = 0; i < _tempList.Count; i++)
            {
                var c = _tempList[i];
                if (!resetCurrent && ReferenceEquals(c, current))
                {
                    _storage.ResetCurrentChunkAfterRead();
                    resetCurrent = true;
                }
                else
                {
                    c.Dispose();
                }
            }
            _tempList.Clear();

            // 一轮读取结束，清理映射状态
            _storage.Clear();
            _virtualToRealities.Clear();
        }

        /// <summary>
        /// 解析单个 chunk 并执行其中的命令。方法内部对每条命令做边界检查并调用对应的 Apply* 方法。
        /// 注意：该方法假设调用方在主线程，并且 chunk 内容来源可信（由命令缓冲写入端保证格式）。
        /// </summary>
        private unsafe void ParseChunk(CommandBufferChunk chunk, int usedBytes)
        {
            if (chunk == null || usedBytes <= 0)
                return;

            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int readableLength = Math.Min(usedBytes, chunk.Capacity);
            ReadOnlySpan<byte> span = new(chunk.Ptr, readableLength);

            int offset = 0;
            while (offset + headSize + entitySize <= span.Length)
            {
                EntityCommandHead head = MemoryMarshal.Read<EntityCommandHead>(span.Slice(offset, headSize));
                offset += headSize;

                Entity target = MemoryMarshal.Read<Entity>(span.Slice(offset, entitySize));
                offset += entitySize;

                switch (head.Type)
                {
                    case EntityCommandType.DestroyEntity:
                        {
                            ApplyDestroy(target);
                            break;
                        }

                    case EntityCommandType.RemoveComponent:
                        {
                            if (offset + sizeof(ushort) > span.Length)
                                return;

                            ushort typeIndex = MemoryMarshal.Read<ushort>(span.Slice(offset, sizeof(ushort)));
                            offset += sizeof(ushort);

                            var ct = ComponentType.GetCommandType(typeIndex);
                            ApplyRemoveComponent(target, ct);
                            break;
                        }

                    case EntityCommandType.AddComponent:
                    case EntityCommandType.SetComponent:
                        {
                            if (offset + sizeof(ushort) > span.Length)
                                return;

                            ushort typeIndex = MemoryMarshal.Read<ushort>(span.Slice(offset, sizeof(ushort)));
                            offset += sizeof(ushort);

                            int dataLen = head.DataLength;
                            if (offset + dataLen > span.Length)
                                return;

                            var ct = ComponentType.GetCommandType(typeIndex);
                            nint dataPtr;
                            fixed (byte* src = &MemoryMarshal.GetReference(span.Slice(offset, dataLen)))
                            {
                                dataPtr = (nint)src;
                                ApplySetLike(target, ct, dataPtr, dataLen);
                            }

                            offset += dataLen;
                            break;
                        }

                    case EntityCommandType.DestroyEntitiesForQuery:
                        {
                            int dataLen = head.DataLength;
                            if (offset + dataLen > span.Length)
                                return;

                            if (TryReadEntityQueryDesc(span, offset, out var desc))
                            {
                                ApplyDestroyEntitiesForQuery(desc);
                            }

                            offset += dataLen;
                            break;
                        }

                    default:
                        return;
                }
            }
        }

        /// <summary>
        /// 销毁实体。若 target 是虚拟实体，则先尝试从虚拟->真实映射表中拿到真实实体并销毁；否则直接销毁真实实体。
        /// </summary>
        private void ApplyDestroy(Entity target)
        {
            if (target.IsVirtual)
            {
                if (!_virtualToRealities.Remove(target, out target))
                    return;
            }
            _entityManager.DestroyEntity(target);
        }

        /// <summary>
        /// 移除实体上的组件（无数据）。实现要点：
        /// - 支持虚拟实体解析为真实实体；
        /// - 计算目标掩码并决定是否迁移到新 Archetype 或直接移除到空 Archetype；
        /// - 在发生尾部交换时更新被移动实体在 EntityManager 中的索引。
        /// </summary>
        private void ApplyRemoveComponent(Entity target, ComponentType ct)
        {
            if (target.IsVirtual && !TryAccessRealityEntity(target, out target))
                return;

            ComponentMask newMask = new();
            Entity changedEntity = Entity.Empty;
            if (_entityManager.TryGetArchetype(target, out var archetype, out var archetypeIndex) &&
            archetype != null)
            {
                newMask.SetComponents(archetype.ComponentMask);
                newMask.Remove(ct);
                if (newMask == archetype.ComponentMask)
                    return;

                if (newMask.IsEmpty)
                {
                    if (archetype.TryRemoveEntity(archetypeIndex, out changedEntity) &&
                        !changedEntity.IsEmpty)
                    {
                        _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
                    }
                    _entityManager.TryChangedArchetype(target, null, 0);
                    return;
                }
            }

            if (!newMask.IsEmpty)
            {
                if (archetype?.TryRemoveEntity(archetypeIndex, out changedEntity) == true &&
                    !changedEntity.IsEmpty)
                {
                    _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
                }

                archetype = null;
                archetypeIndex = 0;
            }
            else
            {
                var newArchetype = _archetypeManager.GetOrCreateArchetype(newMask);
                var newIndex = newArchetype.AddEntity(target);
                if (archetype?.TryCopyToAndRemove(archetypeIndex, newArchetype, newIndex, out changedEntity) == true &&
                    !changedEntity.IsEmpty)
                {
                    _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
                }
                archetype = newArchetype;
                archetypeIndex = newIndex;
            }

            _entityManager.TryChangedArchetype(target, archetype, archetypeIndex);
        }

        /// <summary>
        /// 添加组件（无数据）或在已存在时更新组件；若 target 为虚拟实体则可按需创建真实实体。
        /// 回放时优先计算目标掩码并在目标 Archetype 分配槽位，然后复制并更新 EntityManager 映射，最后拷贝数据（若有）。
        /// </summary>
        private void ApplyAddComponentNoData(Entity target, ComponentType ct)
        {
            //创建实体
            if (target.IsVirtual && !TryAccessRealityEntity(target, out target, true))
                return;

            ComponentMask newMask = new(ct);
            if (_entityManager.TryGetArchetype(target, out var archetype, out var archetypeIndex) &&
                archetype != null)
            {
                newMask.SetComponents(archetype.ComponentMask);
                if (newMask == archetype.ComponentMask)
                    return;
            }

            var newArchetype = _archetypeManager.GetOrCreateArchetype(newMask);
            var newIndex = newArchetype.AddEntity(target);
            if (archetype?.TryCopyToAndRemove(archetypeIndex, newArchetype, newIndex, out var changedEntity) == true)
            {
                _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
            }
            _entityManager.TryChangedArchetype(target, newArchetype, newIndex);
        }

        /// <summary>
        /// 类似 AddComponent，但带二进制数据拷贝（Set/SetLike）。
        /// </summary>
        private void ApplySetLike(Entity target, ComponentType ct, nint dataPtr, int dataSize)
        {
            //创建实体
            if (target.IsVirtual && !TryAccessRealityEntity(target, out target, true))
                return;

            ComponentMask newMask = new(ct);
            if (_entityManager.TryGetArchetype(target, out var archetype, out var archetypeIndex) &&
                archetype != null)
            {
                newMask.SetComponents(archetype.ComponentMask);
                if (newMask == archetype.ComponentMask)
                    return;
            }

            var newArchetype = _archetypeManager.GetOrCreateArchetype(newMask);
            var newIndex = newArchetype.AddEntity(target);
            if (archetype?.TryCopyToAndRemove(archetypeIndex, newArchetype, newIndex, out var changedEntity) == true)
            {
                _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
            }
            _entityManager.TryChangedArchetype(target, newArchetype, newIndex);

            if (dataSize > 0 && newArchetype.TryGetChunk(ct, newIndex, out var chunk, out var localIndex))
            {
                chunk.CopiedUnsafe(localIndex, dataPtr, dataSize);
            }
        }

        /// <summary>
        /// 修改实体组件掩码：直接在目标 Archetype 分配槽位并迁移数据，然后更新映射。
        /// </summary>
        private void ApplyModifyComponent(Entity target, ComponentMask mask)
        {
            // Modify 创建实体
            if (target.IsVirtual && !TryAccessRealityEntity(target, out target, true))
                return;

            var newArchetype = _archetypeManager.GetOrCreateArchetype(mask);
            var newIndex = newArchetype.AddEntity(target);
            if (_entityManager.TryGetArchetype(target, out var archetype, out var archetypeIndex) &&
                archetype != null)
            {
                if (archetype?.TryCopyToAndRemove(archetypeIndex, newArchetype, newIndex, out var changedEntity) == true)
                {
                    _entityManager.TryChangedArchetypeIndex(changedEntity, archetypeIndex);
                }
            }
            _entityManager.TryChangedArchetype(target, newArchetype, newIndex);
        }

        /// <summary>
        /// 将虚拟实体解析（或创建）为真实实体。返回 true 并通过 out 返回真实实体。
        /// - 若映射表中存在映射则直接返回；
        /// - 若 createIfMissing 为 true，则创建新的真实实体并记录映射。
        /// </summary>
        private bool TryAccessRealityEntity(Entity entity, out Entity real, bool createIfMissing = false)
        {
            real = entity;
            if (_virtualToRealities.TryGetValue(entity, out real))
                return true;

            if (!createIfMissing)
                return false;

            real = _entityManager.CreateEntity();
            _virtualToRealities[entity] = real;
            return true;
        }

        /// <summary>
        /// 从 span 的指定偏移处读取序列化的 EntityQueryDesc。
        /// 序列化顺序为：Query(ComponentMask), All(ComponentMask), Any(ComponentMask), None(ComponentMask), Relation(RelationMask)
        /// </summary>
        private bool TryReadEntityQueryDesc(ReadOnlySpan<byte> span, int offset, out EntityQueryDesc desc)
        {
            desc = default;
            int compMaskSize = Marshal.SizeOf<ComponentMask>();
            int relationSize = Marshal.SizeOf<Entities.RelationMask>();
            int total = compMaskSize * 4 + relationSize;
            if (offset + total > span.Length)
                return false;

            try
            {
                var query = MemoryMarshal.Read<ComponentMask>(span.Slice(offset, compMaskSize));
                offset += compMaskSize;
                var all = MemoryMarshal.Read<ComponentMask>(span.Slice(offset, compMaskSize));
                offset += compMaskSize;
                var any = MemoryMarshal.Read<ComponentMask>(span.Slice(offset, compMaskSize));
                offset += compMaskSize;
                var none = MemoryMarshal.Read<ComponentMask>(span.Slice(offset, compMaskSize));
                offset += compMaskSize;
                var rel = MemoryMarshal.Read<Entities.RelationMask>(span.Slice(offset, relationSize));

                // EntityQueryDesc constructor validates Query is not empty; handle exceptions by returning false
                desc = new EntityQueryDesc(query, all, any, none, rel);
                return true;
            }
            catch
            {
                desc = default;
                return false;
            }
        }

        /// <summary>
        /// 回放：根据查询描述删除匹配的实体。 方法体留空，由调用者实现具体删除逻辑。
        /// </summary>
        private void ApplyDestroyEntitiesForQuery(EntityQueryDesc desc)
        {
            var query = _entityQueryManager.GetOrCreateQuery(desc);
            _entityManager.DestroyEntitiesForQuery(query);
        }
    }
}