using ExtenderApp.ECS.Commands;
using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统运行时上下文（SystemContext）。
    /// </summary>
    public struct SystemContext
    {
        private readonly SharedComponentManager _sharedComponentManager;

        /// <summary>
        /// 实体命令缓冲区（EntityCommandBuffer）。
        /// 用于在系统执行期间记录对实体/组件的结构性更改（新增/移除组件、创建/销毁实体等），
        /// 由调度器在适当时机（通常为帧末或同步点）统一回放以保证线程安全。
        /// </summary>
        public EntityCommandBuffer CommandBuffer;

        internal SystemContext(SharedComponentManager sharedComponentManager, EntityCommandBuffer commandBuffer)
        {
            _sharedComponentManager = sharedComponentManager;
            CommandBuffer = commandBuffer;
        }

        public T GetSharedComponent<T>() where T : struct
        {
            return _sharedComponentManager.Get<T>();
        }

        public bool TryGetSharedComponent<T>(out T component) where T : struct
        {
            return _sharedComponentManager.TryGet(out component);
        }
    }
}