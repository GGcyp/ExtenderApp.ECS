using ExtenderApp.ECS.Interfaces;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 存储实体在二维空间位置的组件。
    /// </summary>
    public class PositionComponent : IComponent
    {
        /// <summary>
        /// 获取或设置 X 坐标。
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 获取或设置 Y 坐标。
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 初始化 <see cref="PositionComponent"/> 的新实例。
        /// </summary>
        public PositionComponent()
        {
        }

        /// <summary>
        /// 使用初始坐标初始化 <see cref="PositionComponent"/> 的新实例。
        /// </summary>
        /// <param name="x">初始 X 坐标。</param>
        /// <param name="y">初始 Y 坐标。</param>
        public PositionComponent(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
