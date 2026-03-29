# ExtenderApp.ECS

面向 .NET 的 ECS（Entity-Component-System）库：以 `World` 管理实体与原型，通过 `SystemUpdateContext` 构建查询、驱动系统，并支持并行作业与命令缓冲回放。

**作者：** GGGcyp  
**许可证：** [MIT](LICENSE.txt)

---

## 特性概览

- **World**：实体创建/销毁、组件读写、共享组件、命令缓冲与系统组调度。
- **查询**：`Query<T1,...,T5>()`、`EntityQueryBuilder` / `With<...>()`、`EntityQueryRow` 行遍历（`Deconstruct` / `DeconstructRefs`）。
- **系统**：`ISystem` 生命周期（`OnCreate` / `OnStart` / `OnUpdate` / `OnStop` / `OnDestroy`），`IParallelSystem<...>` + `AddParallelSystem` 并行遍历。
- **线程**：`World` 与 `SystemUpdateContext` 相关 API 需在主线程使用（内部会做检测）。

---

## 组件类型：值类型与托管堆上的类

本框架**允许将组件声明为引用类型（`class`）**，即数据可存放在**托管堆**上；也支持常见的 **`struct` 组件**。

| 方式 | 说明 |
|------|------|
| **`struct` 组件** | 更利于连续内存与缓存友好遍历，适合高频模拟与大量实体。 |
| **`class` 组件** | 可用，但会引入**堆分配、GC 压力与间接访问**；是否可接受由**使用方根据场景自行评估**。 |

并行路径中若组件为引用类型，同样需注意**多线程下的可见性与竞态**（与任何共享可变引用类型相同），请自行设计同步或不可变策略。

---

## 安装

在解决方案中引用库项目：

```xml
<ItemGroup>
  <ProjectReference Include="src/ExtenderApp.ECS/ExtenderApp.ECS.csproj" />
</ItemGroup>
```

解决方案入口：`ExtenderApp.ECS.slnx`（含库 `src/ExtenderApp.ECS`、测试库 `test/ECSTest`、控制台入口 `test/ECSTest.App`）。

---

## 快速开始

### 1. 定义组件（`struct` 示例）

```csharp
public struct Position
{
    public float X;
    public float Y;
}
```

引用类型组件示例（性能与线程安全由使用方评估）：

```csharp
public class InventoryData
{
    public List<int> ItemIds { get; } = new();
}
```

### 2. 实现系统

系统需实现 `ISystem`，通常实现为 **`struct`**（满足 `new()` 约束，无额外堆分配）：

```csharp
using ExtenderApp.ECS;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Systems;

public struct MoveSystem : ISystem
{
    public void OnCreate(ref SystemCreateContext createContext) { }
    public void OnStart() { }

    public void OnUpdate(ref SystemUpdateContext updateContext)
    {
        var query = updateContext.Query<Position>();
        foreach (var row in query)
        {
            row.DeconstructRefs(out RefRW<Position> pos);
            pos.Value.X += 1f * updateContext.DeltaTime;
        }
    }

    public void OnStop() { }
    public void OnDestroy() { }
}
```

### 3. 创建 World、注册系统并驱动循环

```csharp
using ExtenderApp.ECS;

var world = new World();

// 向默认每帧组注册系统（还可选用 AddRenderingFrameSystem、AddFixedUpdateSystem 等）
world.AddDefaultFrameSystem<MoveSystem>();

world.InitializeSystems();
world.StartSystems();

// 创建实体（多组件重载见 World.CreateEntity<T1,...>）
world.CreateEntity(new Position { X = 0, Y = 0 });

var sw = System.Diagnostics.Stopwatch.StartNew();
double last = 0;

for (int i = 0; i < 60; i++)
{
    double t = sw.Elapsed.TotalSeconds;
    float dt = (float)(t - last);
    last = t;

    world.Update(dt);
}

world.StopSystems();
world.DestroySystems();
```

`Update(float deltaTime)` 会驱动默认每帧系统组并在适当时机回放命令缓冲；固定步长逻辑可使用 `FixedUpdate`。

---

## 查询与写入

- **快捷查询**：`updateContext.Query<T>()` 等价于 `With<T>().Build()`。
- **复杂条件**：使用 `updateContext.QueryBuilder()` 或 `With<T1,...>()` 链式配置后 `Build()`。
- **按行访问**：`foreach` 得到 `EntityQueryRow<...>`，用 `Deconstruct` 取组件值，或用 `DeconstructRefs` 取 `RefRW<T>` / `RefRO<T>` 做原地修改（需遵守当前帧内访问规则）。

---

## 并行系统（简介）

实现 `IParallelSystem<T1,...>`，在 `OnUpdate(JobEntityQuery<...> queryResult, ref SystemUpdateContext updateContext)` 中遍历 `queryResult`。在主线程系统的 `OnUpdate` 里通过 `updateContext.AddParallelSystem<TSystem, T1, ...>()` 注册作业。

结构性变更应写入 `SystemUpdateContext.CommandBuffer`，由框架在回放阶段应用。详细约定见接口 XML 注释与 `test/ECSTest` 中的示例（如 `PositionParallelAccumulator`）。

---

## 测试与基准

- 自动化测试（xUnit）：`dotnet test test/ECSTest/ECSTest.csproj`
- 交互式菜单与命令行（基准 / CustomRunner / WorldTests）：`dotnet run --project test/ECSTest.App/ECSTest.App.csproj`
- 自定义跑法实现：`test/ECSTest/CustomRuns/`（partial `CustomRunner`）

建议在 **Release** 配置下评估性能。

---

## 更多说明

- 错误与异常消息在库内倾向使用**简体中文**（便于阅读源码与日志）。
- 若需发布 NuGet，可在 `ExtenderApp.ECS.csproj` 中补充包元数据（`PackageId`、`Authors`、`Description` 等）。

如有问题或改进建议，欢迎通过 Issue / PR 交流。
