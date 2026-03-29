using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtenderApp.ECS
{
    /// <summary>
    /// 组件掩码：使用多个 64 位无符号整数表示组件存在位的集合。 每一位对应一个组件类型索引，用于快速集合运算（包含、交集、编码位置等）。
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public struct ComponentMask : IEquatable<ComponentMask>, IEnumerable<ComponentType>
    {
        /// <summary>
        /// 每段位数（64），用于计算段索引与位偏移。
        /// </summary>
        private const int SegmentBits = 64;

        /// <summary>
        /// 段落数量（8），总共支持 512 个组件类型索引（0..511）。
        /// </summary>
        private const int SegmentCount = 8;

        /// <summary>
        /// 最大支持的组件数量（基于段数 * 每段位数）。
        /// </summary>
        private const int MaxComponentCount = SegmentCount * SegmentBits;

        /// <summary>
        /// 段内位掩码（用于计算 index % 64）。
        /// </summary>
        private const int SegmentMask = SegmentBits - 1;

        /// <summary>
        /// 将索引右移以获得段序号（等价于 index / 64）。
        /// </summary>
        private const int IndexShift = 6;

        /// <summary>
        /// 获取一个空掩码实例，表示没有任何组件。所有位均为 0。
        /// </summary>
        public static readonly ComponentMask Empty = new(0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        /// 掩码的第 0 段（位索引 0..63）。
        /// </summary>
        private ulong u0;

        /// <summary>
        /// 掩码的第 1 段（位索引 64..127）。
        /// </summary>
        private ulong u1;

        /// <summary>
        /// 掩码的第 2 段（位索引 128..191）。
        /// </summary>
        private ulong u2;

        /// <summary>
        /// 掩码的第 3 段（位索引 192..255）。
        /// </summary>
        private ulong u3;

        /// <summary>
        /// 掩码的第 4 段（位索引 256..319）。
        /// </summary>
        private ulong u4;

        /// <summary>
        /// 掩码的第 5 段（位索引 320..383）。
        /// </summary>
        private ulong u5;

        /// <summary>
        /// 掩码的第 6 段（位索引 384..447）。
        /// </summary>
        private ulong u6;

        /// <summary>
        /// 掩码的第 7 段（位索引 448..511）。
        /// </summary>
        private ulong u7;

        /// <summary>
        /// 获取一个值，指示当前掩码是否为空（即没有任何组件）。当所有位均为 0 时返回 true。
        /// </summary>
        public bool IsEmpty => Equals(Empty);

        /// <summary>
        /// 获取当前掩码中设置为 1 的组件数量（即包含的组件类型数量）。通过对每段使用 BitOperations.PopCount 计算总数。
        /// </summary>
        public int ComponentCount
        {
            get
            {
                int count = 0;
                foreach (var item in GetMaskSpan())
                {
                    count += BitOperations.PopCount(item);
                }
                return count;
            }
        }

        /// <summary>
        /// 默认构造函数，创建一个空掩码（所有位为 0）。表示没有任何组件。
        /// </summary>
        public ComponentMask() : this(0, 0, 0, 0, 0, 0, 0, 0)
        {
        }

        /// <summary>
        /// 使用另一个掩码的值创建一个新的掩码实例（复制构造函数）。相当于对每个段进行赋值复制。
        /// </summary>
        /// <param name="mask">指定的掩码实例。</param>
        public ComponentMask(in ComponentMask mask) : this(mask.u0, mask.u1, mask.u2, mask.u3, mask.u4, mask.u5, mask.u6, mask.u7)
        {
        }

        /// <summary>
        /// 使用单个组件类型创建一个掩码。对应组件类型的位将被设置为 1。
        /// </summary>
        /// <param name="componentType">要设置的组件类型。</param>
        public ComponentMask(ComponentType componentType) : this(0, 0, 0, 0, 0, 0, 0, 0)
        {
            SetComponent(componentType);
        }

        /// <summary>
        /// 使用八个 64 位段创建一个新的组件掩码（内部使用）。
        /// </summary>
        /// <param name="u0">第 0 段的位掩码。</param>
        /// <param name="u1">第 1 段的位掩码。</param>
        /// <param name="u2">第 2 段的位掩码。</param>
        /// <param name="u3">第 3 段的位掩码。</param>
        internal ComponentMask(ulong u0, ulong u1, ulong u2, ulong u3, ulong u4, ulong u5, ulong u6, ulong u7)
        {
            this.u0 = u0;
            this.u1 = u1;
            this.u2 = u2;
            this.u3 = u3;
            this.u4 = u4;
            this.u5 = u5;
            this.u6 = u6;
            this.u7 = u7;
        }

        /// <summary>
        /// 将指定组件添加到掩码（等同于 SetComponent）。
        /// </summary>
        /// <param name="type">要添加的组件类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ComponentType type) => SetComponent(type);

        /// <summary>
        /// 将指定泛型组件类型添加到掩码（等同于 SetComponent(ComponentType) 的泛型快捷重载）。
        /// </summary>
        /// <typeparam name="T">要添加的组件类型（值类型并实现 IComponent）。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>() => Add(ComponentType.Create<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>() => SetComponent(ComponentType.Create<T>());

        /// <summary>
        /// 将指定组件类型的对应位设置为 1，表示包含该组件。
        /// </summary>
        /// <param name="componentType">要设置的组件类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent(ComponentType componentType) => SetBit(componentType.TypeIndex);

        /// <summary>
        /// 将多个组件类型添加到掩码，等同于对每个组件类型调用 SetComponent。提供 params 重载以方便一次设置多个组件。
        /// </summary>
        /// <param name="componentTypes">要设置的组件类型数组。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponents(IEnumerable<ComponentType> componentTypes)
        {
            foreach (var componentType in componentTypes)
            {
                SetComponent(componentType);
            }
        }

        /// <summary>
        /// 将另一个掩码中的所有组件类型添加到当前掩码（位或操作）。相当于对每个段进行按位或运算。提供一个快速的批量设置方法。
        /// </summary>
        /// <param name="mask">指定掩码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponents(in ComponentMask mask)
        {
            foreach (var type in mask)
            {
                SetComponent(type);
            }
        }

        /// <summary>
        /// 将指定索引位置的位设置为 1。 索引范围在 0 到 MaxComponentCount-1 之间（包含）。
        /// </summary>
        /// <param name="index">组件类型的整数索引。</param>
        /// <exception cref="ArgumentOutOfRangeException">当索引超出范围时抛出。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBit(int index)
        {
            int ulongIndex = index >> IndexShift; // index / 64
            int bitOffset = index & SegmentMask; // index % 64

            ref ulong segment = ref GetMaskUlongRef(ulongIndex);
            segment |= 1UL << bitOffset;
        }

        /// <summary>
        /// 从掩码中移除指定组件（将对应位清零）。
        /// </summary>
        /// <param name="componentType">要移除的组件类型。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ComponentType componentType) => ClearBit(componentType.TypeIndex);

        /// <summary>
        /// 从掩码中移除指定泛型组件类型（等同于 RemoveAt(ComponentType) 的泛型快捷重载）。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型（值类型并实现 IComponent）。</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>() => Remove(ComponentType.Create<T>());

        /// <summary>
        /// 将另一个掩码中的所有组件类型从当前掩码中移除（位与非操作）。相当于对每个段进行按位与非运算。提供一个快速的批量移除方法。
        /// </summary>
        /// <param name="mask">指定掩码。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(in ComponentMask mask)
        {
            foreach (var type in mask)
            {
                Remove(type);
            }
        }

        /// <summary>
        /// 将指定索引位置的位设置为 0。 索引范围在 0 到 MaxComponentCount-1 之间（包含）。
        /// </summary>
        /// <param name="index">组件类型的整数索引。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBit(int index)
        {
            int ulongIndex = index >> IndexShift; // index / 64
            int bitOffset = index & SegmentMask; // index % 64

            ref ulong segment = ref GetMaskUlongRef(ulongIndex);
            segment &= ~(1UL << bitOffset);
        }

        /// <summary>
        /// 判断当前掩码是否包含指定组件。
        /// </summary>
        /// <param name="componentType">要检测的组件类型。</param>
        /// <returns>如果当前掩码包含该组件则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool On(in ComponentType componentType)
        {
            int index = componentType.TypeIndex;
            int ulongIndex = index >> IndexShift;
            int bitOffset = index & SegmentMask;
            return (GetMaskUlongRef(ulongIndex) & (1UL << bitOffset)) != 0;
        }

        /// <summary>
        /// 检查当前掩码是否包含指定泛型组件类型（等同于 On(ComponentType) 的泛型快捷重载）。
        /// </summary>
        /// <typeparam name="T">要检查的组件类型（值类型并实现 IComponent）。</typeparam>
        /// <returns>包含返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool On<T>() => On(ComponentType.Create<T>());

        /// <summary>
        /// 判断当前掩码是否包含另一个掩码的所有组件（位包含）。 若 other 的任何位在当前掩码中为 1，则该位必须在当前掩码中也是 1。
        /// </summary>
        /// <param name="other">要比较的另一个掩码。</param>
        /// <returns>如果当前掩码包含 other 的所有位则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool All(in ComponentMask other)
        {
            return (u0 & other.u0) == other.u0
                && (u1 & other.u1) == other.u1
                && (u2 & other.u2) == other.u2
                && (u3 & other.u3) == other.u3
                && (u4 & other.u4) == other.u4
                && (u5 & other.u5) == other.u5
                && (u6 & other.u6) == other.u6
                && (u7 & other.u7) == other.u7;
        }

        /// <summary>
        /// 判断当前掩码与另一个掩码是否有任意共同的组件（交集不为空）。
        /// </summary>
        /// <param name="other">要比较的另一个掩码。</param>
        /// <returns>若存在任意位在两者中都为 1，则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Any(in ComponentMask other)
        {
            return (u0 & other.u0) != 0
                || (u1 & other.u1) != 0
                || (u2 & other.u2) != 0
                || (u3 & other.u3) != 0
                || (u4 & other.u4) != 0
                || (u5 & other.u5) != 0
                || (u6 & other.u6) != 0
                || (u7 & other.u7) != 0;
        }

        /// <summary>
        /// 判断当前掩码与另一个掩码是否没有交集。
        /// </summary>
        /// <param name="other">要比较的另一个掩码。</param>
        /// <returns>没有交集返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool None(in ComponentMask other) => !Any(other);

        /// <summary>
        /// 获取当前掩码与指定掩码中相同组件类型的数量（交集位数量）。
        /// </summary>
        /// <param name="other">要比较的掩码。</param>
        /// <returns>两个掩码中相同组件类型的个数。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSameTypeCount(in ComponentMask other)
        {
            return BitOperations.PopCount(u0 & other.u0)
                + BitOperations.PopCount(u1 & other.u1)
                + BitOperations.PopCount(u2 & other.u2)
                + BitOperations.PopCount(u3 & other.u3)
                + BitOperations.PopCount(u4 & other.u4)
                + BitOperations.PopCount(u5 & other.u5)
                + BitOperations.PopCount(u6 & other.u6)
                + BitOperations.PopCount(u7 & other.u7);
        }

        /// <summary>
        /// 尝试计算指定组件在当前掩码中的编码位置（基于已设置位的顺序，0-based）。 例如：掩码中有 3 个有效组件，若目标组件是第二个被编码的，则返回 1（从 0 开始）。 若组件位未设置则返回 false。
        /// </summary>
        /// <param name="componentType">要查询的组件类型。</param>
        /// <param name="position">输出的 0-based 编码位置（若返回 true）。</param>
        /// <returns>若找到则返回 true 并设置 position；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEncodedPosition(ComponentType componentType, out int position)
        {
            position = 0;
            int index = componentType.TypeIndex;
            if (index < 0 || index >= MaxComponentCount)
                return false;

            int ulongIndex = index >> IndexShift;
            int bitOffset = index & SegmentMask;

            // 检查该位是否存在
            ulong maskBit = 1UL << bitOffset;

            if ((GetMaskUlongRef(ulongIndex) & maskBit) == 0)
                return false;

            // 统计目标段之前所有段的 1 的数量
            for (int i = 0; i < ulongIndex; i++)
            {
                position += BitOperations.PopCount(GetMaskUlongRef(i));
            }

            // 加上目标段中目标位之前的 1 的数量
            position += BitCountBefore(GetMaskUlongRef(ulongIndex), bitOffset);
            return true;

            int BitCountBefore(ulong value, int bitOffset)
            {
                return BitOperations.PopCount(value & ((bitOffset == 0) ? 0UL : ((1UL << bitOffset) - 1UL)));
            }
        }

        /// <summary>
        /// 尝试计算指定掩码中所有组件在当前掩码中的编码位置，跳过未设置的组件。 positions 数组长度必须至少等于 AddMask 中组件数量。
        /// </summary>
        /// <param name="mask">要查询的掩码。</param>
        /// <param name="positions">输出的编码位置数组。</param>
        /// <returns>若成功获取所有编码位置则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEncodedPosition(in ComponentMask mask, scoped Span<int> positions)
        {
            int i = 0;
            int position = 0;
            foreach (var componentType in mask)
            {
                if (positions.Length <= i)
                    return false;

                int index = componentType.TypeIndex;
                int ulongIndex = index >> IndexShift;
                int bitOffset = index & SegmentMask;

                // 检查该位是否存在
                ulong maskBit = 1UL << bitOffset;

                if ((GetMaskUlongRef(ulongIndex) & maskBit) == 0)
                    continue;

                // 加上目标段中目标位之前的 1 的数量
                position += BitCountBefore(GetMaskUlongRef(ulongIndex), bitOffset);

                positions[i] = position;
                i++;
            }
            return true;

            int BitCountBefore(ulong value, int bitOffset)
            {
                return BitOperations.PopCount(value & ((bitOffset == 0) ? 0UL : ((1UL << bitOffset) - 1UL)));
            }
        }

        /// <summary>
        /// 尝试计算指定泛型组件在当前掩码中的编码位置（等同于 TryGetEncodedPosition(ComponentType, out int) 的泛型重载）。
        /// </summary>
        /// <typeparam name="T">要查询的组件类型（值类型并实现 IComponent）。</typeparam>
        /// <param name="position">输出的 0-based 编码位置（若返回 true）。</param>
        /// <returns>若找到则返回 true 并设置 position；否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEncodedPosition<T>(out int position) => TryGetEncodedPosition(ComponentType.Create<T>(), out position);

        /// <summary>
        /// 获取一个 ReadOnlySpan，包含掩码的所有段（u0..u7）。通过 MemoryMarshal 创建 Span 以避免数组分配。
        /// </summary>
        /// <returns>包含掩码所有段的只读 Span。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ReadOnlySpan<ulong> GetMaskSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in u0), SegmentCount);

        /// <summary>
        /// 获取一个 ref ulong 引用，指向指定段索引的段值。通过 Unsafe 进行引用偏移以避免数组分配。
        /// </summary>
        /// <param name="index">段索引（0-based）。</param>
        /// <returns>指定段的 ref ulong 引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ulong GetMaskUlongRef(int index)
        {
            ref ulong u0Ref = ref Unsafe.AsRef(in u0);
            return ref Unsafe.Add(ref u0Ref, index);
        }

        /// <summary>
        /// 深拷贝复制当前掩码实例，返回一个新的掩码对象。相当于调用复制构造函数。
        /// </summary>
        /// <returns>返回一个新的 ComponentMask 实例，包含与当前掩码相同的值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentMask Cone() => new(this);

        /// <summary>
        /// 实现 IEquatable{ComponentMask}，判断掩码相等性。
        /// </summary>
        /// <param name="other">另一个掩码。</param>
        /// <returns>若相等则返回 true。</returns>
        public bool Equals(ComponentMask other) => GetMaskSpan().SequenceEqual(other.GetMaskSpan());

        /// <summary>
        /// 判断对象是否是相同的掩码实例。
        /// </summary>
        public override bool Equals(object? obj) => obj is ComponentMask other && Equals(other);

        /// <summary>
        /// 生成掩码的哈希码（基于所有段的值）。
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(u0, u1, u2, u3, u4, u5, u6, u7);

        /// <summary>
        /// 返回当前掩码的可读字符串表示，用于调试与日志。
        /// </summary>
        public override string ToString()
        {
            if (IsEmpty)
            {
                return "ComponentMask: [Empty]";
            }

            StringBuilder stringBuilder = new();
            stringBuilder.Append("ComponentMask: [");
            foreach (var item in this)
            {
                stringBuilder.Append(item.ToString());
                stringBuilder.Append(", ");
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2); // 移除最后的 ", "
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 等号运算符重载，判断两个掩码是否相等。
        /// </summary>
        public static bool operator ==(ComponentMask left, ComponentMask right) => left.Equals(right);

        /// <summary>
        /// 不等号运算符重载，判断两个掩码是否不相等。
        /// </summary>
        public static bool operator !=(ComponentMask left, ComponentMask right) => !left.Equals(right);

        public static bool operator &(ComponentMask left, ComponentMask right) => left.Any(right);

        public static bool operator |(ComponentMask left, ComponentMask right) => left.All(right);

        public static implicit operator ComponentMask(ComponentType componentType) => new(componentType);

        #region Enumerable

        // 提供一个模式枚举器（struct），使得在 foreach 中不会产生托管堆分配。
        /// <summary>
        /// 获取枚举器，用于遍历掩码中所有存在的组件类型。
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(u0, u1, u2, u3, u4, u5, u6, u7);

        /// <summary>
        /// IEnumerable{ComponentType} 的显式实现。
        /// </summary>
        IEnumerator<ComponentType> IEnumerable<ComponentType>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 非泛型 IEnumerable 的显式实现。
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// ComponentMask 的结构化枚举器：按位扫描每个 64 位段，返回 ComponentType。 使用位操作（TrailingZeroCount 与清除最低位）高效遍历。
        /// </summary>
        public struct Enumerator : IEnumerator<ComponentType>
        {
            /// <summary>
            /// 只读备份段 0（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w0;

            /// <summary>
            /// 只读备份段 1（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w1;

            /// <summary>
            /// 只读备份段 2（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w2;

            /// <summary>
            /// 只读备份段 3（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w3;

            /// <summary>
            /// 只读备份段 4（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w4;

            /// <summary>
            /// 只读备份段 5（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w5;

            /// <summary>
            /// 只读备份段 6（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w6;

            /// <summary>
            /// 只读备份段 7（用于 Reset 时恢复）。
            /// </summary>
            private readonly ulong _w7;

            /// <summary>
            /// 当前正在扫描的工作段 0（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w0;

            /// <summary>
            /// 当前正在扫描的工作段 1（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w1;

            /// <summary>
            /// 当前正在扫描的工作段 2（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w2;

            /// <summary>
            /// 当前正在扫描的工作段 3（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w3;

            /// <summary>
            /// 当前正在扫描的工作段 4（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w4;

            /// <summary>
            /// 当前正在扫描的工作段 5（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w5;

            /// <summary>
            /// 当前正在扫描的工作段 6（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w6;

            /// <summary>
            /// 当前正在扫描的工作段 7（会被修改以清除已扫描的位）。
            /// </summary>
            private ulong w7;

            private ComponentType _current;

            /// <summary>
            /// 当前段的基偏移（以位为单位）：0、64、128 或 192 等。 用于将段内的位索引转换为全局组件索引。
            /// </summary>
            private int _baseOffset;

            /// <summary>
            /// 使用提供的八个段值创建枚举器。
            /// </summary>
            /// <param name="w0">段 0 的值。</param>
            /// <param name="w1">段 1 的值。</param>
            /// <param name="w2">段 2 的值。</param>
            /// <param name="w3">段 3 的值。</param>
            internal Enumerator(ulong w0,
                ulong w1,
                ulong w2,
                ulong w3,
                ulong w4,
                ulong w5,
                ulong w6,
                ulong w7)
            {
                _w0 = w0;
                _w1 = w1;
                _w2 = w2;
                _w3 = w3;
                _w4 = w4;
                _w5 = w5;
                _w6 = w6;
                _w7 = w7;

                this.w0 = w0;
                this.w1 = w1;
                this.w2 = w2;
                this.w3 = w3;
                this.w4 = w4;
                this.w5 = w5;
                this.w6 = w6;
                this.w7 = w7;

                _current = ComponentType.Empty;
                _baseOffset = 0;
            }

            /// <summary>
            /// 当前枚举到的组件类型。
            /// </summary>
            public ComponentType Current => _current;

            object IEnumerator.Current => Current;

            /// <summary>
            /// 前进到下一个存在的组件位。
            /// </summary>
            /// <returns>如果找到下一个组件则返回 true，否则返回 false（枚举结束）。</returns>
            public bool MoveNext()
            {
                if (w0 != 0)
                {
                    _current = GetComponentType(w0);
                    w0 &= w0 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w1 != 0)
                {
                    _current = GetComponentType(w1);
                    w1 &= w1 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w2 != 0)
                {
                    _current = GetComponentType(w2);
                    w2 &= w2 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w3 != 0)
                {
                    _current = GetComponentType(w3);
                    w3 &= w3 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w4 != 0)
                {
                    _current = GetComponentType(w4);
                    w4 &= w4 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w5 != 0)
                {
                    _current = GetComponentType(w5);
                    w5 &= w5 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w6 != 0)
                {
                    _current = GetComponentType(w6);
                    w6 &= w6 - 1;
                    return true;
                }

                _baseOffset += 64;

                if (w7 != 0)
                {
                    _current = GetComponentType(w7);
                    w7 &= w7 - 1;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 将段内最低位的位索引转换为 ComponentType。 使用 BitOperations.TrailingZeroCount 获取最低 1 位的位置。
            /// </summary>
            /// <param name="w">要计算的段值。</param>
            /// <returns>对应的 ComponentType。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ComponentType GetComponentType(ulong w)
            {
                int tz = BitOperations.TrailingZeroCount(w);
                return ComponentType.GetCommandType((ushort)(_baseOffset + tz));
            }

            /// <summary>
            /// 将工作段恢复为初始备份值，以便重新开始枚举。
            /// </summary>
            public void Reset()
            {
                w0 = _w0;
                w1 = _w1;
                w2 = _w2;
                w3 = _w3;
                w4 = _w4;
                w5 = _w5;
                w6 = _w6;
                w7 = _w7;
                _baseOffset = 0;
            }

            /// <summary>
            /// 释放枚举器（无操作，存在以满足接口）。
            /// </summary>
            public void Dispose()
            {
            }
        }

        #endregion Enumerable
    }
}