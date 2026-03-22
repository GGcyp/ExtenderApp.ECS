using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 组件句柄：用于根据全局索引与组件掩码快速定位并读写实体组件。
    /// </summary>
    internal class ComponentHandle
    {
        /// <summary>
        /// 所属句柄池，用于回收当前句柄实例。
        /// </summary>
        private readonly ComponentHandlePool _pool;

        /// <summary>
        /// 当前句柄关联的块管理器。
        /// </summary>
        public ArchetypeChunkManager Manager;

        /// <summary>
        /// 当前句柄对应实体的全局索引。
        /// </summary>
        public int GlobalIndex;

        /// <summary>
        /// 当前句柄对应实体的组件掩码。
        /// </summary>
        public ComponentMask ComponentTypes;

        /// <summary>
        /// 初始化 <see cref="ComponentHandle" /> 的新实例。
        /// </summary>
        /// <param name="pool">句柄池实例。</param>
        public ComponentHandle(ComponentHandlePool pool)
        {
            Manager = default!;
            GlobalIndex = -1;
            _pool = pool;
        }

        /// <summary>
        /// 设置指定类型组件的值。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="component">要写入的组件值。</param>
        public void SetComponent<T>(T component) where T : struct
        {
            if (ComponentTypes.TryGetEncodedPosition<T>(out var position) &&
                Manager.TryFindChunkForGlobalIndex(position, GlobalIndex, out var chunk, out int localIndex) &&
                chunk is ArchetypeChunk<T> c)
            {
                c.SetComponent(localIndex, component);
                return;
            }

            ThrowInvalidComponentType<T>();
        }

        /// <summary>
        /// 获取指定类型组件的值。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>组件值。</returns>
        public T GetComponent<T>() where T : struct
        {
            if (ComponentTypes.TryGetEncodedPosition<T>(out var position) &&
                Manager.TryFindChunkForGlobalIndex(position, GlobalIndex, out var chunk, out int localIndex) &&
                chunk is ArchetypeChunk<T> c)
            {
                return c.GetComponent(localIndex);
            }

            ThrowInvalidComponentType<T>();
            return default;
        }

        /// <summary>
        /// 归还当前句柄到对象池，并重置运行时状态。
        /// </summary>
        public void Return()
        {
            Manager = default!;
            GlobalIndex = -1;
            _pool.Return(this);
        }

        /// <summary>
        /// 抛出组件类型不匹配异常。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        private void ThrowInvalidComponentType<T>()
        {
            throw new InvalidOperationException($"当前实体不包含类型为 {typeof(T).Name} 的组件。");
        }
    }
}