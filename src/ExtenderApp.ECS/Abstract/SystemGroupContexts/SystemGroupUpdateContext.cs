namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 系统每帧更新时传入的上下文快照（只读结构体）。
    /// </summary>
    public readonly ref struct SystemGroupUpdateContext
    {
        /// <summary>
        /// 每帧步长（秒）。系统逻辑应使用该值进行与时间相关的更新计算。
        /// </summary>
        public readonly float DeltaTime;

        /// <summary>
        /// 全局运行时间（秒）。可用于需要绝对时间的逻辑或随机种子等。
        /// </summary>
        public readonly double Time;

        /// <summary>
        /// 当前帧索引（从 0 开始递增）。用于跨帧唯一标识或基于帧的采样/统计。
        /// </summary>
        public readonly ulong FrameIndex;

        /// <summary>
        /// 创建新的更新上下文快照实例。该构造函数用于框架在每帧开始时填充时间相关字段并传入系统。
        /// </summary>
        /// <param name="deltaTime">每帧步长（秒）。</param>
        /// <param name="time">全局运行时间（秒）。</param>
        /// <param name="frameIndex">当前帧索引。</param>
        /// <param name="cancellationToken">可选取消令牌，默认 <see cref="CancellationToken.None"/>。</param>
        public SystemGroupUpdateContext(float deltaTime, double time, ulong frameIndex)
        {
            DeltaTime = deltaTime;
            Time = time;
            FrameIndex = frameIndex;
        }
    }
}