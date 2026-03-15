namespace ExtenderApp.ECS
{
    /// <summary>
    /// 实体管理器接口，负责实体的创建、销毁与状态查询。
    /// </summary>
    public interface IEntityManager
    {
        /// <summary>
        /// 创建一个新实体。
        /// </summary>
        /// <returns>新创建的实体。</returns>
        Entity CreateEntity();

        /// <summary>
        /// 销毁指定实体。
        /// </summary>
        /// <param name="entity">要销毁的实体。</param>
        /// <returns>
        /// 销毁成功返回 true；若实体不存在或已销毁则返回 false。
        /// </returns>
        bool DestroyEntity(Entity entity);

        /// <summary>
        /// 检查指定实体是否处于存活状态。
        /// </summary>
        /// <param name="entity">要检查的实体。</param>
        /// <returns>存活返回 true；否则返回 false。</returns>
        bool IsAlive(Entity entity);
    }
}