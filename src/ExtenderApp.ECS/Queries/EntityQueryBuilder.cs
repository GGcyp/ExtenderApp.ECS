using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Entities;

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
    /// 限制：内部对每个掩码的组件数量进行了上限检查（由 `MaxCount` 控制），超过将抛出 <see cref="ArgumentOutOfRangeException" />。
    /// </summary>
    public struct EntityQueryBuilder
    {
        private readonly EntityQueryManager _queryManager;

        /// <summary>
        /// 每个查询掩码允许的最大组件数。
        /// </summary>
        private const int MaxCount = 5;

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
        /// 获取标识关系类型的掩码引用。
        /// </summary>
        private RelationMask relationTypes;

        /// <summary>
        /// 使用指定的查询管理器创建一个新的实体查询构建器实例。
        /// </summary>
        /// <param name="queryManager">用于管理与缓存实体查询的管理器实例。</param>
        internal EntityQueryBuilder(EntityQueryManager queryManager)
        {
            _queryManager = queryManager;
            all = new();
            any = new();
            none = new();
            relationTypes = new();
        }

        #region WithAll

        /// <summary>
        /// 指定必须全部匹配的单个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T>() where T : struct
        {
            all.SetComponent<T>();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            all.SetComponent<T4>();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            all.SetComponent<T1>();
            all.SetComponent<T2>();
            all.SetComponent<T3>();
            all.SetComponent<T4>();
            all.SetComponent<T5>();
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll(ComponentType componentType)
        {
            all.SetComponent(componentType);
            return this;
        }

        /// <summary>
        /// 指定必须全部匹配的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAll(params ComponentType[] componentTypes)
        {
            all.SetComponents(componentTypes);
            return this;
        }

        #endregion WithAll

        #region WithAny

        /// <summary>
        /// 指定任意匹配的单个泛型组件类型（满足其一即通过）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T>() where T : struct
        {
            any.SetComponent<T>();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            any.SetComponent<T4>();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            any.SetComponent<T1>();
            any.SetComponent<T2>();
            any.SetComponent<T3>();
            any.SetComponent<T4>();
            any.SetComponent<T5>();
            return this;
        }

        /// <summary>
        /// 指定任意匹配的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny(ComponentType componentType)
        {
            any.SetComponent(componentType);
            return this;
        }

        /// <summary>
        /// 指定任意匹配的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithAny(params ComponentType[] componentTypes)
        {
            any.SetComponents(componentTypes);
            return this;
        }

        #endregion WithAny

        #region WithNone

        /// <summary>
        /// 指定必须不包含的单个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T>() where T : struct
        {
            none.SetComponent<T>();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的两个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的三个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的四个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            none.SetComponent<T4>();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的最多五个泛型组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            none.SetComponent<T1>();
            none.SetComponent<T2>();
            none.SetComponent<T3>();
            none.SetComponent<T4>();
            none.SetComponent<T5>();
            return this;
        }

        /// <summary>
        /// 指定必须不包含的运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone(ComponentType componentType)
        {
            none.SetComponent(componentType);
            return this;
        }

        /// <summary>
        /// 指定必须不包含的多个运行时组件类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithNone(params ComponentType[] componentTypes)
        {
            none.SetComponents(componentTypes);
            return this;
        }

        #endregion WithNone

        #region WithRelation

        /// <summary>
        /// 指定一个关系类型加入查询掩码，用于查询具有特定关系的实体。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <returns>当前实体查询构建器实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithRelation<T>() where T : struct => WithRelation(RelationType.Create<T>());

        /// <summary>
        /// 指定一个关系类型加入查询掩码，用于查询具有特定关系的实体。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>当前实体查询构建器实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder WithRelation(RelationType relationType)
        {
            relationTypes.Add(relationType);
            return this;
        }

        #endregion WithRelation

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的 <see cref="EntityQuery" />。
        /// </summary>
        /// <returns>从管理器缓存或新建的实体查询实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T> Build<T>() where T : struct
        {
            ComponentMask buildQuery = new();
            buildQuery.SetComponent<T>();

            EntityQueryDesc desc = new(buildQuery, all, any, none, relationTypes);
            return _queryManager.GetOrCreateQuery<T>(desc);
        }

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的双组件实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2> Build<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            ComponentMask buildQuery = new();
            buildQuery.SetComponent<T1>();
            buildQuery.SetComponent<T2>();

            EntityQueryDesc desc = new(buildQuery, all, any, none, relationTypes);
            return _queryManager.GetOrCreateQuery<T1, T2>(desc);
        }

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的三组件实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3> Build<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            ComponentMask buildQuery = new();
            buildQuery.SetComponent<T1>();
            buildQuery.SetComponent<T2>();
            buildQuery.SetComponent<T3>();

            EntityQueryDesc desc = new(buildQuery, all, any, none, relationTypes);
            return _queryManager.GetOrCreateQuery<T1, T2, T3>(desc);
        }

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的四组件实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4> Build<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            ComponentMask buildQuery = new();
            buildQuery.SetComponent<T1>();
            buildQuery.SetComponent<T2>();
            buildQuery.SetComponent<T3>();
            buildQuery.SetComponent<T4>();

            EntityQueryDesc desc = new(buildQuery, all, any, none, relationTypes);
            return _queryManager.GetOrCreateQuery<T1, T2, T3, T4>(desc);
        }

        /// <summary>
        /// 根据当前已配置的掩码构建或获取对应的五组件实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4, T5> Build<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            ComponentMask buildQuery = new();
            buildQuery.SetComponent<T1>();
            buildQuery.SetComponent<T2>();
            buildQuery.SetComponent<T3>();
            buildQuery.SetComponent<T4>();
            buildQuery.SetComponent<T5>();

            EntityQueryDesc desc = new(buildQuery, all, any, none, relationTypes);
            return _queryManager.GetOrCreateQuery<T1, T2, T3, T4, T5>(desc);
        }
    }
}