using System;
using System.Collections.Generic;
using ExtenderApp.ECS.Interfaces;

namespace ExtenderApp.ECS.Managers
{
    /// <summary>
    /// 管理组件的存储与按实体查询。
    /// </summary>
    public class ComponentManager
    {
        /// <summary>
        /// 内部存储：组件类型 ->（实体标识 -> 组件实例）。
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, IComponent>> _components = new();

        /// <summary>
        /// 为实体添加或替换组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entityId">实体标识。</param>
        /// <param name="component">要存储的组件实例。</param>
        public void AddComponent<T>(int entityId, T component) where T : IComponent
        {
            var type = typeof(T);
            if (!_components.TryGetValue(type, out var byEntity))
            {
                byEntity = new Dictionary<int, IComponent>();
                _components[type] = byEntity;
            }

            byEntity[entityId] = component;
        }

        /// <summary>
        /// 尝试获取实体上的组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entityId">实体标识。</param>
        /// <returns>存在时返回组件实例；否则返回 null。</returns>
        public T? GetComponent<T>(int entityId) where T : class, IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var byEntity) && byEntity.TryGetValue(entityId, out var comp))
                return comp as T;

            return null;
        }

        /// <summary>
        /// 检查实体是否拥有 T 类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entityId">实体标识。</param>
        /// <returns>存在时为 true；否则为 false。</returns>
        public bool HasComponent<T>(int entityId) where T : IComponent
        {
            var type = typeof(T);
            return _components.TryGetValue(type, out var byEntity) && byEntity.ContainsKey(entityId);
        }

        /// <summary>
        /// 从实体上移除 T 类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entityId">实体标识。</param>
        /// <returns>移除成功时为 true；否则为 false。</returns>
        public bool RemoveComponent<T>(int entityId) where T : IComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var byEntity))
            {
                var removed = byEntity.Remove(entityId);
                if (byEntity.Count == 0)
                    _components.Remove(type);
                return removed;
            }

            return false;
        }

        /// <summary>
        /// 获取同时拥有所有指定组件类型的实体。
        /// </summary>
        /// <param name="types">要匹配的组件类型数组。</param>
        /// <returns>满足条件的实体标识可枚举序列。</returns>
        public IEnumerable<int> GetEntitiesWith(params Type[] types)
        {
            if (types == null || types.Length == 0)
                yield break;

            // 以第一个组件类型的实体集合作为初始候选集
            if (!_components.TryGetValue(types[0], out var firstMap))
                yield break;

            foreach (var kv in firstMap)
            {
                var entityId = kv.Key;
                var hasAll = true;
                for (int i = 1; i < types.Length; i++)
                {
                    if (!_components.TryGetValue(types[i], out var map) || !map.ContainsKey(entityId))
                    {
                        hasAll = false;
                        break;
                    }
                }

                if (hasAll)
                    yield return entityId;
            }
        }

        /// <summary>
        /// 泛型辅助方法：获取同时拥有两个指定组件类型的实体。
        /// </summary>
        /// <typeparam name="T1">第一个组件类型。</typeparam>
        /// <typeparam name="T2">第二个组件类型。</typeparam>
        /// <returns>实体标识的可枚举序列。</returns>
        public IEnumerable<int> GetEntitiesWith<T1, T2>() where T1 : IComponent where T2 : IComponent
        {
            return GetEntitiesWith(typeof(T1), typeof(T2));
        }
    }
}
