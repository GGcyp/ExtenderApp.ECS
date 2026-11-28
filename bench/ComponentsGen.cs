using ExtenderApp.ECS;

namespace RelationBench;

// 50 个单字段组件（基准用，编号 C1..C50），字段类型为 float。

internal struct C1 { public float A; }
internal struct C2 { public float A; }
internal struct C3 { public float A; }
internal struct C4 { public float A; }
internal struct C5 { public float A; }
internal struct C6 { public float A; }
internal struct C7 { public float A; }
internal struct C8 { public float A; }
internal struct C9 { public float A; }
internal struct C10 { public float A; }
internal struct C11 { public float A; }
internal struct C12 { public float A; }
internal struct C13 { public float A; }
internal struct C14 { public float A; }
internal struct C15 { public float A; }
internal struct C16 { public float A; }
internal struct C17 { public float A; }
internal struct C18 { public float A; }
internal struct C19 { public float A; }
internal struct C20 { public float A; }
internal struct C21 { public float A; }
internal struct C22 { public float A; }
internal struct C23 { public float A; }
internal struct C24 { public float A; }
internal struct C25 { public float A; }
internal struct C26 { public float A; }
internal struct C27 { public float A; }
internal struct C28 { public float A; }
internal struct C29 { public float A; }
internal struct C30 { public float A; }
internal struct C31 { public float A; }
internal struct C32 { public float A; }
internal struct C33 { public float A; }
internal struct C34 { public float A; }
internal struct C35 { public float A; }
internal struct C36 { public float A; }
internal struct C37 { public float A; }
internal struct C38 { public float A; }
internal struct C39 { public float A; }
internal struct C40 { public float A; }
internal struct C41 { public float A; }
internal struct C42 { public float A; }
internal struct C43 { public float A; }
internal struct C44 { public float A; }
internal struct C45 { public float A; }
internal struct C46 { public float A; }
internal struct C47 { public float A; }
internal struct C48 { public float A; }
internal struct C49 { public float A; }
internal struct C50 { public float A; }

/// <summary>
/// 生成 50 组件掩码等静态构造。
/// </summary>
internal static class ComponentsGen
{
    /// <summary>
    /// 包含 C1..C50 全部 50 个组件的掩码。
    /// </summary>
    public static readonly ComponentMask Mask50 = CreateMask50();

    private static ComponentMask CreateMask50()
    {
        var mask = new ComponentMask();
        mask.Add(ComponentType.Create<C1>());
        mask.Add(ComponentType.Create<C2>());
        mask.Add(ComponentType.Create<C3>());
        mask.Add(ComponentType.Create<C4>());
        mask.Add(ComponentType.Create<C5>());
        mask.Add(ComponentType.Create<C6>());
        mask.Add(ComponentType.Create<C7>());
        mask.Add(ComponentType.Create<C8>());
        mask.Add(ComponentType.Create<C9>());
        mask.Add(ComponentType.Create<C10>());
        mask.Add(ComponentType.Create<C11>());
        mask.Add(ComponentType.Create<C12>());
        mask.Add(ComponentType.Create<C13>());
        mask.Add(ComponentType.Create<C14>());
        mask.Add(ComponentType.Create<C15>());
        mask.Add(ComponentType.Create<C16>());
        mask.Add(ComponentType.Create<C17>());
        mask.Add(ComponentType.Create<C18>());
        mask.Add(ComponentType.Create<C19>());
        mask.Add(ComponentType.Create<C20>());
        mask.Add(ComponentType.Create<C21>());
        mask.Add(ComponentType.Create<C22>());
        mask.Add(ComponentType.Create<C23>());
        mask.Add(ComponentType.Create<C24>());
        mask.Add(ComponentType.Create<C25>());
        mask.Add(ComponentType.Create<C26>());
        mask.Add(ComponentType.Create<C27>());
        mask.Add(ComponentType.Create<C28>());
        mask.Add(ComponentType.Create<C29>());
        mask.Add(ComponentType.Create<C30>());
        mask.Add(ComponentType.Create<C31>());
        mask.Add(ComponentType.Create<C32>());
        mask.Add(ComponentType.Create<C33>());
        mask.Add(ComponentType.Create<C34>());
        mask.Add(ComponentType.Create<C35>());
        mask.Add(ComponentType.Create<C36>());
        mask.Add(ComponentType.Create<C37>());
        mask.Add(ComponentType.Create<C38>());
        mask.Add(ComponentType.Create<C39>());
        mask.Add(ComponentType.Create<C40>());
        mask.Add(ComponentType.Create<C41>());
        mask.Add(ComponentType.Create<C42>());
        mask.Add(ComponentType.Create<C43>());
        mask.Add(ComponentType.Create<C44>());
        mask.Add(ComponentType.Create<C45>());
        mask.Add(ComponentType.Create<C46>());
        mask.Add(ComponentType.Create<C47>());
        mask.Add(ComponentType.Create<C48>());
        mask.Add(ComponentType.Create<C49>());
        mask.Add(ComponentType.Create<C50>());
        return mask;
    }
}
