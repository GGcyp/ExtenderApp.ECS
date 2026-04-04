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
/// 测试中使用的共享组件（结构体）。
/// </summary>
public struct TestSharedConfig
{
    public int Seed;
}
