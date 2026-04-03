namespace ExtenderApp.ECS;

/// <summary>
/// 三个内置系统组名称常量：每帧逻辑、每帧呈现、固定步长逻辑。
/// </summary>
public static class WorldSystemGroupNames
{
    /// <summary>
    /// 每帧逻辑组名；由 <see cref="World.AddDefaultFrameSystem{TSystem}"/> 与 <see cref="World.UpdateSimulation"/> 使用。
    /// </summary>
    public const string DefaultGroup = "DefaultGroup";

    /// <summary>
    /// 每帧呈现组名（与历史内部名 <c>RenderingSystem</c> 一致）；由 <see cref="World.AddRenderingFrameSystem{TSystem}"/> 与 <see cref="World.UpdatePresentation"/> 使用。
    /// </summary>
    public const string PresentationGroup = "RenderingSystem";

    /// <summary>
    /// 固定步长逻辑组名；由 <see cref="World.AddFixedUpdateSystem{TSystem}"/> 与 <see cref="World.FixedUpdate"/> 使用。
    /// </summary>
    public const string FixedGroup = "FixedGroup";
}
