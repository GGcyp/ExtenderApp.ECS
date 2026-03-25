using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ExtenderApp.ECS.Queries;

namespace ExtenderApp.ECS.Commands
{
    /// <summary>
    /// 线程安全的命令缓冲写入端（轻量版）。
    /// 
    /// 语义：该类提供并发向命令缓冲中追加命令的能力（用于在工作线程生产命令），回放需在主线程由 <see cref="EntityCommandReader"/> 读取并执行。
    /// 设计要点：
    /// - 写入采用分段 arena（CommandBufferChunk），支持并发通过原子预留（CAS）追加字节；
    /// - 每条命令采用固定头部 <see cref="EntityCommandHead"/>，随后为目标实体字段（占位或真实实体），以及可选的数据区；
    /// - 当命令需要包含二进制数据（例如组件数据或查询描述符）时，会在头部的 <c>DataLength</c> 字段中注明字节长度；回放方须据此安全读取；
    /// - 回放后必须调用存储的 Clear/Dispose 路径以释放分配的非托管内存。
    /// 
    /// 线程安全：该类的写入方法均为线程安全（适用于多个写线程并发向同一缓冲追加），但回放必须在主线程进行以保证实体/原型数据一致性。
    /// </summary>
    public sealed unsafe class EntityCommandBuffer
    {
        /// <summary>
        /// 底层存储：用于管理已封存和当前活动的 chunk，并提供原子获取写入区的能力。
        /// </summary>
        private readonly EntityCommandStorage _storage;

