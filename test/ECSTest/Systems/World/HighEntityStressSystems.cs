using System.Runtime.CompilerServices;
using ECSTest.Components;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Systems;

namespace ECSTest.WorldTests;

/// <summary>
/// 高实体压力测试：主线程积分系统，按 Velocity 更新 Position。
/// </summary>
public struct IntegrateVelocitySystem : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        float dt = updateContext.DeltaTime;
        var query = updateContext.Query<Position, Velocity>();
        foreach (var row in query)
        {
            row.Deconstruct(out RefRW<Position> pos, out RefRW<Velocity> vel);
            pos.Value.X += vel.Value.Vx * dt;
            pos.Value.Y += vel.Value.Vy * dt;
        }
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 高实体压力测试：对 Position 做固定次数浮点运算（不改变数值，仅消耗 CPU）。
/// </summary>
public struct HeavyPositionTouchSystem : ISystem
{
    /// <summary>
    /// 每个实体上执行的纯计算迭代次数（与 <see cref="OnUpdate"/> 内循环一致）。
    /// </summary>
    public const int InnerIterations = 24;

    public void OnCreate(ref SystemCreateContext createContext)
    {
    }

    public void OnStart()
    {
    }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position>();
        foreach (var row in query)
        {
            row.Deconstruct(out RefRW<Position> pos);
            float x = pos.Value.X;
            for (int k = 0; k < InnerIterations; k++)
                x = MathF.Sqrt(MathF.Max(x * x, 0f));

            pos.Value.X = x;
        }
    }

    public void OnStop()
    {
    }

    public void OnDestroy()
    {
    }
}

/// <summary>
/// 高实体规模下期望的 Sum(Position.X) 全为同一初值经积分后的闭式解（避免大数累加浮点误差）。
/// </summary>
internal static class HighEntityStressMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ExpectedUniformSumX(int entityCount, float initialX, float velocityX, float deltaTime)
        => entityCount * (initialX + velocityX * deltaTime);
}
