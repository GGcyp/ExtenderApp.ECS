using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 简单的系统调度器：管理注册系统并在主线程上顺序执行。
    /// 支持在系统内部基于 Query 决定是否对部分数据并行化处理。
    /// </summary>
    public sealed class SystemScheduler : IDisposable
    {
        private readonly List<SystemBase> _systems = new();

        public void Register(SystemBase system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            _systems.Add(system);
        }

        public void Unregister(SystemBase system) => _systems.Remove(system);

        /// <summary>
        /// 在主线程上同步更新所有系统。系统内部可以自行决定将部分工作并行化。
        /// </summary>
        public void UpdateAll(World world)
        {
            foreach (var s in _systems)
            {
                s.Update(world);
            }
        }

        public void Dispose()
        {
            _systems.Clear();
        }
    }
}
