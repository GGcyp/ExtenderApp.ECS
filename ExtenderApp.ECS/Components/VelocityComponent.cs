using ExtenderApp.ECS.Interfaces;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 存储实体在二维空间速度的组件。
    /// </summary>
    public class VelocityComponent : IComponent
    {
        /// <summary>
        /// 获取或设置 X 轴方向速度。
        /// </summary>
        public float VX { get; set; }

        /// <summary>
        /// 获取或设置 Y 轴方向速度。
        /// </summary>
        public float VY { get; set; }

        /// <summary>
        /// 初始化 <see cref="VelocityComponent"/> 的新实例。
        /// </summary>
        public VelocityComponent()
        {
        }

        /// <summary>
        /// 使用初始速度初始化 <see cref="VelocityComponent"/> 的新实例。
        /// </summary>
        /// <param name="vx">初始 X 方向速度。</param>
        /// <param name="vy">初始 Y 方向速度。</param>
        public VelocityComponent(float vx, float vy)
        {
            VX = vx;
            VY = vy;
        }
    }
}
