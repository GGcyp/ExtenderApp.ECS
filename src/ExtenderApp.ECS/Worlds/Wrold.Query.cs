using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的查询相关 API 的局部实现。 提供实体查询构建器以及常用泛型查询的快捷创建方法。
    /// </summary>
    public partial class World
    {
        #region With

        /// <summary>
        /// 通过泛型参数直接创建指定组件组合的实体查询（1 个组件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder<T> With<T>() where T : struct
        {
            ThrowIfNotMainThread();
            return new(QueryManager);
        }

        /// <summary>
        /// 通过泛型参数直接创建指定组件组合的实体查询（2 个组件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder<T1, T2> With<T1, T2>()
            where T1 : struct
            where T2 : struct
        {
            ThrowIfNotMainThread();
            return new(QueryManager);
        }

        /// <summary>
        /// 通过泛型参数直接创建指定组件组合的实体查询（3 个组件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder<T1, T2, T3> With<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            ThrowIfNotMainThread();
            return new(QueryManager);
        }

        /// <summary>
        /// 通过泛型参数直接创建指定组件组合的实体查询（4 个组件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder<T1, T2, T3, T4> With<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            ThrowIfNotMainThread();
            return new(QueryManager);
        }

        /// <summary>
        /// 通过泛型参数直接创建指定组件组合的实体查询（5 个组件）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder<T1, T2, T3, T4, T5> With<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            ThrowIfNotMainThread();
            return new(QueryManager);
        }

        #endregion With

        #region Query

        /// <summary>
        /// 创建或获取仅包含单个组件类型的实体查询。
        /// 现由 With<T1>() 实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T> Query<T>() where T : struct
            => With<T>().Build();

        /// <summary>
        /// 创建或获取包含两个组件类型的实体查询。
        /// 现由 With<T1,T2>() 实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2> Query<T1, T2>()
            where T1 : struct
            where T2 : struct
            => With<T1, T2>().Build();

        /// <summary>
        /// 创建或获取包含三个组件类型的实体查询。
        /// 现由 With<T1,T2,T3>() 实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            => With<T1, T2, T3>().Build();

        /// <summary>
        /// 创建或获取包含四个组件类型的实体查询。
        /// 现由 With<T1,T2,T3,T4>() 实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            => With<T1, T2, T3, T4>().Build();

        /// <summary>
        /// 创建或获取包含五个组件类型的实体查询。
        /// 现由 With<T1,T2,T3,T4,T5>() 实现。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            => With<T1, T2, T3, T4, T5>().Build();

        #endregion Query

        /// <summary>
        /// 删除指定查询匹配的所有实体。 该方法会遍历查询匹配的所有实体并调用 Entities.DestroyEntity() 来销毁它们。
        /// </summary>
        /// <param name="query">查询到的实体集合。</param>
        public void DestroyEntitiesForQuery(EntityQuery query)
        {
            ThrowIfNotMainThread();

            var current = query.Core.GetArchetypeSegmentHead();
            while (current != null)
            {
                var archetype = current.Archetype;
                var entities = archetype.Entities;
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    var segmentInfo = entities[i];
                    var infoEntities = segmentInfo.Entities;
                    for (int j = infoEntities.Length - 1; j >= 0; j--)
                    {
                        var entity = infoEntities[j];
                        Entities.DestroyEntity(entity);
                    }
                }
                current = current.Next;
            }
        }

        /// <summary>
        /// 创建一个实体查询构建器，用于按条件链式构造 EntityQuery。
        /// 兼容旧代码，返回底层构建器。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder QueryBuilder()
        {
            ThrowIfNotMainThread();
            return new EntityQueryBuilder(QueryManager);
        }
    }
}