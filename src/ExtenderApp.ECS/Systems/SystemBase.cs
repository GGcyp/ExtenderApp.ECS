using ExtenderApp.ECS.Components;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// ECS 系统基类。系统会在每帧处理中实体。
    /// 系统默认在主线程运行，如需并行处理请在实现中自行控制并确保线程安全。
    /// </summary>
    public abstract class SystemBase : ISystem
    {
        /// <summary>
        /// 每帧调用一次以更新系统。
        /// 接收 World 以便系统可访问 ArchetypeManager/EntityManager 并创建 Query。
        /// </summary>
        /// <param name="world">当前世界实例（主线程上下文）。</param>
        public abstract void Update(World world);
    }
}