using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Components
{
    internal static class ComponentManager
    {
        #region 组件类型位索引注册表

        /// <summary>
        /// 从组件类型到位索引的映射字典（线程安全）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ushort> _typeToIndex = new();

        /// <summary>
        /// 从位索引到组件类型的反向映射字典（线程安全）。
        /// </summary>
        private static readonly ConcurrentDictionary<ushort, Type> _indexToType = new();

        /// <summary>
        /// 下一个将分配的位索引（使用 Interlocked.Increment 原子递增）。
        /// 初始值为 1，分配时会减 1 来得到从 1 开始的索引值。
        /// </summary>
        private static int nextIndex = 1;

        /// <summary>
        /// 获取或创建组件类型对应的位索引。如果该类型尚未注册则分配一个新的索引。
        /// </summary>
        /// <typeparam name="T">组件类型，必须为值类型并实现 IComponent。</typeparam>
        /// <returns>该组件类型对应的位索引（从 0 开始）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetOrCreateIndex<T>() where T : struct, IComponent
        {
            var componentType = typeof(T);
            return GetOrCreateIndex(componentType);
        }

        /// <summary>
        /// 获取或创建组件类型对应的位索引。如果该类型尚未注册则分配一个新的索引。
        /// </summary>
        /// <param name="componentType">要查询或注册的组件类型。</param>
        /// <returns>该组件类型对应的位索引（从 0 开始）。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetOrCreateIndex(Type componentType)
        {
            if (!_typeToIndex.TryGetValue(componentType, out var index))
            {
                index = _typeToIndex.GetOrAdd(componentType, static _ => (ushort)(Interlocked.Increment(ref nextIndex) - 1));
                _indexToType.TryAdd(index, componentType);
            }
            return index;
        }

        /// <summary>
        /// 根据位索引获取注册的组件类型。
        /// </summary>
        /// <param name="index">要查询的位索引。</param>
        /// <returns>对应的组件类型 Type。</returns>
        /// <exception cref="ArgumentOutOfRangeException">当指定索引未注册时抛出。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetTypeByIndex(ushort index)
        {
            if (_indexToType.TryGetValue(index, out var type))
            {
                return type;
            }
            throw new ArgumentOutOfRangeException(nameof(index), "无效的组件类型索引。");
        }

        #endregion 组件类型位索引注册表
    }
}