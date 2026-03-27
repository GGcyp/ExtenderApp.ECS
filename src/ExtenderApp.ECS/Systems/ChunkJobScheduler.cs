using ExtenderApp.ECS.Abstract;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace ExtenderApp.ECS.Systems
{


    /// <summary>
    /// 多线程并行任务调度器：
    /// - 全局入口队列接收任务；
    /// - 按 Archetype 路由到 lane；
    /// - 每个 lane 由长期驻留任务顺序消费；
    /// - 不同 lane 可并行执行。
    /// </summary>
    internal sealed class ChunkJobScheduler
    {
        private readonly Channel<SystemSchedulerWorkItem> _globalQueue;

        /// <summary>
        /// 系统调度工作项描述：用于声明系统所属原型与访问信息。
        /// </summary>
        private readonly struct SystemSchedulerWorkItem
        {
            public readonly Archetype CurrentArchetype;



            /// <summary>
            /// 初始化工作项。
            /// </summary>
            public SystemSchedulerWorkItem()
            {

            }
        }

        /// <summary>
        /// 原型 lane 信息。
        /// </summary>
        private sealed class ArchetypeLane
        {
            public readonly Channel<SchedulerJob> Queue;
            public readonly Task WorkerTask;

            public ArchetypeLane(Channel<SchedulerJob> queue, Task workerTask)
            {
                Queue = queue;
                WorkerTask = workerTask;
            }
        }

        /// <summary>
        /// 内部调度任务对象（对象池复用）。
        /// </summary>
        private sealed class SchedulerJob
        {

        }

        /// <summary>
        /// 帧批次完成器：跟踪一帧内所有任务并汇总异常。
        /// </summary>
        private sealed class FrameBatch
        {
           
        }
    }
}