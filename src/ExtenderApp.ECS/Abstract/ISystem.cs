using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Systems;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统基础接口（标记接口）。
    /// 具体系统请实现 <see cref="ISyncSystem{T}"/> 或 <see cref="IParallelSystem{T}"/> 接口。
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 当系统被注册或首次启用时调用（主线程）。用于初始化长期资源。
        /// </summary>
        /// <param name="context">系统上下文（按引用传入）。</param>
        void OnCreate(ref SystemContext context) { }

        /// <summary>
        /// 系统被销毁或注销时调用（主线程）。用于释放资源。
        /// </summary>
        /// <param name="context">系统上下文（按引用传入）。</param>
        void OnDestroy(ref SystemContext context) { }

        /// <summary>
        /// 每帧调用的更新入口（同步或并行路径中会被适当调度）。
        /// </summary>
        /// <param name="context">系统上下文（按引用传入）。</param>
        void OnUpdate(ref SystemContext context);
    }
}