        /// <summary>
        /// 构造：创建首个活动段并初始化虚拟实体计数器（由存储负责）。
        /// </summary>
        internal EntityCommandBuffer(EntityCommandStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// 创建一个新的虚拟实体（id 负数），并写入创建命令到缓冲区（线程安全）。回放时会替换为实际实体。
        /// </summary>
        /// <returns>返回新创建的虚拟实体</returns>
        public Entity CreateEntity() => _storage.CreateEntity();

        /// <summary>
        /// 写入销毁实体命令到缓冲区（线程安全）。
        /// </summary>
        public void DestroyEntity(Entity entity)
        {
            if (entity.IsEmpty)
                return;

            WriteSimpleCommand(EntityCommandType.DestroyEntity, entity);
        }

        /// <summary>
        /// 写入移除组件命令（仅记录 component type index，无组件数据）。
        /// </summary>
        public void RemoveComponent<T>(Entity entity) where T : struct
        {
            if (entity.IsEmpty)
                return;

            var compType = ComponentType.Create<T>();
            WriteTypeCommand(EntityCommandType.RemoveComponent, entity, compType.TypeIndex);
        }

        public void AddComponent<T>(Entity entity) where T : struct
        {
            if (entity.IsEmpty)
                return;

            var compType = ComponentType.Create<T>();
            WriteTypeCommand(EntityCommandType.AddComponent, entity, compType.TypeIndex);
        }

        /// <summary>
        /// 写入添加组件命令并包含组件数据（按值序列化）。
        /// </summary>
        public void AddComponent<T>(Entity entity, T component) where T : struct
        {
            if (entity.IsEmpty)
                return;

            var compType = ComponentType.Create<T>();
            WriteTypeAndDataCommand(EntityCommandType.AddComponent, entity, compType.TypeIndex, component);
        }

        /// <summary>
        /// 写入设置组件命令并包含组件数据（按值序列化）。
        /// </summary>
        public void SetComponent<T>(Entity entity, T component) where T : struct
        {
            if (entity.IsEmpty)
                return;

            var compType = ComponentType.Create<T>();
            WriteTypeAndDataCommand(EntityCommandType.SetComponent, entity, compType.TypeIndex, component);
        }

        public void ModifyComponent(Entity entity, Archetype archetype)
        {
            ModifyComponent(entity, archetype.ComponentMask);
        }

        /// <summary>
        /// 切换组件命令并包含组件数据（按值序列化）。回放时如果实体已存在该组件则设置，否则添加。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entity">目标实体</param>
        /// <param name="component">组件数据</param>
        public void ModifyComponent(Entity entity, ComponentMask componentTypes)
        {
            if (entity.IsEmpty)
                return;

            WriteMaskCommand(EntityCommandType.ModifyComponent, entity, componentTypes);
        }

        /// <summary>
        /// 写入按查询删除实体的命令的序列化格式说明（回放端应按此格式解析）：
        /// 数据区布局（按字节顺序）：
        /// - Query: <see cref="ComponentMask"/>（表示主查询掩码，非空）
        /// - All: <see cref="ComponentMask"/>（可为空）
        /// - Any: <see cref="ComponentMask"/>（可为空）
        /// - None: <see cref="ComponentMask"/>（可为空）
        /// - Relation: <see cref="Entities.RelationMask"/>（可为空）
        /// 
        /// 注：由于命令流需要统一的头部与实体字段，占用实体字段位置写入 <see cref="Entity.Empty"/> 作为占位，实际回放时使用上面序列化的查询描述符来匹配并删除实体。
        /// </summary>
        public void DestroyEntityForQuery(EntityQuery qunery)
        {
            if (qunery.IsEmpty)
                return;

            DestroyEntityForQuery(qunery.QueryDesc);
        }

        /// <summary>
        /// 内部写入：将 <see cref="EntityQueryDesc"/> 序列化并追加为一条 <see cref="EntityCommandType.DestroyEntitiesForQuery"/> 命令。
        /// 方法不会执行回放，仅将命令二进制写入当前活动段（或新增段）。
        /// </summary>
        internal void DestroyEntityForQuery(EntityQueryDesc desc)
        {
            if (desc.IsEmpty)
                return;

            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int compMaskSize = Marshal.SizeOf<ComponentMask>();
            int relationSize = Marshal.SizeOf<Entities.RelationMask>();
            int dataLength = compMaskSize * 4 + relationSize;
            ushort dataLenByte = (ushort)dataLength;

            int required = headSize + entitySize + dataLength; // head + placeholder entity + data

            var seg = _storage.GetBufferChunk(required, out int offset);
            var span = new Span<byte>(seg.Ptr, seg.Capacity);

            EntityCommandHead head = new(EntityCommandType.DestroyEntitiesForQuery, dataLenByte);
            MemoryMarshal.Write(span.Slice(offset, headSize), in head);

            var emptyEntity = Entity.Empty;
            MemoryMarshal.Write(span.Slice(offset + headSize, entitySize), in emptyEntity);

            int writePos = offset + headSize + entitySize;
            MemoryMarshal.Write(span.Slice(writePos, compMaskSize), in desc.Query);
            writePos += compMaskSize;
            MemoryMarshal.Write(span.Slice(writePos, compMaskSize), in desc.All);
            writePos += compMaskSize;
            MemoryMarshal.Write(span.Slice(writePos, compMaskSize), in desc.Any);
            writePos += compMaskSize;
            MemoryMarshal.Write(span.Slice(writePos, compMaskSize), in desc.None);
            writePos += compMaskSize;

            MemoryMarshal.Write(span.Slice(writePos, relationSize), in desc.Relation);
        }

        #region Write

        /// <summary>
        /// 向当前活动段写入仅目标 id 的命令（例如 Destroy）。当当前段空间不足时尝试 CAS 替换段并重试。
        /// </summary>
        private void WriteSimpleCommand(EntityCommandType type, Entity target)
        {
            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int required = headSize + entitySize; // head + Entity(id+version)

            var seg = _storage.GetBufferChunk(required, out int offset);

            var span = new Span<byte>(seg.Ptr, seg.Capacity);
            EntityCommandHead head = new(type, 0);
            MemoryMarshal.Write(span.Slice(offset, headSize), in head);
            MemoryMarshal.Write(span.Slice(offset + headSize, entitySize), in target);
        }

        /// <summary>
        /// 向当前活动段写入包含 typeIndex 的命令（例如 RemoveComponent）。
        /// </summary>
        private void WriteTypeCommand(EntityCommandType type, Entity target, ushort typeIndex)
        {
            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int required = headSize + entitySize + 2; // head + entity + typeIndex

            var seg = _storage.GetBufferChunk(required, out int offset);
            var span = new Span<byte>(seg.Ptr, seg.Capacity);
            EntityCommandHead head = new(type, 0);
            MemoryMarshal.Write(span.Slice(offset, headSize), in head);
            MemoryMarshal.Write(span.Slice(offset + headSize, entitySize), in target);
            MemoryMarshal.Write(span.Slice(offset + headSize + entitySize, 2), in typeIndex);
        }

        private void WriteMaskCommand(EntityCommandType type, Entity entity, ComponentMask componentTypes)
        {
            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int dataLength = Marshal.SizeOf<ComponentMask>();
            ushort dataLenByte = (ushort)dataLength;
            int required = headSize + entitySize + 2 + dataLength; // head + entity + typeIndex + data

            var seg = _storage.GetBufferChunk(required, out int offset);
            var span = new Span<byte>(seg.Ptr, seg.Capacity);
            EntityCommandHead head = new(type, dataLenByte);
            MemoryMarshal.Write(span.Slice(offset, headSize), in head);
            MemoryMarshal.Write(span.Slice(offset + headSize, entitySize), in entity);
            MemoryMarshal.Write(span.Slice(offset + headSize + entitySize, 2), in componentTypes);
            MemoryMarshal.Write(span.Slice(offset + headSize + entitySize + 2, dataLength), in componentTypes);
        }

        /// <summary>
        /// 向当前活动段写入包含 typeIndex 与数据的命令（例如 AddComponent/SetComponent）。
        /// </summary>
        private void WriteTypeAndDataCommand<T>(EntityCommandType type, Entity target, ushort typeIndex, T value) where T : struct
        {
            int headSize = Marshal.SizeOf<EntityCommandHead>();
            int entitySize = Unsafe.SizeOf<Entity>();
            int dataLength = Marshal.SizeOf<T>();
            ushort dataLenByte = (ushort)dataLength;
            int required = headSize + entitySize + 2 + dataLength; // head + entity + typeIndex + data

            var seg = _storage.GetBufferChunk(required, out int offset);
            var span = new Span<byte>(seg.Ptr, seg.Capacity);
            EntityCommandHead head = new(type, dataLenByte);
            MemoryMarshal.Write(span.Slice(offset, headSize), in head);
            MemoryMarshal.Write(span.Slice(offset + headSize, entitySize), in target);
            MemoryMarshal.Write(span.Slice(offset + headSize + entitySize, 2), in typeIndex);
            MemoryMarshal.Write(span.Slice(offset + headSize + entitySize + 2, dataLength), in value);
        }

        #endregion Write
    }
}