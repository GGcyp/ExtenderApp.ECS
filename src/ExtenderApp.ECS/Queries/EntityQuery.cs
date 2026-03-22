using System.Buffers;
using System.Runtime.CompilerServices;
using ExtenderApp.Contracts;
using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Threading;
using ExtenderApp.ECS.Worlds;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 单组件实体查询包装。
    /// 负责基于 <see cref="QueryCore" /> 创建指定组件类型的访问器与枚举器。
    /// </summary>
    public readonly struct EntityQuery<T> where T : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T> SkipUnchanged(bool skip = true) => new(_core, skip);

        private EntityQueryAccessor<T> GetAccessor() => _core.CreateAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 获取按值输出的枚举器，可直接用于 foreach。
        /// </summary>
        public EntityQueryAccessor<T>.Enumerator GetEnumerator() => GetValues();

        /// <summary>
        /// 获取按组件值遍历的结构体枚举器。
        /// </summary>
        public EntityQueryAccessor<T>.Enumerator GetValues() => GetAccessor().GetEnumerator();

        /// <summary>
        /// 获取按块输出的组件访问器枚举器。
        /// </summary>
        public EntityQueryAccessor<T>.ComponentAccessorEnumerator GetComponentAccessors() => GetAccessor().GetComponentAccessorEnumerator();

        public EntityQueryAccessor<T>.RefROEnumerator GetRefROs() => GetAccessor().GetRefROs();
        public EntityQueryAccessor<T>.RefRWEnumerator GetRefRWs() => GetAccessor().GetRefRWs();

        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }
    }

    /// <summary>
    /// 双组件实体查询包装。
    /// 无参 <see cref="GetEnumerator" /> 直接返回可 foreach 的值元组枚举器。
    /// </summary>
    public readonly struct EntityQuery<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;

        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2> SkipUnchanged(bool skip = true) => new(_core, skip);

        private EntityQueryAccessor<T1> GetAccessorForT1() => _core.CreateAccessor<T1>(_skipUnchanged);
        private EntityQueryAccessor<T2> GetAccessorForT2() => _core.CreateAccessor<T2>(_skipUnchanged);

        public MulticastEnumerator<EntityQueryAccessor<T1>.Enumerator, T1, EntityQueryAccessor<T2>.Enumerator, T2>.Enumerator GetEnumerator()
            => new EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator>(GetEnumeratorForT1(), GetEnumeratorForT2()).GetEnumerator();

        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();

        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public struct EntityQueryEnumerator<TEnum1, TEnum2>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;

            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
            }

            public MulticastEnumerator<TEnum1, T1, TEnum2, T2>.Enumerator GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2>(enum1, enum2).GetEnumerator();
        }
    }

    /// <summary>
    /// 三组件实体查询包装。
    /// 无参 <see cref="GetEnumerator" /> 直接返回可 foreach 的值元组枚举器。
    /// </summary>
    public readonly struct EntityQuery<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3> SkipUnchanged(bool skip = true) => new(_core, skip);

        private EntityQueryAccessor<T1> GetAccessorForT1() => _core.CreateAccessor<T1>(_skipUnchanged);
        private EntityQueryAccessor<T2> GetAccessorForT2() => _core.CreateAccessor<T2>(_skipUnchanged);
        private EntityQueryAccessor<T3> GetAccessorForT3() => _core.CreateAccessor<T3>(_skipUnchanged);

        public MulticastEnumerator<EntityQueryAccessor<T1>.Enumerator, T1, EntityQueryAccessor<T2>.Enumerator, T2, EntityQueryAccessor<T3>.Enumerator, T3>.Enumerator GetEnumerator()
            => new EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator>(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3()).GetEnumerator();

        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3> GetEnumerator<TEnum1, TEnum2, TEnum3>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            => new(enum1, enum2, enum3);

        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();

        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;

            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
            }

            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3>.Enumerator GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3>(enum1, enum2, enum3).GetEnumerator();
        }
    }

    /// <summary>
    /// 四组件实体查询包装。
    /// 无参 <see cref="GetEnumerator" /> 直接返回可 foreach 的值元组枚举器。
    /// </summary>
    public readonly struct EntityQuery<T1, T2, T3, T4>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4> SkipUnchanged(bool skip = true) => new(_core, skip);

        private EntityQueryAccessor<T1> GetAccessorForT1() => _core.CreateAccessor<T1>(_skipUnchanged);
        private EntityQueryAccessor<T2> GetAccessorForT2() => _core.CreateAccessor<T2>(_skipUnchanged);
        private EntityQueryAccessor<T3> GetAccessorForT3() => _core.CreateAccessor<T3>(_skipUnchanged);
        private EntityQueryAccessor<T4> GetAccessorForT4() => _core.CreateAccessor<T4>(_skipUnchanged);

        public MulticastEnumerator<EntityQueryAccessor<T1>.Enumerator, T1, EntityQueryAccessor<T2>.Enumerator, T2, EntityQueryAccessor<T3>.Enumerator, T3, EntityQueryAccessor<T4>.Enumerator, T4>.Enumerator GetEnumerator()
            => new EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator, EntityQueryAccessor<T4>.Enumerator>(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3(), GetEnumeratorForT4()).GetEnumerator();

        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4> GetEnumerator<TEnum1, TEnum2, TEnum3, TEnum4>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            => new(enum1, enum2, enum3, enum4);

        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();
        public EntityQueryAccessor<T4>.Enumerator GetEnumeratorForT4() => GetAccessorForT4().GetEnumerator();
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();
        public EntityQueryAccessor<T4>.RefROEnumerator GetRefROsForT4() => GetAccessorForT4().GetRefROs();
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();
        public EntityQueryAccessor<T4>.RefRWEnumerator GetRefRWsForT4() => GetAccessorForT4().GetRefRWs();

        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;
            private TEnum4 enum4;

            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3, TEnum4 enum4)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
                this.enum4 = enum4;
            }

            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4>.Enumerator GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4>(enum1, enum2, enum3, enum4).GetEnumerator();
        }
    }

    /// <summary>
    /// 五组件实体查询包装。
    /// 无参 <see cref="GetEnumerator" /> 直接返回可 foreach 的值元组枚举器。
    /// </summary>
    public readonly struct EntityQuery<T1, T2, T3, T4, T5>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4, T5> SkipUnchanged(bool skip = true) => new(_core, skip);

        private EntityQueryAccessor<T1> GetAccessorForT1() => _core.CreateAccessor<T1>(_skipUnchanged);
        private EntityQueryAccessor<T2> GetAccessorForT2() => _core.CreateAccessor<T2>(_skipUnchanged);
        private EntityQueryAccessor<T3> GetAccessorForT3() => _core.CreateAccessor<T3>(_skipUnchanged);
        private EntityQueryAccessor<T4> GetAccessorForT4() => _core.CreateAccessor<T4>(_skipUnchanged);
        private EntityQueryAccessor<T5> GetAccessorForT5() => _core.CreateAccessor<T5>(_skipUnchanged);

        public MulticastEnumerator<EntityQueryAccessor<T1>.Enumerator, T1, EntityQueryAccessor<T2>.Enumerator, T2, EntityQueryAccessor<T3>.Enumerator, T3, EntityQueryAccessor<T4>.Enumerator, T4, EntityQueryAccessor<T5>.Enumerator, T5>.Enumerator GetEnumerator()
            => new EntityQueryEnumerator<EntityQueryAccessor<T1>.Enumerator, EntityQueryAccessor<T2>.Enumerator, EntityQueryAccessor<T3>.Enumerator, EntityQueryAccessor<T4>.Enumerator, EntityQueryAccessor<T5>.Enumerator>(GetEnumeratorForT1(), GetEnumeratorForT2(), GetEnumeratorForT3(), GetEnumeratorForT4(), GetEnumeratorForT5()).GetEnumerator();

        public EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5> GetEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5>(in TEnum1 enum1, in TEnum2 enum2, in TEnum3 enum3, in TEnum4 enum4, in TEnum5 enum5)
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            where TEnum5 : struct, IStructEnumerator<T5>
            => new(enum1, enum2, enum3, enum4, enum5);

        public EntityQueryAccessor<T1>.Enumerator GetEnumeratorForT1() => GetAccessorForT1().GetEnumerator();
        public EntityQueryAccessor<T2>.Enumerator GetEnumeratorForT2() => GetAccessorForT2().GetEnumerator();
        public EntityQueryAccessor<T3>.Enumerator GetEnumeratorForT3() => GetAccessorForT3().GetEnumerator();
        public EntityQueryAccessor<T4>.Enumerator GetEnumeratorForT4() => GetAccessorForT4().GetEnumerator();
        public EntityQueryAccessor<T5>.Enumerator GetEnumeratorForT5() => GetAccessorForT5().GetEnumerator();
        public EntityQueryAccessor<T1>.RefROEnumerator GetRefROsForT1() => GetAccessorForT1().GetRefROs();
        public EntityQueryAccessor<T2>.RefROEnumerator GetRefROsForT2() => GetAccessorForT2().GetRefROs();
        public EntityQueryAccessor<T3>.RefROEnumerator GetRefROsForT3() => GetAccessorForT3().GetRefROs();
        public EntityQueryAccessor<T4>.RefROEnumerator GetRefROsForT4() => GetAccessorForT4().GetRefROs();
        public EntityQueryAccessor<T5>.RefROEnumerator GetRefROsForT5() => GetAccessorForT5().GetRefROs();
        public EntityQueryAccessor<T1>.RefRWEnumerator GetRefRWsForT1() => GetAccessorForT1().GetRefRWs();
        public EntityQueryAccessor<T2>.RefRWEnumerator GetRefRWsForT2() => GetAccessorForT2().GetRefRWs();
        public EntityQueryAccessor<T3>.RefRWEnumerator GetRefRWsForT3() => GetAccessorForT3().GetRefRWs();
        public EntityQueryAccessor<T4>.RefRWEnumerator GetRefRWsForT4() => GetAccessorForT4().GetRefRWs();
        public EntityQueryAccessor<T5>.RefRWEnumerator GetRefRWsForT5() => GetAccessorForT5().GetRefRWs();

        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public struct EntityQueryEnumerator<TEnum1, TEnum2, TEnum3, TEnum4, TEnum5>
            where TEnum1 : struct, IStructEnumerator<T1>
            where TEnum2 : struct, IStructEnumerator<T2>
            where TEnum3 : struct, IStructEnumerator<T3>
            where TEnum4 : struct, IStructEnumerator<T4>
            where TEnum5 : struct, IStructEnumerator<T5>
        {
            private TEnum1 enum1;
            private TEnum2 enum2;
            private TEnum3 enum3;
            private TEnum4 enum4;
            private TEnum5 enum5;

            public EntityQueryEnumerator(TEnum1 enum1, TEnum2 enum2, TEnum3 enum3, TEnum4 enum4, TEnum5 enum5)
            {
                this.enum1 = enum1;
                this.enum2 = enum2;
                this.enum3 = enum3;
                this.enum4 = enum4;
                this.enum5 = enum5;
            }

            public MulticastEnumerator<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4, TEnum5, T5>.Enumerator GetEnumerator()
                => MulticastEnumerator.Create<TEnum1, T1, TEnum2, T2, TEnum3, T3, TEnum4, T4, TEnum5, T5>(enum1, enum2, enum3, enum4, enum5).GetEnumerator();
        }
    }
}