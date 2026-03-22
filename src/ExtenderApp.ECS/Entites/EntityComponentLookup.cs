using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Threading;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 实体组件查找器。 基于 <see cref="ComponentHandle" /> 提供快速组件读取与写入能力。
    /// </summary>
    public readonly struct EntityComponentLookup
    {
        /// <summary>
        /// 底层组件句柄。
        /// </summary>
        private readonly ComponentHandle _handle;

        /// <summary>
        /// 初始化 <see cref="EntityComponentLookup" /> 的新实例。
        /// </summary>
        /// <param name="handle">组件句柄。</param>
        internal EntityComponentLookup(ComponentHandle handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// 获取指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>组件值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>() where T : struct
        {
            ThrowIfNotMainThread();
            return _handle.GetComponent<T>();
        }

        /// <summary>
        /// 设置指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="component">组件值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(T component) where T : struct
        {
            ThrowIfNotMainThread();
            _handle.SetComponent(component);
        }

        /// <summary>
        /// 尝试获取指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="component">输出组件值。</param>
        /// <returns>获取成功返回 true；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(out T component) where T : struct
        {
            ThrowIfNotMainThread();

            component = default;
            try
            {
                component = _handle.GetComponent<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 校验当前线程为主线程，否则抛出异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfNotMainThread() => MainThreadDetector.ThrowIfNotMainThread();
    }
}