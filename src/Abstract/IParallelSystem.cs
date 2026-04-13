using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 并行系统接口：在并行作业上下文中执行，通过 <see cref="JobEntityQuery"/> 遍历实体。
    /// 线程约定：实现中不得使用 <see cref="EntityQueryManager"/> 或任意依赖主线程一致性的查询构建 API；
    /// 结构性变更须写入 <see cref="SystemUpdateContext.CommandBuffer"/>，由主线程在回放阶段统一应用。
    /// </summary>
    public interface IParallelSystem
    {
        void OnUpdate(JobEntityQuery queryResult, ref SystemUpdateContext updateContext);
    }

    /// <summary>
    /// 泛型并行系统接口（1 个组件类型）。 在并行作业中接收包含类型为 T1 的组件数据的查询结果并进行处理。
    /// </summary>
    public interface IParallelSystem<T1>
    {
        void OnUpdate(JobEntityQuery<T1> queryResult, ref SystemUpdateContext updateContext);
    }

    /// <summary>
    /// 泛型并行系统接口（2 个组件类型）。 在并行作业中接收包含类型为 T1 和 T2 的组件数据的查询结果并进行处理。
    /// </summary>
    public interface IParallelSystem<T1, T2>
    {
        void OnUpdate(JobEntityQuery<T1, T2> queryResult, ref SystemUpdateContext updateContext);
    }

    /// <summary>
    /// 泛型并行系统接口（3 个组件类型）。 在并行作业中接收包含类型为 T1、T2 和 T3 的组件数据的查询结果并进行处理。
    /// </summary>
    public interface IParallelSystem<T1, T2, T3>
    {
        void OnUpdate(JobEntityQuery<T1, T2, T3> queryResult, ref SystemUpdateContext updateContext);
    }

    /// <summary>
    /// 泛型并行系统接口（4 个组件类型）。 在并行作业中接收包含类型为 T1、T2、T3 和 T4 的组件数据的查询结果并进行处理。
    /// </summary>
    public interface IParallelSystem<T1, T2, T3, T4>
    {
        void OnUpdate(JobEntityQuery<T1, T2, T3, T4> queryResult, ref SystemUpdateContext updateContext);
    }

    /// <summary>
    /// 泛型并行系统接口（5 个组件类型）。 在并行作业中接收包含类型为 T1、T2、T3、T4 和 T5 的组件数据的查询结果并进行处理。
    /// </summary>
    public interface IParallelSystem<T1, T2, T3, T4, T5>
    {
        void OnUpdate(JobEntityQuery<T1, T2, T3, T4, T5> queryResult, ref SystemUpdateContext updateContext);
    }
}