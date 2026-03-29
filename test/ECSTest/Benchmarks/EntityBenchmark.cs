using System.Diagnostics;
using ECSTest.Components;
using ExtenderApp.ECS;

// 备注：将实体基准测试提取到独立文件，包含测试用组件定义。

public static class EntityBenchmark
{
    public static void Run()
    {
        Console.WriteLine("开始单线程实体创建/销毁基准测试（仅主线程操作）...");

        int totalEntities = 10000;

        Console.WriteLine($"请输入总目标创建数量（默认 {totalEntities}，建议根据实际情况调整）：");
        string? input = Console.ReadLine();
        totalEntities = int.Parse(string.IsNullOrWhiteSpace(input) ? $"{totalEntities}" : input); // 总目标创建数量（可调整）

        Console.WriteLine($"目标总数: {totalEntities}");

        World world = new();

        // 构建组件掩码（Position + Velocity）
        var mask = new ComponentMask(ComponentType.Create<Position>());
        mask.Add(ComponentType.Create<Velocity>());

        var createdList = new List<Entity>(totalEntities);
        long createdCount = 0;
        long destroyedCount = 0;

        var rnd = new Random(Environment.TickCount);

        // 创建阶段（主线程执行所有创建/销毁操作）
        var swCreate = Stopwatch.StartNew();
        for (int i = 0; i < totalEntities; i++)
        {
            var e = world.CreateEntity(mask);
            createdList.Add(e);
            createdCount++;

            // 随机回收一部分实体以模拟增删（例如 10% 概率）
            if (rnd.NextDouble() < 0.10 && createdList.Count > 0)
            {
                var last = createdList[^1];
                createdList.RemoveAt(createdList.Count - 1);
                try
                {
                    world.DestroyEntity(last);
                    destroyedCount++;
                }
                catch
                {
                    // 忽略测试过程中的异常
                }
            }
        }
        swCreate.Stop();

        Console.WriteLine($"创建阶段完成。已创建: {createdCount}, 创建期间销毁: {destroyedCount}, 耗时: {swCreate.ElapsedMilliseconds} 毫秒");

        // 清理阶段：销毁所有剩余实体并计时（主线程执行）
        var swDestroy = Stopwatch.StartNew();
        int removed = 0;
        for (int i = createdList.Count - 1; i >= 0; i--)
        {
            var e = createdList[i];
            try
            {
                world.DestroyEntity(e);
                removed++;
            }
            catch
            {
                // ignore
            }
        }
        swDestroy.Stop();
        destroyedCount += removed;

        Console.WriteLine($"清理阶段完成。额外销毁: {removed}, 总销毁: {destroyedCount}, 耗时: {swDestroy.ElapsedMilliseconds} 毫秒");

        Console.WriteLine($"摘要: 尝试创建总数 = {createdCount}, 总销毁 = {destroyedCount}, 存活净数 = {createdCount - destroyedCount}");
        Console.WriteLine("按回车返回菜单。");
        Console.ReadLine();
    }
}