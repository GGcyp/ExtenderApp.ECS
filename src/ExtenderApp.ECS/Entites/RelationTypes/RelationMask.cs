using System.Collections;
using System.Runtime.CompilerServices;

namespace ExtenderApp.ECS.Entities
{
    /// <summary>
    /// 关系掩码。 使用 64 位位图表示关系类型集合，每一位对应一个 <see cref="RelationType" /> 的索引。
    /// </summary>
    public struct RelationMask : IEquatable<RelationMask>, IEnumerable<RelationType>
    {
        /// <summary>
        /// 一个空的关系掩码实例，表示没有任何关系类型被设置。
        /// </summary>
        public static readonly RelationMask Empty = new(0);

        /// <summary>
        /// 位图数据。
        /// </summary>
        private ulong mask;

        /// <summary>
        /// 当前掩码是否为空。
        /// </summary>
        public bool IsEmpty => mask == 0;

        /// <summary>
        /// 获取当前掩码中设置的关系类型数量。
        /// </summary>
        public int RelationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int count = 0;
                ulong tempMask = mask;
                while (tempMask != 0)
                {
                    count += (int)(tempMask & 1);
                    tempMask >>= 1;
                }
                return count;
            }
        }

        /// <summary>
        /// 使用单个关系类型初始化关系掩码。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        public RelationMask(RelationType relationType) : this(0)
        {
            Add(relationType);
        }

        /// <summary>
        /// 初始化一个空的关系掩码。
        /// </summary>
        public RelationMask() : this(0)
        {
        }

        /// <summary>
        /// 使用指定位图值初始化关系掩码。
        /// </summary>
        /// <param name="mask">位图值。</param>
        internal RelationMask(ulong mask) => this.mask = mask;

        /// <summary>
        /// 设置关系类型位。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        public void Add<T>() where T : struct => Add(RelationType.Create<T>());

        /// <summary>
        /// 添加关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        public void Add(RelationType relationType) => SetBit(relationType.Id);

        /// <summary>
        /// 移除指定关系。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        public void Remove<T>() where T : struct => Remove(RelationType.Create<T>());   

        /// <summary>
        /// 移除关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        public void Remove(RelationType relationType) => ClearBit(relationType.Id);

        /// <summary>
        /// 判断当前掩码是否包含指定关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>包含返回 true；否则返回 false。</returns>
        public bool On(RelationType relationType) => (mask & (1UL << relationType.Id)) != 0;

        /// <summary>
        /// 判断当前掩码是否包含指定泛型关系类型。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <returns>包含返回 true；否则返回 false。</returns>
        public bool On<T>() where T : struct => On(RelationType.Create<T>());

        /// <summary>
        /// 判断当前掩码是否与给定关系掩码存在交集（任意一个关系位同时为 1）。
        /// </summary>
        /// <param name="relationTypes">要比较的关系掩码。</param>
        /// <returns>存在交集返回 true；否则返回 false。</returns>
        public bool On(RelationMask relationTypes) => (mask & relationTypes.mask) != 0;

        /// <summary>
        /// 判断当前掩码是否不包含指定泛型关系类型。
        /// </summary>
        /// <typeparam name="T">关系类型的结构体类型。</typeparam>
        /// <returns>不包含返回 true；否则返回 false。</returns>
        public bool Off<T>() where T : struct => Off(RelationType.Create<T>());

        /// <summary>
        /// 判断当前掩码是否不包含指定关系类型。
        /// </summary>
        /// <param name="relationType">关系类型。</param>
        /// <returns>不包含返回 true；否则返回 false。</returns>
        public bool Off(RelationType relationType) => (mask & (1UL << relationType.Id)) == 0;

        /// <summary>
        /// 判断当前掩码与给定关系掩码是否完全不相交（交集为空）。
        /// </summary>
        /// <param name="relationTypes">要比较的关系掩码。</param>
        /// <returns>无交集返回 true；否则返回 false。</returns>
        public bool Off(RelationMask relationTypes) => (mask & relationTypes.mask) == 0;

        /// <summary>
        /// 设置指定索引位为 1。
        /// </summary>
        /// <param name="index">位索引。</param>
        public void SetBit(int index) => mask |= 1UL << index;

        /// <summary>
        /// 清除指定索引位（设置为 0）。
        /// </summary>
        /// <param name="index">位索引。</param>
        public void ClearBit(int index) => mask &= ~(1UL << index);

        /// <summary>
        /// 获取结构体枚举器（foreach 优先走该路径，避免装箱）。
        /// </summary>
        /// <returns>结构体枚举器。</returns>
        public Enumerator GetEnumerator() => new(mask);

        /// <summary>
        /// 获取泛型接口枚举器。
        /// </summary>
        /// <returns>枚举器实例。</returns>
        IEnumerator<RelationType> IEnumerable<RelationType>.GetEnumerator() => new Enumerator(mask);

        /// <summary>
        /// 获取非泛型接口枚举器。
        /// </summary>
        /// <returns>枚举器实例。</returns>
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(mask);

        /// <summary>
        /// 判断当前掩码是否与另一个掩码相等。
        /// </summary>
        /// <param name="other">另一个关系掩码。</param>
        /// <returns>位图完全一致返回 true；否则返回 false。</returns>
        public bool Equals(RelationMask other) => this.mask == other.mask;

        /// <summary>
        /// 判断当前掩码是否与指定对象相等。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>相等返回 true；否则返回 false。</returns>
        public override bool Equals(object? obj) => obj is RelationMask other && Equals(other);

        /// <summary>
        /// 获取当前掩码哈希码。
        /// </summary>
        /// <returns>哈希码值。</returns>
        public override int GetHashCode() => mask.GetHashCode();

        /// <summary>
        /// 返回关系掩码的字符串表示。
        /// </summary>
        /// <returns>字符串表示。</returns>
        public override string ToString() => $"RelationMask: {mask}";

        /// <summary>
        /// 判断两个关系掩码是否相等。
        /// </summary>
        public static bool operator ==(RelationMask left, RelationMask right) => left.Equals(right);

        /// <summary>
        /// 判断两个关系掩码是否不相等。
        /// </summary>
        public static bool operator !=(RelationMask left, RelationMask right) => !left.Equals(right);

        /// <summary>
        /// 关系掩码结构体枚举器。 按位从低到高遍历掩码中被设置的关系类型。
        /// </summary>
        public struct Enumerator : IEnumerator<RelationType>
        {
            private readonly ulong _mask;
            private int _index;

            /// <summary>
            /// 初始化枚举器。
            /// </summary>
            /// <param name="mask">待遍历掩码。</param>
            internal Enumerator(ulong mask)
            {
                _mask = mask;
                _index = 0;
                Current = RelationType.Empty;
            }

            /// <summary>
            /// 当前关系类型（在 <see cref="MoveNext" /> 返回 true 后有效）。
            /// </summary>
            public RelationType Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// 移动到下一个被设置位对应的关系类型。
            /// </summary>
            /// <returns>存在下一项返回 true；否则返回 false。</returns>
            public bool MoveNext()
            {
                while (_index < 64)
                {
                    int bitIndex = _index++;
                    if ((_mask & (1UL << bitIndex)) == 0)
                        continue;

                    Current = RelationType.GetCommandType(bitIndex);
                    return true;
                }

                Current = RelationType.Empty;
                return false;
            }

            /// <summary>
            /// 将枚举器重置到初始位置。
            /// </summary>
            public void Reset()
            {
                _index = 0;
                Current = RelationType.Empty;
            }

            /// <summary>
            /// 释放资源（无托管资源，空实现）。
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}