using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统缓存抽象基类（框架内部使用）。
    ///
    /// 说明：该类型用于在调度器中保存具体系统实例并统一触发生命周期回调（OnCreate/OnStart/OnUpdate/OnStop/OnDestroy）。
    /// 当前实现采用链式结构（通过 <see cref="Next"/> 字段串联），以便于在运行时以链表方式维护系统顺序与动态插入/删除。
    ///
    /// 设计权衡（链式 vs 数组/连续容器）：
    /// - 链式（当前实现）优点：插入/移除操作简单、低开销，适合运行时经常变更系统集合的场景；
    ///   缺点：遍历时内存局部性较差，可能带来缓存不命中，影响热路径性能。
    /// - 数组/列表优点：遍历性能优（连续内存，缓存友好），适合系统集合在运行期稳定且更新为热路径时；
    ///   缺点：插入/移除需要搬移或额外标记逻辑，运行时动态变更成本较高。
    ///
    /// 建议：若系统集合在应用启动阶段构建完毕并很少修改，优先使用数组/连续容器以获得最优遍历性能；
    /// 若需要频繁在运行期增删系统（例如动态插件或编辑器模式），链式结构能够降低修改复杂度。
    /// 实务中也可采用混合策略：维护可变链表用于插入/删除操作，在每帧前将链表平铺到数组用于热路径遍历。
    /// </summary>
    internal abstract class SystemSchedulerContext : ISystem
    {
        /// <summary>
        /// 系统名称，用于调试与日志输出。
        /// </summary>
        internal string Name { get; }

        protected SystemSchedulerContext(string name)
        {
            Name = name;
        }

        public void OnCreate(ref SystemGroupCreateContext createContext) 
            => OnCreateProtected(ref createContext);

        public void OnStart() 
            => OnStartProtected();

        public void OnUpdate(ref SystemGroupUpdateContext updateContext) 
            => OnUpdateProtected(ref updateContext);

        public void OnStop() 
            => OnStopProtected();

        public void OnDestroy() 
            => OnDestroyProtected();

        /// <summary>
        /// 子类实现具体的 OnCreate 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnCreateProtected(ref SystemGroupCreateContext createContext);
        protected abstract void OnStartProtected();

        /// <summary>
        /// 子类实现具体的 OnUpdate 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnUpdateProtected(ref SystemGroupUpdateContext updateContext);
        protected abstract void OnStopProtected();

        /// <summary>
        /// 子类实现具体的 OnDestroy 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnDestroyProtected();
    }

    /// <summary>
    /// 泛型系统缓存：为某个具体的值类型系统提供缓存与生命周期委托。
    /// 保存系统实例并在外部通过基类统一入口触发其生命周期方法。
    /// 该类型仅供框架内部使用，TSystem 必须为值类型并实现 <see cref="ISystem"/> 接口。
    ///
    /// 备注：由于使用值类型系统（struct），在此处直接保存字段的副本并在调度时调用其方法。
    /// 若需要避免值拷贝或保存引用语义，应将系统实现为类并调整调度器存储策略。
    /// </summary>
    internal sealed class SystemSchedulerContext<TSystem> : SystemSchedulerContext where TSystem : struct, ISystem
    {
        private readonly TSystem _system;

        public SystemSchedulerContext(TSystem system) : this(system,typeof(TSystem).Name) { }

        public SystemSchedulerContext(TSystem system, string name) : base(name) 
            => _system = system;

        protected override void OnCreateProtected(ref SystemGroupCreateContext createContext)
        => _system.OnCreate(ref createContext);

        protected override void OnDestroyProtected()
        => _system.OnDestroy();

        protected override void OnStartProtected()
        => _system.OnStart();

        protected override void OnStopProtected() 
            => _system.OnStop();

        protected override void OnUpdateProtected(ref SystemGroupUpdateContext updateContext) 
            => _system.OnUpdate(ref updateContext);
    }
}