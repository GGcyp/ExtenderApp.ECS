namespace ECSTest.Components;

/// <summary>
/// 测试中使用的二维位置组件（非托管结构体）。
/// </summary>
public struct TestPosition
{
    public float X;
    public float Y;
}

/// <summary>
/// 测试中使用的速度组件（非托管结构体）。
/// </summary>
public struct TestVelocity
{
    public float Dx;
    public float Dy;
}

/// <summary>
/// 测试中使用的生命值组件（非托管结构体）。
/// </summary>
public struct TestHealth
{
    public int Value;
}

/// <summary>
/// 测试中使用的托管上下文组件（用于校验迁移后引用与字段仍正确）。
/// </summary>
public class TestEcsContext
{
    /// <summary>
    /// 显示用名称。
    /// </summary>
    public string Name = "";

    /// <summary>
    /// 逻辑标识。
    /// </summary>
    public int Id;
}

/// <summary>
/// 测试中使用的代理/智能体标识组件。
/// </summary>
public struct TestAgentContext
{
    /// <summary>
    /// 代理编号。
    /// </summary>
    public int AgentId;
}

/// <summary>
/// 测试中使用的标签组件（整型载荷）。
/// </summary>
public struct TestTag
{
    /// <summary>
    /// 标签值。
    /// </summary>
    public int Value;
}

/// <summary>
/// 测试中使用的缓冲元素组件（整型载荷）。
/// </summary>
public struct TestBufferElement
{
    /// <summary>
    /// 元素值。
    /// </summary>
    public int Value;
}

/// <summary>
/// 测试中使用的共享组件（结构体）。
/// </summary>
public struct TestSharedConfig
{
    public int Seed;
}