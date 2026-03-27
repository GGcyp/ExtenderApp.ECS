namespace ExtenderApp.ECS.Worlds
{
    /// <summary>
    /// CurrentWorld 版本管理器。
    /// 用于跟踪 CurrentWorld 级别与 Archetype 级别的数据结构变更版本，
    /// 以便查询或缓存系统在检测到版本变化时执行失效与重建。
    /// </summary>
    internal class WorldVersionManager
    {
        /// <summary>
        /// CurrentWorld 的全局版本号。
        /// 适合用于标记实体集合或组件数据发生的广义变更。
        /// 当前查询路径主要未直接使用该值，可作为后续更细粒度缓存失效的扩展点。
        /// </summary>
        public ulong WorldVersion { get; private set; }

        /// <summary>
        /// 递增 CurrentWorld 的全局版本号。
        /// </summary>
        public void IncrementWorldVersion()
        {
            WorldVersion++;
        }
    }
}