using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// World 的查询相关 API 的局部实现。 提供实体查询构建器以及常用泛型查询的快捷创建方法。
    /// </summary>
    public partial class World
    {
        /// <summary>
        /// 创建一个实体查询构建器，用于按条件链式构造 EntityQuery。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQueryBuilder CreateEntityQueryBuilder()
        {
            ThrowIfNotMainThread();
            return new EntityQueryBuilder(EntityQueryManager);
        }

        /// <summary>
        /// 创建或获取仅包含单个组件类型的实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T> CreateEntityQuery<T>() where T : struct
            => CreateEntityQueryBuilder().Build<T>();

        /// <summary>
        /// 创建或获取包含两个组件类型的实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2> CreateEntityQuery<T1, T2>()
            where T1 : struct
            where T2 : struct
            => CreateEntityQueryBuilder().Build<T1, T2>();

        /// <summary>
        /// 创建或获取包含三个组件类型的实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3> CreateEntityQuery<T1, T2, T3>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            => CreateEntityQueryBuilder().Build<T1, T2, T3>();

        /// <summary>
        /// 创建或获取包含四个组件类型的实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4> CreateEntityQuery<T1, T2, T3, T4>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            => CreateEntityQueryBuilder().Build<T1, T2, T3, T4>();

        /// <summary>
        /// 创建或获取包含五个组件类型的实体查询。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery<T1, T2, T3, T4, T5> CreateEntityQuery<T1, T2, T3, T4, T5>()
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            => CreateEntityQueryBuilder().Build<T1, T2, T3, T4, T5>();
    }
}