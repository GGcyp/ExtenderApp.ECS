using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Entities;

namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// Archetype 构造器。
    /// 用于按任意顺序组装组件掩码与关系掩码，并最终构建（或获取）对应的 <see cref="Archetype"/>。
    /// </summary>
    public struct ArchetypeBuilder
    {
        /// <summary>
        /// 原型管理器，用于在 <see cref="Build"/> 时获取或创建 Archetype。
        /// </summary>
        private readonly ArchetypeManager _archetypeManager;

        /// <summary>
        /// 构造中的组件掩码。
        /// </summary>
        private ComponentMask _componentMask;

        /// <summary>
        /// 构造中的关系掩码。
        /// </summary>
        private RelationMask _relationMask;

        /// <summary>
        /// 当前构造器中的组件掩码。
        /// </summary>
        public readonly ComponentMask ComponentMask => _componentMask;

        /// <summary>
        /// 当前构造器中的关系掩码。
        /// </summary>
        public readonly RelationMask RelationMask => _relationMask;

        /// <summary>
        /// 使用指定的 <see cref="ArchetypeManager"/> 初始化构造器。
        /// </summary>
        /// <param name="archetypeManager">原型管理器实例。</param>
        internal ArchetypeBuilder(ArchetypeManager archetypeManager)
        {
            _archetypeManager = archetypeManager;
            _componentMask = new();
            _relationMask = new();
        }

        /// <summary>
        /// 添加一个组件类型。
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddComponent(ComponentType componentType)
        {
            _componentMask.Add(componentType);
            return this;
        }

        /// <summary>
        /// 添加多个组件类型。
        /// </summary>
        /// <param name="componentTypes">组件类型集合。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddComponents(params ComponentType[] componentTypes)
        {
            for (int i = 0; i < componentTypes.Length; i++)
                _componentMask.Add(componentTypes[i]);
            return this;
        }

        /// <summary>
        /// 添加一个关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddRelation(RelationType relationType)
        {
            _relationMask.Add(relationType);
            return this;
        }

        /// <summary>
        /// 添加多个关系类型。
        /// </summary>
        /// <param name="relationTypes">关系类型集合。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddRelations(params RelationType[] relationTypes)
        {
            for (int i = 0; i < relationTypes.Length; i++)
                _relationMask.Add(relationTypes[i]);
            return this;
        }

        /// <summary>
        /// 移除一个组件类型。
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveComponent(ComponentType componentType)
        {
            _componentMask.Remove(componentType);
            return this;
        }

        /// <summary>
        /// 移除多个组件类型。
        /// </summary>
        /// <param name="componentTypes">组件类型集合。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveComponents(params ComponentType[] componentTypes)
        {
            for (int i = 0; i < componentTypes.Length; i++)
                _componentMask.Remove(componentTypes[i]);
            return this;
        }

        /// <summary>
        /// 移除一个关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveRelation(RelationType relationType)
        {
            _relationMask.Remove(relationType);
            return this;
        }

        /// <summary>
        /// 移除多个关系类型。
        /// </summary>
        /// <param name="relationTypes">关系类型集合。</param>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveRelations(params RelationType[] relationTypes)
        {
            for (int i = 0; i < relationTypes.Length; i++)
                _relationMask.Remove(relationTypes[i]);
            return this;
        }

        /// <summary>
        /// 添加一个泛型组件类型。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddComponent<T>()
        {
            _componentMask.Add(ComponentType.Create<T>());
            return this;
        }

        /// <summary>
        /// 添加两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddComponents<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            _componentMask.Add(ComponentType.Create<T1>());
            _componentMask.Add(ComponentType.Create<T2>());
            return this;
        }

        /// <summary>
        /// 移除一个泛型组件类型。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveComponent<T>()
        {
            _componentMask.Remove(ComponentType.Create<T>());
            return this;
        }

        /// <summary>
        /// 添加一个泛型关系类型。
        /// </summary>
        /// <typeparam name="T">关系类型。</typeparam>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder AddRelation<T>() where T : struct
        {
            _relationMask.Add(RelationType.Create<T>());
            return this;
        }

        /// <summary>
        /// 移除一个泛型关系类型。
        /// </summary>
        /// <typeparam name="T">关系类型。</typeparam>
        /// <returns>返回当前构造器，便于链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeBuilder RemoveRelation<T>() where T : struct
        {
            _relationMask.Remove(RelationType.Create<T>());
            return this;
        }

        /// <summary>
        /// 构建（或获取）与当前组件掩码和关系掩码对应的 Archetype。
        /// </summary>
        /// <returns>对应掩码的 Archetype 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype Build()
        {
            return _archetypeManager.GetOrCreateArchetype(_componentMask, _relationMask);
        }
    }
}