using ExtenderApp.ECS.Abstract;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 将一个或多个结构体枚举器（实现 <see cref="IStructEnumerator{T}"/> 的 struct）组合为一个并行枚举器。
    ///
    /// 说明：这些类型接受若干个结构体枚举器（每个枚举器返回某一类型的项），并将它们按步长同步推进。
    /// 当且仅当所有子枚举器同时能推进（MoveNext 返回 true）时，组合枚举器才返回 true 并通过 Current 同步返回所有子枚举器的当前项（以元组形式）。
    ///
    /// 设计要点：
    /// - 所有组合类型均为 struct，并通过泛型约束保持零装箱（避免把 struct 转为接口或 object）。
    /// - 提供 1 到 5 个子枚举器的组合实现，覆盖常用场景。
    /// - 每个组合器都实现 <see cref="IStructEnumerator{T}"/>，可与 `GenericEnumerator` 一起使用以实现泛型统一处理。
    /// </summary>
    public static class MulticastEnumerator
    {
        /// <summary>
        /// 创建单枚举器的 Multicast 组合器（透传单个枚举器）。
        /// </summary>
        /// <typeparam name="TEnum1">第一个（也是唯一）枚举器类型，必须为 struct 且实现 <see cref="IStructEnumerator{TItem1}"/>。</typeparam>
        /// <typeparam name="TItem1">第一个枚举器返回项的类型。</typeparam>
        /// <param name="enum1">要组合的第一个枚举器（以 in 引用传入以避免不必要拷贝）。</param>
        /// <returns>返回一个可用于同步枚举的 <see cref="MulticastEnumerator{TEnum1,TItem1}.Enumerator"/> 的封装。</returns>
        public static MulticastEnumerator<TEnum1, TItem1> Create<TEnum1, TItem1>(in TEnum1 enum1)
            where TEnum1 : struct, IStructEnumerator<TItem1>
            => new MulticastEnumerator<TEnum1, TItem1>(enum1);

        /// <summary>
        /// 创建两个枚举器的 Multicast 组合器。组合器在 MoveNext 时同时推进两个枚举器，
        /// 并在两个枚举器都成功时返回一个包含两者当前项的元组。
        /// </summary>
        /// <typeparam name="TEnum1">第一个枚举器类型，必须为 struct 且实现 <see cref="IStructEnumerator{TItem1}"/>。</typeparam>
        /// <typeparam name="TItem1">第一个枚举器返回项的类型。</typeparam>
        /// <typeparam name="TEnum2">第二个枚举器类型，必须为 struct 且实现 <see cref="IStructEnumerator{TItem2}"/>。</typeparam>
        /// <typeparam name="TItem2">第二个枚举器返回项的类型。</typeparam>
        /// <param name="enum1">第一个枚举器。</param>
        /// <param name="enum2">第二个枚举器。</param>
        /// <returns>返回一个可用于同步枚举的 <see cref="MulticastEnumerator{TEnum1,TItem1,TEnum2,TItem2}.Enumerator"/> 的封装。</returns>
        public static MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2> Create<TEnum1, TItem1, TEnum2, TItem2>(TEnum1 enum1, TEnum2 enum2)
            where TEnum1 : struct, IStructEnumerator<TItem1>
            where TEnum2 : struct, IStructEnumerator<TItem2>
            => new MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2>(enum1, enum2);

        /// <summary>
        /// 创建三个枚举器的 Multicast 组合器。
        /// </summary>
        /// <remarks>行为类似于两个枚举器的 Create，但针对三个枚举器同步推进并返回三元组。</remarks>
        public static MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3> Create<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3)
            where TEnum1 : struct, IStructEnumerator<TItem1>
            where TEnum2 : struct, IStructEnumerator<TItem2>
            where TEnum3 : struct, IStructEnumerator<TItem3>
            => new MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3>(enum1, enum2, enum3);

        /// <summary>
        /// 创建四个枚举器的 Multicast 组合器。
        /// </summary>
        public static MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4> Create<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4)
            where TEnum1 : struct, IStructEnumerator<TItem1>
            where TEnum2 : struct, IStructEnumerator<TItem2>
            where TEnum3 : struct, IStructEnumerator<TItem3>
            where TEnum4 : struct, IStructEnumerator<TItem4>
            => new MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4>(enum1, enum2, enum3, enum4);

        /// <summary>
        /// 创建五个枚举器的 Multicast 组合器。
        /// </summary>
        public static MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4, TEnum5, TItem5> Create<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4, TEnum5, TItem5>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4, in TEnum5 enum5)
            where TEnum1 : struct, IStructEnumerator<TItem1>
            where TEnum2 : struct, IStructEnumerator<TItem2>
            where TEnum3 : struct, IStructEnumerator<TItem3>
            where TEnum4 : struct, IStructEnumerator<TItem4>
            where TEnum5 : struct, IStructEnumerator<TItem5>
            => new MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4, TEnum5, TItem5>(enum1, enum2, enum3, enum4, enum5);
    }

    /// <summary>
    /// 单个枚举器的组合（等于透传），用于在统一 API 下处理单一枚举器。
    /// </summary>
    public readonly struct MulticastEnumerator<TEnum1, TItem1>
        where TEnum1 : struct, IStructEnumerator<TItem1>
    {
        private readonly TEnum1 _enum1;

        internal MulticastEnumerator(in TEnum1 enum1) => _enum1 = enum1;

        /// <summary>
        /// 获取用于 foreach 或显式驱动的枚举器实例。
        /// </summary>
        /// <returns>返回一个 struct 枚举器实例，支持 MoveNext/Current 操作。</returns>
        public Enumerator GetEnumerator() => new(_enum1);

        /// <summary>
        /// 单枚举器的枚举器实现，直接透传内部枚举器的 MoveNext/Current。
        /// </summary>
        public struct Enumerator : IStructEnumerator<TItem1>
        {
            private readonly TEnum1 _enum1;

            internal Enumerator(in TEnum1 enum1)
            { _enum1 = enum1; }

            /// <summary>
            /// 推进内部枚举器到下一个元素。
            /// </summary>
            public bool MoveNext() => _enum1.MoveNext();

            /// <summary>
            /// 当前项：直接返回内部枚举器的 Current。
            /// </summary>
            public TItem1 Current => _enum1.Current;
        }
    }

    /// <summary>
    /// 两个枚举器的组合（并行推进并返回 (T1,T2) 元组）。
    /// </summary>
    public readonly struct MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2>
        where TEnum1 : struct, IStructEnumerator<TItem1>
        where TEnum2 : struct, IStructEnumerator<TItem2>
    {
        private readonly TEnum1 _enum1;
        private readonly TEnum2 _enum2;

        internal MulticastEnumerator(in TEnum1 enum1, in TEnum2 enum2)
        {
            _enum1 = enum1;
            _enum2 = enum2;
        }

        /// <summary>
        /// 获取组合枚举器用于同步枚举两个子枚举器。
        /// </summary>
        /// <returns>组合枚举器实例。</returns>
        public Enumerator GetEnumerator() => new(_enum1, _enum2);

        /// <summary>
        /// 组合枚举器实现：MoveNext 会同时推进两个子枚举器，只有当两个均能推进时才返回 true。
        /// </summary>
        public struct Enumerator : IStructEnumerator<(TItem1, TItem2)>
        {
            private readonly TEnum1 _enum1;
            private readonly TEnum2 _enum2;

            internal Enumerator(in TEnum1 enum1, in TEnum2 enum2)
            {
                _enum1 = enum1;
                _enum2 = enum2;
            }

            /// <summary>
            /// 同步推进两个子枚举器。
            /// </summary>
            public bool MoveNext() => _enum1.MoveNext() && _enum2.MoveNext();

            /// <summary>
            /// 当前项：返回两个子枚举器的当前项组成的元组。
            /// </summary>
            public (TItem1, TItem2) Current => (_enum1.Current, _enum2.Current);
        }
    }

    /// <summary>
    /// 三个枚举器的组合（并行推进并返回 (T1,T2,T3) 元组）。
    /// </summary>
    public readonly struct MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3>
        where TEnum1 : struct, IStructEnumerator<TItem1>
        where TEnum2 : struct, IStructEnumerator<TItem2>
        where TEnum3 : struct, IStructEnumerator<TItem3>
    {
        private readonly TEnum1 _enum1;
        private readonly TEnum2 _enum2;
        private readonly TEnum3 _enum3;

        internal MulticastEnumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
        }

        /// <summary>
        /// 获取组合枚举器用于同步枚举三个子枚举器。
        /// </summary>
        public Enumerator GetEnumerator() => new(_enum1, _enum2, _enum3);

        /// <summary>
        /// 组合枚举器实现：MoveNext 会同时推进三个子枚举器，只有当全部能推进时才返回 true。
        /// </summary>
        public struct Enumerator : IStructEnumerator<(TItem1, TItem2, TItem3)>
        {
            private readonly TEnum1 _enum1;
            private readonly TEnum2 _enum2;
            private readonly TEnum3 _enum3;

            internal Enumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3)
            {
                _enum1 = enum1;
                _enum2 = enum2;
                _enum3 = enum3;
            }

            /// <summary>
            /// 同步推进三个子枚举器。
            /// </summary>
            public bool MoveNext() => _enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext();

            /// <summary>
            /// 当前项：返回三个子枚举器的当前项组成的元组。
            /// </summary>
            public (TItem1, TItem2, TItem3) Current => (_enum1.Current, _enum2.Current, _enum3.Current);
        }
    }

    /// <summary>
    /// 四个枚举器的组合（并行推进并返回 (T1,T2,T3,T4) 元组）。
    /// </summary>
    public readonly struct MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4>
        where TEnum1 : struct, IStructEnumerator<TItem1>
        where TEnum2 : struct, IStructEnumerator<TItem2>
        where TEnum3 : struct, IStructEnumerator<TItem3>
        where TEnum4 : struct, IStructEnumerator<TItem4>
    {
        private readonly TEnum1 _enum1;
        private readonly TEnum2 _enum2;
        private readonly TEnum3 _enum3;
        private readonly TEnum4 _enum4;

        internal MulticastEnumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
        }

        /// <summary>
        /// 获取组合枚举器用于同步枚举四个子枚举器。
        /// </summary>
        public Enumerator GetEnumerator() => new(_enum1, _enum2, _enum3, _enum4);

        /// <summary>
        /// 组合枚举器实现：MoveNext 会同时推进四个子枚举器，只有当全部能推进时才返回 true。
        /// </summary>
        public struct Enumerator : IStructEnumerator<(TItem1, TItem2, TItem3, TItem4)>
        {
            private readonly TEnum1 _enum1;
            private readonly TEnum2 _enum2;
            private readonly TEnum3 _enum3;
            private readonly TEnum4 _enum4;

            internal Enumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4)
            {
                _enum1 = enum1;
                _enum2 = enum2;
                _enum3 = enum3;
                _enum4 = enum4;
            }

            /// <summary>
            /// 同步推进四个子枚举器。
            /// </summary>
            public bool MoveNext() => _enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext() && _enum4.MoveNext();

            /// <summary>
            /// 当前项：返回四个子枚举器的当前项组成的元组。
            /// </summary>
            public (TItem1, TItem2, TItem3, TItem4) Current => (_enum1.Current, _enum2.Current, _enum3.Current, _enum4.Current);
        }
    }

    /// <summary>
    /// 五个枚举器的组合（并行推进并返回 (T1,T2,T3,T4,T5) 元组）。
    /// </summary>
    public readonly struct MulticastEnumerator<TEnum1, TItem1, TEnum2, TItem2, TEnum3, TItem3, TEnum4, TItem4, TEnum5, TItem5>
        where TEnum1 : struct, IStructEnumerator<TItem1>
        where TEnum2 : struct, IStructEnumerator<TItem2>
        where TEnum3 : struct, IStructEnumerator<TItem3>
        where TEnum4 : struct, IStructEnumerator<TItem4>
        where TEnum5 : struct, IStructEnumerator<TItem5>
    {
        private readonly TEnum1 _enum1;
        private readonly TEnum2 _enum2;
        private readonly TEnum3 _enum3;
        private readonly TEnum4 _enum4;
        private readonly TEnum5 _enum5;

        internal MulticastEnumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4, in TEnum5 enum5)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
            _enum5 = enum5;
        }

        /// <summary>
        /// 获取组合枚举器用于同步枚举五个子枚举器。
        /// </summary>
        public Enumerator GetEnumerator() => new(_enum1, _enum2, _enum3, _enum4, _enum5);

        /// <summary>
        /// 组合枚举器实现：MoveNext 会同时推进五个子枚举器，只有当全部能推进时才返回 true。
        /// </summary>
        public struct Enumerator : IStructEnumerator<(TItem1, TItem2, TItem3, TItem4, TItem5)>
        {
            private readonly TEnum1 _enum1;
            private readonly TEnum2 _enum2;
            private readonly TEnum3 _enum3;
            private readonly TEnum4 _enum4;
            private readonly TEnum5 _enum5;

            internal Enumerator(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4, in TEnum5 enum5)
            {
                _enum1 = enum1;
                _enum2 = enum2;
                _enum3 = enum3;
                _enum4 = enum4;
                _enum5 = enum5;
            }

            /// <summary>
            /// 同步推进五个子枚举器。
            /// </summary>
            public bool MoveNext() => _enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext() && _enum4.MoveNext() && _enum5.MoveNext();

            /// <summary>
            /// 当前项：返回五个子枚举器的当前项组成的元组。
            /// </summary>
            public (TItem1, TItem2, TItem3, TItem4, TItem5) Current => (_enum1.Current, _enum2.Current, _enum3.Current, _enum4.Current, _enum5.Current);
        }
    }
}