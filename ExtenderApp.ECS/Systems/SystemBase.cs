using ExtenderApp.ECS.Managers;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// ECS 系统基类。系统会在每帧处理中实体。
    /// </summary>
    public abstract class SystemBase
    {
        /// <summary>
        /// 每帧调用一次以更新系统。
        /// </summary>
        /// <param name="entityManager">实体管理器实例。</param>
        /// <param name="componentManager">组件管理器实例。</param>
        /// <param name="deltaTime">距离上次更新经过的秒数。</param>
        public abstract void Update(EntityManager entityManager, ComponentManager componentManager, double deltaTime);
    }
}
