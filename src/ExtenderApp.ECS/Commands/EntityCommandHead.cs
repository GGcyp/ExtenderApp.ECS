using System.Runtime.InteropServices;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 命令头（固定大小），用于记录一条命令的元数据。
    /// 布局使用 Sequential 保证在托管/非托管内存中按预期字节序排列，可直接按字节写入/读取。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct EntityCommandHead
    {
        /// <summary>
        /// 命令类型（Create/Destroy/Add/Remove/Set）。
        /// </summary>
        public readonly EntityCommandType Type;

        /// <summary>
        /// 可变数据长度（Uint16），例如组件数据的字节长度；为 0 表示无附加数据。
        /// </summary>
        public readonly ushort DataLength;

        public EntityCommandHead(EntityCommandType type, ushort dataLength)
        {
            Type = type;
            DataLength = dataLength;
        }
    }
}