using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 实体查询构建器：用于以流式接口构造实体查询描述符。
    /// 
    /// 功能：
    /// - 通过 `With` 系列方法指定要查询的组件集合（最多支持 5 个组件）。
    /// - 通过 `WithAll` 指定必须全部存在的组件掩码。
    /// - 通过 `WithAny` 指定任意匹配的组件掩码（满足任一即可）。
    /// - 通过 `WithNone` 指定必须不包含的组件掩码。
    /// - 最终调用 `Build` 通过 `EntityQueryManager` 获取或创建对应的 `EntityQuery` 实例。
    /// 
    /// 限制：内部对每个掩码的组件数量进行了上限检查（由 `MaxCount` 控制），超过将抛出 <see cref="ArgumentOutOfRangeException"/>。
    /// </summary>
    public ref struct EntityQueryBuilder
    {
        private readonly EntityQueryManager _queryManager;

        /// <summary>
        /// 每个查询掩码允许的最大组件数。
        /// </summary>
        private const int MaxCount = 5;

        /// <summary>
        /// 获取表示查询条件的组件掩码引用。
        /// </summary>
        private ComponentMask query;

        /// <summary>
        /// 获取表示必须全部匹配的组件掩码引用。
        /// </summary>
        private ComponentMask all;

        /// <summary>
        /// 获取表示任意匹配的组件掩码引用（只读）。
        /// </summary>
        private ComponentMask any;

        /// <summary>
        /// 获取表示必须不包含的组件掩码引用。
        /// </summary>
        private ComponentMask none;

        /// <summary>
        /// 使用指定的查询管理器创建一个新的实体查询构建器实例。
        /// </summary>
        /// <param name="queryManager">用于管理与缓存实体查询的管理器实例。</param>
        internal EntityQueryBuilder(EntityQueryManager queryManager)
        {
            _queryManager = queryManager;
            query = new();
            all = new();
            any = new();
            none = new();
        }

        #region With

        /// <summary>
        /// 将泛型组件类型加入查询掩码（等同于 "必须包含"）。
        /// </summary>
        /// <typeparam name="T">要包含的组件类型（值类型且实现 <see cref="IComponent"/>）。</typeparam>
        /// <returns>返回当前构建器以支持链式调用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With<T>() where T : struct, IComponent
        {
            query.SetComponent<T>();
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将两个泛型组件类型加入查询掩码。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            query.SetComponent<T1>();
            query.SetComponent<T2>();
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将三个泛型组件类型加入查询掩码。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            query.SetComponent<T1>();
            query.SetComponent<T2>();
            query.SetComponent<T3>();
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将四个泛型组件类型加入查询掩码。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            query.SetComponent<T1>();
            query.SetComponent<T2>();
            query.SetComponent<T3>();
            query.SetComponent<T4>();
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将最多五个泛型组件类型加入查询掩码（项目上限）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            query.SetComponent<T1>();
            query.SetComponent<T2>();
            query.SetComponent<T3>();
            query.SetComponent<T4>();
            query.SetComponent<T5>();
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将指定的运行时组件类型加入查询掩码。
        /// </summary>
        /// <param name="componentType">要加入的组件类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With(ComponentType componentType)
        {
            query.SetComponent(componentType);
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 将多个运行时组件类型一次性加入查询掩码。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder With(params ComponentType[] componentTypes)
        {
            query.SetComponents(componentTypes);
            ThrowQueryOutOfRange();
            return this;
        }

        /// <summary>
        /// 检查查询掩码中的组件数量是否超过许可的最大值。
        /// </summary>
        private void ThrowQueryOutOfRange()
        {
            ThrowOutOfRange(in query);
        }

        #endregion With

        #region WithAll

        /// <summary>
        /// 指定必须全部匹配的单个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T>() where T : struct, IComponent
        {
            all.SetComponent<T>();
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            all.SetComponent<T4>();
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            all.SetComponent<T4>();
            all.SetComponent<T5>();
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll(ComponentType componentType)
        {
            all.SetComponent(componentType);
            ThrowAllOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll(params ComponentType[] componentTypes)
        {
            all.SetComponents(componentTypes);
            ThrowAllOutOfRange();
            return this;
        }

        #endregion WithAll

        #region WithAny

        /// <summary>
        /// 指定任意匹配的单个泛型组件类型（满足其一即通过）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T>() where T : struct, IComponent
        {
            any.SetComponent<T>();
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            any.SetComponent<T4>();
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            any.SetComponent<T4>();
            any.SetComponent<T5>();
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny(ComponentType componentType)
        {
            any.SetComponent(componentType);
            ThrowAnyOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny(params ComponentType[] componentTypes)
        {
            any.SetComponents(componentTypes);
            ThrowAnyOutOfRange();
            return this;
        }

        #endregion WithAny

        #region WithNone

        /// <summary>
        /// 指定必须不包含的单个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T>() where T : struct, IComponent
        {
            none.SetComponent<T>();
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            none.SetComponent<T4>();
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            none.SetComponent<T4>();
            none.SetComponent<T5>();
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone(ComponentType componentType)
        {
            none.SetComponent(componentType);
            ThrowNoneOutOfRange();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone(params ComponentType[] componentTypes)
        {
            none.SetComponents(componentTypes);
            ThrowNoneOutOfRange();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowAllOutOfRange()
        {
            ThrowOutOfRange(in all);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowAnyOutOfRange()
        {
            ThrowOutOfRange(in any);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowNoneOutOfRange()
        {
            ThrowOutOfRange(in none);
        }

        #endregion WithNone

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的 <see cref="EntityQuery"/>。
        /// </summary>
        /// <returns>从管理器缓存或新建的实体查询实例。</returns>
        public EntityQuery Build()
        {
            EntityQueryDesc desc = new(query, all, any, none);
            return _queryManager.GetOrCreateQuery(desc);
        }

        /// <summary>
        /// 抛出当指定掩码中组件数量超过允许上限时的异常。
        /// </summary>
        /// <param name="componentTypes">要检查的组件掩码（按引用传递以避免拷贝）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowOutOfRange(in ComponentMask componentTypes)
        {
            if (componentTypes.ComponentCount > MaxCount)
                throw new ArgumentOutOfRangeException($"最多只能查询或设置 {MaxCount}");
        }
    }
}