using System.Numerics;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// 框架内部使用的共享组件掩码（internal）。
    /// 用于对引用类型的共享组件/单例进行位级快速筛选与集合运算。
    /// 设计为轻量值类型，采用固定段位表示（4 段 × 64 位 = 最多 256 个共享类型），
    /// 提供 Add/RemoveAt/On/All/Any/None 等常用操作以支持快速匹配逻辑。
    ///
    /// 说明：该类型仅供框架内部使用，不作为公共 API 暴露。
    /// 写入共享对象应仅在主线程进行；掩码用于在查询或系统调度中快速判断共享依赖。
    /// </summary>
    internal struct SharedComponentMask : IEquatable<SharedComponentMask>
    {
        internal const int MaxSharedCount = SharedComponentRegistry.MaxSharedCount;
        private const int IndexShift = SharedComponentRegistry.IndexShift;
        private const int SegmentMask = SharedComponentRegistry.SegmentMask;

        private ulong s0;
        private ulong s1;
        private ulong s2;
        private ulong s3;

        /// <summary>
        /// 空掩码实例（不包含任何共享类型）。
        /// </summary>
        public static readonly SharedComponentMask Empty = new(0, 0, 0, 0);

        /// <summary>
        /// 是否为空掩码（未包含任何共享类型）。
        /// </summary>
        public bool IsEmpty => Equals(Empty);

        /// <summary>
        /// 掩码中包含的共享类型数量（位计数）。
        /// </summary>
        public int ComponentCount => BitOperations.PopCount(s0) + BitOperations.PopCount(s1) + BitOperations.PopCount(s2) + BitOperations.PopCount(s3);

        /// <summary>
        /// 内部构造函数，用于按段初始化掩码值。
        /// </summary>
        internal SharedComponentMask(ulong s0, ulong s1, ulong s2, ulong s3)
        {
            this.s0 = s0;
            this.s1 = s1;
            this.s2 = s2;
            this.s3 = s3;
        }

        /// <summary>
        /// 将指定共享类型对应的位设置为 1（添加到掩码）。
        /// </summary>
        public void Add(SharedComponentType type) => SetBit(type.TypeIndex);

        /// <summary>
        /// 泛型快捷添加共享类型（T 必须为结构体类型）。
        /// </summary>
        public void Add<T>() => Add(SharedComponentType.Create<T>());

        /// <summary>
        /// 从掩码中移除指定共享类型（将对应位清零）。
        /// </summary>
        public void Remove(SharedComponentType type) => ClearBit(type.TypeIndex);

        /// <summary>
        /// 泛型快捷移除共享类型（T 必须为结构体类型）。
        /// </summary>
        public void Remove<T>() => Remove(SharedComponentType.Create<T>());

        /// <summary>
        /// 写入位操作：将指定索引对应的位设置为 1。
        /// </summary>
        /// <param name="index">共享类型索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBit(int index)
        {
            if (index <= 0 || index > MaxSharedCount)
                return;

            int seg = index >> IndexShift;
            int bit = index & SegmentMask;
            ref ulong segRef = ref GetSegmentRef(seg);
            segRef |= 1UL << bit;
        }

        /// <summary>
        /// 清除位操作：将指定索引对应的位清零。
        /// </summary>
        /// <param name="index">共享类型索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBit(int index)
        {
            if (index <= 0 || index > MaxSharedCount) return;
            int seg = index >> IndexShift;
            int bit = index & SegmentMask;
            ref ulong segRef = ref GetSegmentRef(seg);
            segRef &= ~(1UL << bit);
        }

        /// <summary>
        /// 检查掩码中是否包含指定共享类型。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool On(SharedComponentType type)
        {
            int index = type.TypeIndex;
            if (index <= 0 || index > MaxSharedCount) return false;
            int seg = index >> IndexShift;
            int bit = index & SegmentMask;
            return (GetSegmentRef(seg) & (1UL << bit)) != 0;
        }

        /// <summary>
        /// 泛型快捷检查（T 必须为值类型）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool On<T>() => On(SharedComponentType.Create<T>());

        /// <summary>
        /// 判断当前掩码是否包含 other 的所有位（即包含全部共享类型）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool All(in SharedComponentMask other)
        {
            return (s0 & other.s0) == other.s0
                && (s1 & other.s1) == other.s1
                && (s2 & other.s2) == other.s2
                && (s3 & other.s3) == other.s3;
        }

        /// <summary>
        /// 判断当前掩码与 other 是否存在任意共同位（交集非空）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Any(in SharedComponentMask other)
        {
            return (s0 & other.s0) != 0
                || (s1 & other.s1) != 0
                || (s2 & other.s2) != 0
                || (s3 & other.s3) != 0;
        }

        /// <summary>
        /// 判断当前掩码与 other 是否没有交集。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool None(in SharedComponentMask other) => !Any(other);

        /// <summary>
        /// 通过索引获取对应段的引用，以便进行位操作。
        /// </summary>
        /// <param name="index">段索引</param>
        /// <returns>对应段的引用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ulong GetSegmentRef(int index)
        {
            ref ulong u0Ref = ref Unsafe.AsRef(in s0);
            return ref Unsafe.Add(ref u0Ref, index);
        }

        public bool Equals(SharedComponentMask other) => s0 == other.s0 && s1 == other.s1 && s2 == other.s2 && s3 == other.s3;

        public override bool Equals(object? obj) => obj is SharedComponentMask other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(s0, s1, s2, s3);

        public override string ToString()
        {
            if (IsEmpty) return "SharedComponentMask: [Empty]";
            return $"SharedComponentMask: Count={ComponentCount}";
        }
    }
}