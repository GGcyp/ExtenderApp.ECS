namespace ExtenderApp.ECS.Archetypes
{
    /// <summary>
    /// 组件字典，存储从组件掩码到对应 Archetype 的映射关系。
    /// </summary>
    internal class ArchetypeDictionary : Dictionary<ComponentMask, Archetype>
    {
    }
}