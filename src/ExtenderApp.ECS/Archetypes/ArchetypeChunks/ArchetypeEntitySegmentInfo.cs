using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 单个实体段的信息结构。
    /// </summary>
    internal struct ArchetypeEntitySegmentInfo
    {
        /// <summary>
        /// 段内组件句柄数组。
        /// </summary>
        public ComponentHandle?[] ComponentHandles;

        /// <summary>
        /// 段内当前实体数量。
        /// </summary>
        public int Count;

        /// <summary>
        /// 段内实体数组。
        /// </summary>
        public Entity[] Entities;

        /// <summary>
        /// 段起始全局索引。
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// 初始化 <see cref="ArchetypeEntitySegmentInfo" /> 的新实例。
        /// </summary>
        /// <param name="entities">实体数组。</param>
        /// <param name="componentHandles">组件句柄数组。</param>
        /// <param name="startIndex">段起始全局索引。</param>
        public ArchetypeEntitySegmentInfo(Entity[] entities, ComponentHandle?[] componentHandles, int startIndex)
        {
            Entities = entities;
            StartIndex = startIndex;
            Count = 0;
            ComponentHandles = componentHandles;
        }

        /// <summary>
        /// 获取一个值，指示当前段是否仍有可用空间。
        /// </summary>
        public bool HasFree => Count < Entities.Length;

        /// <summary>
        /// 按段内索引移除实体，采用尾部交换策略。
        /// </summary>
        public void Remove(int index, out ComponentHandle? removedHandle, out Entity changedEntity, out ComponentHandle? changedHandle)
        {
            changedEntity = Entity.Empty;
            changedHandle = default!;
            removedHandle = ComponentHandles[index];

            if (index < 0 || index >= Count)
                return;

            int last = Count - 1;
            if (index == last)
            {
                Count--;
                return;
            }

            Entities[index] = changedEntity = Entities[last];
            ComponentHandles[index] = changedHandle = ComponentHandles[last];

            if (changedHandle != null)
                changedHandle.GlobalIndex = StartIndex + index;

            Count--;
        }

        /// <summary>
        /// 按索引集合批量移除实体。
        /// </summary>
        public void RemoveRange(Span<int> indexs, Span<ComponentHandle?> removedHandles, Span<Entity> changedEntities, Span<ComponentHandle?> changedHandles)
        {
            for (int outIdx = 0; outIdx < indexs.Length; outIdx++)
            {
                int index = indexs[outIdx];
                Remove(index, out var removedHandle, out var changedEntity, out var changedHandle);
                removedHandles[outIdx] = removedHandle;
                changedEntities[outIdx] = changedEntity;
                changedHandles[outIdx] = changedHandle;
            }
        }

        /// <summary>
        /// 尝试添加实体到当前段。
        /// </summary>
        public bool TryAdd(Entity entity, ComponentHandle? handle, out int index)
        {
            index = -1;
            // 修复：应使用 '<'，避免当 Count == Entities.Length 时写入越界
            if (Count < Entities.Length)
            {
                index = Count++;
                Entities[index] = entity;
                ComponentHandles[index] = handle;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 批量向当前段写入实体。
        /// </summary>
        public bool TryAdds(Span<Entity> entities, Span<int> globalIndexSpan, out int addCount)
        {
            var available = Entities.Length - Count;
            addCount = Math.Min(available, entities.Length);

            if (addCount > 0)
            {
                entities.Slice(0, addCount).CopyTo(Entities.AsSpan(Count, addCount));
                for (int i = 0; i < addCount; i++)
                    globalIndexSpan[i] = StartIndex + Count + i;

                Count += addCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 批量向当前段写入实体。
        /// </summary>
        public bool TryAdds(Span<Entity> entities, Span<int> globalIndexSpan, Span<ComponentHandle?> handles, out int addCount)
        {
            var available = Entities.Length - Count;
            addCount = Math.Min(available, entities.Length);

            if (addCount > 0)
            {
                entities.Slice(0, addCount).CopyTo(Entities.AsSpan(Count, addCount));
                handles.Slice(0, addCount).CopyTo(ComponentHandles.AsSpan(Count, addCount));

                for (int i = 0; i < addCount; i++)
                    globalIndexSpan[i] = StartIndex + Count + i;

                Count += addCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断指定全局索引是否位于当前段，并返回局部索引。
        /// </summary>
        public bool TryWithinSegment(int globalIndex, out int localIndex)
        {
            localIndex = globalIndex - StartIndex;
            return globalIndex >= StartIndex && globalIndex < StartIndex + Count;
        }
    }
}