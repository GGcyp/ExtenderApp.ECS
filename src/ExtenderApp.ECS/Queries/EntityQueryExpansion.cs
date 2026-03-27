using ExtenderApp.ECS.Entities;
using ExtenderApp.ECS.Threading;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 针对 EntityQuery 的扩展方法集合。
    ///
    /// 说明：该类包含一些面向查询的便捷操作（例如按查询删除实体）。
    /// 这些操作通常只应在主线程上执行，因为会直接作用于实体管理器并修改底层原型/段数据结构。
    /// </summary>
    internal static class EntityQueryExpansion
    {
        /// <summary>
        /// 根据给定的查询遍历所有匹配的实体并逐一销毁。
        /// </summary>
        /// <param name="entitiesManager">用于执行实体销毁的实体管理器。</param>
        /// <param name="query">要执行删除操作的查询（其 <see cref="EntityQueryCore"/> 提供匹配的 Archetype 段）。</param>
        /// <remarks>
        /// - 要求：此方法必须在主线程调用（内部通过 <see cref="MainThreadDetector.ThrowIfNotMainThread"/> 校验）。
        /// - 遍历顺序：对查询核心返回的每个 Archetype 段，从后向前遍历段和段内实体，这样在按尾部交换移除实体时不会影响尚未处理的实体索引。
        /// - 语义：方法直接调用 <see cref="EntityManager.DestroyEntity"/> 执行销毁操作，因此会触发所有与销毁相关的资源回收与迁移逻辑。
        /// - 性能与安全：此操作会进行结构性修改（移除实体），应避免在并行遍历期间调用；如需并发产生删除指令，请使用命令缓冲（EntityCommandBuffer）在工作线程记录命令并在主线程统一回放。
        /// </remarks>
        public static void DestroyEntitiesForQuery(this EntityManager entitiesManager, in EntityQuery query)
        {
            // 强制校验主线程
            MainThreadDetector.ThrowIfNotMainThread();

            var current = query.Core.GetArchetypeSegmentHead();

            while (current != null)
            {
                var archetype = current.Archetype;
                var entities = archetype.Entities;

                // 对实体段按倒序遍历以保证在移除时（可能的尾交换）不会跳过或重复处理实体
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    var segmentInfo = entities[i];
                    var infoEntities = segmentInfo.Entities;
                    for (int j = infoEntities.Length - 1; j >= 0; j--)
                    {
                        var entity = infoEntities[j];
                        entitiesManager.DestroyEntity(entity);
                    }
                }

                current = current.Next;
            }
        }
    }
}