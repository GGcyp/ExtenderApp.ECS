namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 延迟实体命令缓冲（EntityCommandBuffer）所使用的命令类型枚举。
    /// </summary>
    internal enum EntityCommandType : byte
    {
        ///// <summary>
        ///// 创建实体命令：TargetId 字段为实体占位符 ID（由上层定义编码策略），ComponentTypeIndex 和数据字段未使用。
        ///// </summary>
        //CreateEntity,

        /// <summary>
        /// 销毁实体命令：TargetId 字段为实体占位符或真实实体 ID（由上层定义编码策略），ComponentTypeIndex 和数据字段未使用。
        /// </summary>
        DestroyEntity,

        /// <summary>
        /// 删除匹配查询的实体命令：数据字段存储序列化的 <see cref="ExtenderApp.ECS.Queries.EntityQueryDesc"/> 描述。
        /// 回放时将根据查询描述遍历并销毁匹配的实体。
        /// </summary>
        DestroyEntitiesForQuery,

        /// <summary>
        /// 添加组件命令：TargetId 字段为实体占位符或真实实体 ID（由上层定义编码策略），ComponentTypeIndex 字段为组件类型的内部索引，数据字段指向组件数据。
        /// </summary>
        AddComponent,

        /// <summary>
        /// 移除组件命令：TargetId 字段为实体占位符或真实实体 ID（由上层定义编码策略），ComponentTypeIndex 字段为组件类型的内部索引，数据字段未使用。
        /// </summary>
        RemoveComponent,

        /// <summary>
        /// 设置组件命令：TargetId 字段为实体占位符或真实实体 ID（由上层定义编码策略），ComponentTypeIndex 字段为组件类型的内部索引，数据字段指向组件数据。
        /// </summary>
        SetComponent,

        /// <summary>
        /// 切换组件命令：TargetId 字段为实体占位符或真实实体 ID（由上层定义编码策略），ComponentTypeIndex 字段为组件类型的内部索引，数据字段指向组件数据。回放时会根据实体当前是否具有该组件来决定是添加还是设置。
        /// </summary>
        ModifyComponent,

        // 以后可扩展更多类型：CopyComponent、Instantiate、AddSharedComponent 等
    }
}