using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtenderApp.ECS.Worlds
{
    /// <summary>
    /// World 版本管理器。
    /// 用于跟踪 World 级别与 Archetype 级别的数据结构变更版本，
    /// 以便查询或缓存系统在检测到版本变化时执行失效与重建。
    /// </summary>
    internal class WorldVersionManager
    {
        /// <summary>
        /// World 的全局版本号。
        /// 适合用于标记实体集合或组件数据发生的广义变更。
        /// 当前查询路径主要未直接使用该值，可作为后续更细粒度缓存失效的扩展点。
        /// </summary>
        public ulong WorldVersion { get; private set; }

        /// <summary>
        /// Archetype 结构版本号。
        /// 当 Archetype 集合或实体 Archetype 归属发生变化时递增。
        /// 当前 EntityQuery 主要依赖该值判断匹配缓存是否需要重建。
        /// </summary>
        public ulong ArchetypeVersion { get; private set; }

        /// <summary>
        /// 递增 World 的全局版本号。
        /// </summary>
        public void IncrementWorldVersion()
        {
            WorldVersion++;
        }

        /// <summary>
        /// 递增 Archetype 结构版本号。
        /// </summary>
        public void IncrementArchetypeVersion()
        {
            ArchetypeVersion++;
        }
    }
}