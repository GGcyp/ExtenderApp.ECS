using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 基础系统组基类。用于组合多个系统并统一管理其生命周期（Create/Start/Update/Stop/Destroy）。
    ///
    /// 说明：
    /// - 作为框架内部或上层用户继承的基类，提供默认的空实现，子类可根据需要重写相应生命周期方法来实现分组行为。
    /// - 系统组通常用于组织逻辑相关的系统（例如：渲染组、物理组、AI 组等），便于在调度器中按组启停与顺序管理。
    /// - 该类并不约束具体的调度策略（链式或数组），仅提供生命周期接口的统一入口。
    ///
    /// 使用建议：
    /// - 若需在组内维护子系统集合，可在派生类中以数组/列表方式存储并在重写的 OnUpdate 中遍历执行；
    /// - 对于需要动态频繁变更子系统集合的场景，可在派生类内使用链表或维护一个可重建的数组以兼顾插入效率与热路径遍历性能；
    /// - 子类在重写方法时应尽量避免在热路径（每帧）中做大量分配或昂贵操作，以保持调度性能稳定。
    /// </summary>
    internal class BaseSystemGroup : ISystemGroup
    {
        /// <summary>
        /// 当系统组被创建时调用。默认实现为空，派生类可覆盖用于初始化资源或子系统注册。
        /// </summary>
        /// <param name="createContext">创建上下文，包含框架传入的初始化信息。</param>
        public virtual void OnCreate(ref SystemGroupCreateContext createContext)
        {
        }

        /// <summary>
        /// 当系统组被销毁时调用。默认实现为空，派生类可覆盖用于释放资源。
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 当系统组被启动（进入运行状态）时调用。默认实现为空，派生类可覆盖用于按需开始子系统或重置状态。
        /// </summary>
        public virtual void OnStart()
        {
        }

        /// <summary>
        /// 当系统组被停止（退出运行状态）时调用。默认实现为空，派生类可覆盖用于暂停子系统或保存状态。
        /// </summary>
        public virtual void OnStop()
        {
        }

        /// <summary>
        /// 每帧更新回调。默认实现为空，派生类可覆盖并在其中遍历并更新子系统。 注意：此方法为热路径，应避免在实现中进行大量托管分配与昂贵的同步操作。
        /// </summary>
        /// <param name="updateContext">更新上下文，包含本次更新的时间与调度信息。</param>
        public virtual void OnUpdate(ref SystemGroupUpdateContext updateContext)
        {
        }
    }
}