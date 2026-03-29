using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Systems.Parallels
{
    /// <summary>
    /// 并行系统工作项（无泛型组件类型）。 用于在并行作业中持有系统实例和查询结果，并在线程池中执行系统更新。
    /// </summary>
    /// <typeparam name="TSystem">实现 <see cref="IParallelSystem" /> 的系统类型。</typeparam>
    internal sealed class ParallelSystemWorkItem<TSystem> : ParallelJobWorkItem where TSystem : struct, IParallelSystem
    {
        /// <summary>
        /// 提供者引用，用于将工作项返回到池中。
        /// </summary>
        private readonly ParallelSystemProvider<TSystem> _provider;

        /// <summary>
        /// 本次作业要执行的系统实例（由调度方传入，可为 default）。
        /// </summary>
        private TSystem system;

        /// <summary>
        /// 当前工作项持有的实体查询结果。
        /// </summary>
        private JobEntityQuery query;

        /// <summary>
        /// 当前工作项持有的系统更新上下文，包含 deltaTime 和其他可能的系统更新相关信息。
        /// </summary>
        private SystemUpdateContext updateContext;

        /// <summary>
        /// 构造函数，创建一个新的工作项并保存提供者引用。
        /// </summary>
        /// <param name="provider">工作项提供者。</param>
        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 初始化工作项并激活它以便执行。
        /// </summary>
        /// <param name="query">要处理的实体查询结果。</param>
        internal void Init(JobEntityQuery query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        /// <summary>
        /// 在工作线程上执行系统的更新逻辑。
        /// </summary>
        /// <param name="updateContext">系统更新上下文。</param>
        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        /// <summary>
        /// 将工作项标记为未激活并返回到提供者池中复用。
        /// </summary>
        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }

    /// <summary>
    /// 并行系统工作项（1 个泛型组件类型）。 用于在并行作业中持有泛型组件查询结果并在线程池中执行系统更新。
    /// </summary>
    /// <typeparam name="TSystem">实现 <see cref="IParallelSystem{T1}" /> 的系统类型。</typeparam>
    /// <typeparam name="T1">查询中包含的组件数据类型。</typeparam>
    internal sealed class ParallelSystemWorkItem<TSystem, T1> : ParallelJobWorkItem where TSystem : struct, IParallelSystem<T1>
    {
        /// <summary>
        /// 提供者引用，用于将工作项返回到池中。
        /// </summary>
        private readonly ParallelSystemProvider<TSystem, T1> _provider;

        /// <summary>
        /// 本次作业要执行的系统实例（由调度方传入，可为 default）。
        /// </summary>
        private TSystem system;

        /// <summary>
        /// 当前工作项持有的泛型实体查询结果。
        /// </summary>
        private JobEntityQuery<T1> query;

        /// <summary>
        /// 当前工作项持有的系统更新上下文，包含 deltaTime 和其他可能的系统更新相关信息。
        /// </summary>
        private SystemUpdateContext updateContext;

        /// <summary>
        /// 构造函数，创建一个新的工作项并保存提供者引用。
        /// </summary>
        /// <param name="provider">工作项提供者。</param>
        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem, T1> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 初始化工作项并激活它以便执行。
        /// </summary>
        /// <param name="query">要处理的泛型实体查询结果。</param>
        internal void Init(JobEntityQuery<T1> query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        /// <summary>
        /// 在工作线程上执行系统的更新逻辑。
        /// </summary>
        /// <param name="updateContext">系统更新上下文。</param>
        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        /// <summary>
        /// 将工作项标记为未激活并返回到提供者池中复用。
        /// </summary>
        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }

    /// <summary>
    /// 并行系统工作项（2 个组件类型泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemWorkItem<TSystem, T1, T2> : ParallelJobWorkItem where TSystem : struct, IParallelSystem<T1, T2>
    {
        private readonly ParallelSystemProvider<TSystem, T1, T2> _provider;

        private TSystem system;

        private JobEntityQuery<T1, T2> query;

        private SystemUpdateContext updateContext;

        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem, T1, T2> provider)
        {
            _provider = provider;
        }

        internal void Init(JobEntityQuery<T1, T2> query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }

    /// <summary>
    /// 并行系统工作项（3 个组件类型泛型参数）。
    /// </summary>
    /// <typeparam name="TSystem"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    internal sealed class ParallelSystemWorkItem<TSystem, T1, T2, T3> : ParallelJobWorkItem where TSystem : struct, IParallelSystem<T1, T2, T3>
    {
        /// <summary>
        /// 提供者引用
        /// </summary>
        private readonly ParallelSystemProvider<TSystem, T1, T2, T3> _provider;

        private TSystem system;

        /// <summary>
        /// 查询结果
        /// </summary>
        private JobEntityQuery<T1, T2, T3> query;

        /// <summary>
        /// 当前工作项持有的系统更新上下文，包含 deltaTime 和其他可能的系统更新相关信息。
        /// </summary>
        private SystemUpdateContext updateContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem, T1, T2, T3> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 初始化工作项并激活
        /// </summary>
        internal void Init(JobEntityQuery<T1, T2, T3> query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        /// <summary>
        /// 执行调用系统的更新方法
        /// </summary>
        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        /// <summary>
        /// 返回工作项到提供者
        /// </summary>
        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }

    /// <summary>
    /// 并行系统工作项（4 个泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemWorkItem<TSystem, T1, T2, T3, T4> : ParallelJobWorkItem where TSystem : struct, IParallelSystem<T1, T2, T3, T4>
    {
        /// <summary>
        /// 提供者引用
        /// </summary>
        private readonly ParallelSystemProvider<TSystem, T1, T2, T3, T4> _provider;

        private TSystem system;

        /// <summary>
        /// 查询结果
        /// </summary>
        private JobEntityQuery<T1, T2, T3, T4> query;

        /// <summary>
        /// 当前工作项持有的系统更新上下文，包含 deltaTime 和其他可能的系统更新相关信息。
        /// </summary>
        private SystemUpdateContext updateContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem, T1, T2, T3, T4> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 初始化工作项并激活
        /// </summary>
        internal void Init(JobEntityQuery<T1, T2, T3, T4> query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        /// <summary>
        /// 执行调用系统的更新方法
        /// </summary>
        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        /// <summary>
        /// 返回工作项到提供者
        /// </summary>
        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }

    /// <summary>
    /// 并行系统工作项（5 个泛型参数）。
    /// </summary>
    internal sealed class ParallelSystemWorkItem<TSystem, T1, T2, T3, T4, T5> : ParallelJobWorkItem where TSystem : struct, IParallelSystem<T1, T2, T3, T4, T5>
    {
        /// <summary>
        /// 提供者引用
        /// </summary>
        private readonly ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5> _provider;

        private TSystem system;

        /// <summary>
        /// 查询结果
        /// </summary>
        private JobEntityQuery<T1, T2, T3, T4, T5> query;

        /// <summary>
        /// 当前工作项持有的系统更新上下文，包含 deltaTime 和其他可能的系统更新相关信息。
        /// </summary>
        private SystemUpdateContext updateContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelSystemWorkItem(ParallelSystemProvider<TSystem, T1, T2, T3, T4, T5> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// 初始化工作项并激活
        /// </summary>
        internal void Init(JobEntityQuery<T1, T2, T3, T4, T5> query, SystemUpdateContext updateContext, TSystem system)
        {
            this.query = query;
            this.updateContext = updateContext;
            this.system = system;
            IsActived = true;
        }

        /// <summary>
        /// 执行调用系统的更新方法
        /// </summary>
        public override void Execute()
        {
            system.OnUpdate(query, ref updateContext);
        }

        /// <summary>
        /// 返回工作项到提供者
        /// </summary>
        public override void Retrun()
        {
            IsActived = false;
            _provider.Return(this);
        }
    }
}