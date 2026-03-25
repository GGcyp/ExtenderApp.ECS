using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 系统缓存抽象基类（框架内部使用）。
    /// 封装对系统生命周期方法的统一调用（OnCreate/OnUpdate/OnDestroy）与启用状态检查，
    /// 子类应实现对应的受保护方法以委托到具体系统的实现。
    /// </summary>
    internal abstract class SystemCache
    {
        internal bool Enabled { get; set; }

        internal int Index { get; set; }

        internal abstract string Name { get; }

        internal abstract Type SystemType { get; }

        public SystemCache()
        {
            Enabled = true;
        }

        /// <summary>
        /// 在系统创建阶段调用。若当前缓存被禁用则跳过调用。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        public void OnCreate(ref SystemContext context)
        {
            if (!Enabled)
                return;

            OnCreateProtected(ref context);
        }

        /// <summary>
        /// 在每帧或调度点触发系统更新时调用。若当前缓存被禁用则跳过调用。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        public void OnUpdate(ref SystemContext context)
        {
            if (!Enabled)
                return;

            OnUpdateProtected(ref context);
        }

        /// <summary>
        /// 在系统销毁阶段调用。若当前缓存被禁用则跳过调用。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        public void OnDestroy(ref SystemContext context)
        {
            if (!Enabled)
                return;

            OnDestroyProtected(ref context);
        }

        /// <summary>
        /// 子类实现具体的 OnCreate 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnCreateProtected(ref SystemContext context);

        /// <summary>
        /// 子类实现具体的 OnUpdate 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnUpdateProtected(ref SystemContext context);

        /// <summary>
        /// 子类实现具体的 OnDestroy 行为。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected abstract void OnDestroyProtected(ref SystemContext context);
    }

    /// <summary>
    /// 泛型系统缓存：为某个具体的值类型系统提供缓存与生命周期委托。
    /// 保存系统实例并在外部通过基类统一入口触发其生命周期方法。
    /// 该类型仅供框架内部使用，TSystem 必须为值类型并实现 <see cref="ISystem"/> 接口。
    /// </summary>
    internal sealed class SystemCache<TSystem> : SystemCache where TSystem : struct, ISystem
    {
        private TSystem _system;

        internal override Type SystemType => typeof(TSystem);

        internal override string Name { get; }

        /// <summary>
        /// 使用系统实例创建缓存包装。
        /// </summary>
        /// <param name="system">要包装的系统实例。</param>
        public SystemCache(TSystem system) : this(system, typeof(TSystem).Name)
        {
        }

        public SystemCache(TSystem system, string name) : base()
        {
            _system = system;
            Name = name;
        }

        /// <summary>
        /// 转发到被包装系统的 OnCreate 实现。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected override void OnCreateProtected(ref SystemContext context)
        {
            _system.OnCreate(ref context);
        }

        /// <summary>
        /// 转发到被包装系统的 OnUpdate 实现。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected override void OnUpdateProtected(ref SystemContext context)
        {
            _system.OnUpdate(ref context);
        }

        /// <summary>
        /// 转发到被包装系统的 OnDestroy 实现。
        /// </summary>
        /// <param name="context">系统运行时上下文引用。</param>
        protected override void OnDestroyProtected(ref SystemContext context)
        {
            _system.OnDestroy(ref context);
        }

        /// <summary>
        /// 获取当前包装的系统实例（值类型拷贝）。
        /// </summary>
        /// <returns>系统实例副本。</returns>
        public TSystem GetSystem() => _system;

        /// <summary>
        /// 设置/替换当前包装的系统实例。
        /// </summary>
        /// <param name="system">新的系统实例。</param>
        public void SetSystem(TSystem system) => _system = system;
    }
}