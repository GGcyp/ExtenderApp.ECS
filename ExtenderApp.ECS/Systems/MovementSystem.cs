using ExtenderApp.ECS.Components;
using ExtenderApp.ECS.Managers;

namespace ExtenderApp.ECS.Systems
{
    /// <summary>
    /// 简单移动系统：根据速度更新位置。
    /// </summary>
    public class MovementSystem : SystemBase
    {
        /// <summary>
        /// 更新同时拥有 PositionComponent 与 VelocityComponent 的所有实体。
        /// </summary>
        /// <param name="entityManager">实体管理器实例。</param>
        /// <param name="componentManager">组件管理器实例。</param>
        /// <param name="deltaTime">距离上次更新经过的秒数。</param>
        public override void Update(EntityManager entityManager, ComponentManager componentManager, double deltaTime)
        {
            // 遍历同时拥有位置与速度组件的实体
            foreach (var entity in componentManager.GetEntitiesWith<PositionComponent, VelocityComponent>())
            {
                var pos = componentManager.GetComponent<PositionComponent>(entity);
                var vel = componentManager.GetComponent<VelocityComponent>(entity);
                if (pos is null || vel is null)
                    continue;

                // 根据速度与时间增量更新位置
                pos.X += vel.VX * (float)deltaTime;
                pos.Y += vel.VY * (float)deltaTime;
            }
        }
    }
}
