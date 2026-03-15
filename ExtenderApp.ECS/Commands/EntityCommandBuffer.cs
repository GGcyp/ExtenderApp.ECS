using ExtenderApp.ECS.Interfaces;
using ExtenderApp.ECS.Managers;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 类似 Unity ECB 的命令缓冲区。 用于记录实体与组件的结构变更，并在回放阶段统一执行。
    /// </summary>
    public sealed class EntityCommandBuffer
    {
        /// <summary>
        /// 已记录命令列表。
        /// </summary>
        private readonly List<IEntityCommand> _commands = [];

        /// <summary>
        /// 临时实体的下一个负数标识。
        /// </summary>
        private int _nextDeferredEntityId = -1;

        /// <summary>
        /// 标记是否已执行过回放。
        /// </summary>
        private bool _playedBack;

        /// <summary>
        /// 记录创建实体命令，并返回可用于后续命令的临时实体句柄。
        /// </summary>
        /// <returns>临时实体句柄。</returns>
        public Entity CreateEntity()
        {
            EnsureNotPlayedBack();
            var deferredEntity = new Entity(_nextDeferredEntityId--, 1);
            _commands.Add(new CreateEntityCommand(deferredEntity));
            return deferredEntity;
        }

        /// <summary>
        /// 记录销毁实体命令。
        /// </summary>
        /// <param name="entity">要销毁的实体。</param>
        public void DestroyEntity(Entity entity)
        {
            EnsureNotPlayedBack();
            _commands.Add(new DestroyEntityCommand(entity));
        }

        /// <summary>
        /// 记录添加组件命令。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件实例。</param>
        public void AddComponent<T>(Entity entity, T component) where T : IComponent
        {
            ArgumentNullException.ThrowIfNull(component);
            EnsureNotPlayedBack();
            _commands.Add(new AddComponentCommand<T>(entity, component));
        }

        /// <summary>
        /// 记录设置组件命令。 设置行为等价于添加或覆盖组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件实例。</param>
        public void SetComponent<T>(Entity entity, T component) where T : IComponent
        {
            AddComponent(entity, component);
        }

        /// <summary>
        /// 记录移除组件命令。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        public void RemoveComponent<T>(Entity entity) where T : IComponent
        {
            EnsureNotPlayedBack();
            _commands.Add(new RemoveComponentCommand<T>(entity));
        }

        /// <summary>
        /// 将所有命令按记录顺序回放到管理器。
        /// </summary>
        /// <param name="entityManager">实体管理器。</param>
        /// <param name="componentManager">组件管理器。</param>
        public void Playback(EntityManager entityManager, ComponentManager componentManager)
        {
            ArgumentNullException.ThrowIfNull(entityManager);
            ArgumentNullException.ThrowIfNull(componentManager);
            EnsureNotPlayedBack();

            var deferredEntityMap = new Dictionary<int, Entity>();
            for (var i = 0; i < _commands.Count; i++)
            {
                _commands[i].Playback(entityManager, componentManager, deferredEntityMap);
            }

            _commands.Clear();
            _playedBack = true;
        }

        /// <summary>
        /// 清空已记录但尚未回放的命令。
        /// </summary>
        public void Clear()
        {
            EnsureNotPlayedBack();
            _commands.Clear();
            _nextDeferredEntityId = -1;
        }

        /// <summary>
        /// 确保当前缓冲区尚未回放。
        /// </summary>
        private void EnsureNotPlayedBack()
        {
            if (_playedBack)
            {
                throw new InvalidOperationException("当前 EntityCommandBuffer 已回放，不能继续写入或重复回放。请创建新的缓冲区实例。");
            }
        }

        /// <summary>
        /// 解析实体句柄：若为临时实体则映射为真实实体。
        /// </summary>
        /// <param name="entity">原始实体句柄。</param>
        /// <param name="deferredEntityMap">临时实体映射表。</param>
        /// <returns>真实实体句柄。</returns>
        private static Entity ResolveEntity(Entity entity, IReadOnlyDictionary<int, Entity> deferredEntityMap)
        {
            if (entity == Entity.Empty)
            {
                return entity;
            }

            if (entity.Id >= 0)
            {
                return entity;
            }

            if (deferredEntityMap.TryGetValue(entity.Id, out var mappedEntity))
            {
                return mappedEntity;
            }

            throw new InvalidOperationException($"未找到临时实体映射：{entity}");
        }

        /// <summary>
        /// 命令接口。
        /// </summary>
        private interface IEntityCommand
        {
            /// <summary>
            /// 执行命令。
            /// </summary>
            /// <param name="entityManager">实体管理器。</param>
            /// <param name="componentManager">
            /// 组件管理器。
            /// </param>
            /// <param name="deferredEntityMap">
            /// 临时实体映射表。
            /// </param>
            void Playback(EntityManager entityManager, ComponentManager componentManager, Dictionary<int, Entity> deferredEntityMap);
        }

        /// <summary>
        /// 创建实体命令。
        /// </summary>
        /// <param name="deferredEntity">临时实体。</param>
        private sealed class CreateEntityCommand(Entity deferredEntity) : IEntityCommand
        {
            /// <summary>
            /// 临时实体句柄。
            /// </summary>
            private readonly Entity _deferredEntity = deferredEntity;

            /// <summary>
            /// 执行创建实体命令。
            /// </summary>
            /// <param name="entityManager">实体管理器。</param>
            /// <param name="componentManager">
            /// 组件管理器。
            /// </param>
            /// <param name="deferredEntityMap">
            /// 临时实体映射表。
            /// </param>
            public void Playback(EntityManager entityManager, ComponentManager componentManager, Dictionary<int, Entity> deferredEntityMap)
            {
                var realEntity = entityManager.CreateEntity();
                deferredEntityMap[_deferredEntity.Id] = realEntity;
            }
        }

        /// <summary>
        /// 销毁实体命令。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private sealed class DestroyEntityCommand(Entity entity) : IEntityCommand
        {
            /// <summary>
            /// 目标实体。
            /// </summary>
            private readonly Entity _entity = entity;

            /// <summary>
            /// 执行销毁实体命令。
            /// </summary>
            /// <param name="entityManager">实体管理器。</param>
            /// <param name="componentManager">
            /// 组件管理器。
            /// </param>
            /// <param name="deferredEntityMap">
            /// 临时实体映射表。
            /// </param>
            public void Playback(EntityManager entityManager, ComponentManager componentManager, Dictionary<int, Entity> deferredEntityMap)
            {
                var resolved = ResolveEntity(_entity, deferredEntityMap);
                componentManager.RemoveAllComponents(resolved.Id);
                entityManager.DestroyEntity(resolved);
            }
        }

        /// <summary>
        /// 添加组件命令。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        private sealed class AddComponentCommand<T>(Entity entity, T component) : IEntityCommand where T : IComponent
        {
            /// <summary>
            /// 目标实体。
            /// </summary>
            private readonly Entity _entity = entity;

            /// <summary>
            /// 组件实例。
            /// </summary>
            private readonly T _component = component;

            /// <summary>
            /// 执行添加组件命令。
            /// </summary>
            /// <param name="entityManager">实体管理器。</param>
            /// <param name="componentManager">
            /// 组件管理器。
            /// </param>
            /// <param name="deferredEntityMap">
            /// 临时实体映射表。
            /// </param>
            public void Playback(EntityManager entityManager, ComponentManager componentManager, Dictionary<int, Entity> deferredEntityMap)
            {
                var resolved = ResolveEntity(_entity, deferredEntityMap);
                componentManager.AddComponent(resolved.Id, _component);
            }
        }

        /// <summary>
        /// 移除组件命令。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        private sealed class RemoveComponentCommand<T>(Entity entity) : IEntityCommand where T : IComponent
        {
            /// <summary>
            /// 目标实体。
            /// </summary>
            private readonly Entity _entity = entity;

            /// <summary>
            /// 执行移除组件命令。
            /// </summary>
            /// <param name="entityManager">实体管理器。</param>
            /// <param name="componentManager">
            /// 组件管理器。
            /// </param>
            /// <param name="deferredEntityMap">
            /// 临时实体映射表。
            /// </param>
            public void Playback(EntityManager entityManager, ComponentManager componentManager, Dictionary<int, Entity> deferredEntityMap)
            {
                var resolved = ResolveEntity(_entity, deferredEntityMap);
                componentManager.RemoveComponent<T>(resolved.Id);
            }
        }
    }
}