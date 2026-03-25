using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 查询行：单组件行包装（可用于解构赋值）。
    /// 包含实体句柄与该行对应的组件可写引用（RefRW&lt;T1&gt;）。
    /// </summary>
    /// <typeparam name="T1">组件类型。</typeparam>
    public readonly struct EntityQueryRow<T1>
        where T1 : struct
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;

        internal EntityQueryRow(Entity entity, RefRW<T1> item1)
        {
            _entity = entity;
            _item1 = item1;
        }

        /// <summary>
        /// 解构到单个组件引用（只包含组件，不包含实体）。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1)
        {
            item1 = _item1;
        }

        /// <summary>
        /// 解构到组件引用与实体句柄。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out Entity entity)
        {
            item1 = _item1;
            entity = _entity;
        }

        public static implicit operator T1(EntityQueryRow<T1> row) => row._item1;

        public static implicit operator RefRW<T1>(EntityQueryRow<T1> row) => row._item1;

        public static implicit operator (RefRW<T1> item1, Entity entity)(EntityQueryRow<T1> row) => (row._item1, row._entity);

        public static implicit operator RefRO<T1>(EntityQueryRow<T1> row) => row._item1;

        public static implicit operator Entity(EntityQueryRow<T1> row) => row._entity;
    }

    /// <summary>
    /// 查询行：双组件行包装（可用于解构赋值）。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;

        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
        }

        /// <summary>
        /// 解构到两个组件引用（不包含实体）。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2)
        {
            item1 = _item1;
            item2 = _item2;
        }

        /// <summary>
        /// 解构到两个组件引用与实体句柄。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            entity = _entity;
        }

        public static implicit operator (T1, T2)(EntityQueryRow<T1, T2> row) => (row._item1, row._item2);

        public static implicit operator RefRW<T1>(EntityQueryRow<T1, T2> row) => row._item1;

        public static implicit operator (RefRW<T1> item1, Entity entity)(EntityQueryRow<T1, T2> row) => (row._item1, row._entity);

        public static implicit operator RefRO<T1>(EntityQueryRow<T1, T2> row) => row._item1;

        public static implicit operator Entity(EntityQueryRow<T1, T2> row) => row._entity;
    }

    /// <summary>
    /// 查询行：三组件行包装（可用于解构赋值）。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;

        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2, RefRW<T3> item3)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
        }

        /// <summary>
        /// 解构到三个组件引用（不包含实体）。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
        }

        /// <summary>
        /// 解构到三个组件引用与实体句柄。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            entity = _entity;
        }

        public static implicit operator (T1, T2, T3)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3);

        public static implicit operator (T1, T2, T3, Entity)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, Entity)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3, row._entity);
    }

    /// <summary>
    /// 查询行：四组件行包装（可用于解构赋值）。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;
        private readonly RefRW<T4> _item4;

        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2, RefRW<T3> item3, RefRW<T4> item4)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
            _item4 = item4;
        }

        /// <summary>
        /// 解构到四个组件引用（不包含实体）。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
        }

        /// <summary>
        /// 解构到四个组件引用与实体句柄。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            entity = _entity;
        }

        public static implicit operator (T1, T2, T3, T4)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4);

        public static implicit operator (T1, T2, T3, T4, Entity)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, Entity)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4, row._entity);
    }

    /// <summary>
    /// 查询行：五组件行包装（可用于解构赋值）。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4, T5>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;
        private readonly RefRW<T4> _item4;
        private readonly RefRW<T5> _item5;

        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2, RefRW<T3> item3, RefRW<T4> item4, RefRW<T5> item5)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
            _item4 = item4;
            _item5 = item5;
        }

        /// <summary>
        /// 解构到五个组件引用（不包含实体）。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out RefRW<T5> item5)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            item5 = _item5;
        }

        /// <summary>
        /// 解构到五个组件引用与实体句柄。
        /// </summary>
        public void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out RefRW<T5> item5, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            item5 = _item5;
            entity = _entity;
        }

        public static implicit operator (T1, T2, T3, T4, T5)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, RefRW<T5>)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5);

        public static implicit operator (T1, T2, T3, T4, T5, Entity)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, RefRW<T5>, Entity)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5, row._entity);
    }

    /// <summary>
    /// 行枚举器：单组件查询行的结构体枚举器，用于 foreach 语法。
    /// 枚举时同时推进组件枚举器与实体枚举器，并在对齐时返回组合行。
    /// </summary>
    public struct EntityQueryRowEnumerator
    {
        /// <summary>
        /// 实体访问器的结构体枚举器：负责按原型段顺序枚举实体句柄。
        /// </summary>
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        /// <summary>
        /// 使用已准备好的实体枚举器初始化行枚举器。
        /// 该构造仅供框架内部使用，调用方通过 Query API 获得行枚举器实例。
        /// </summary>
        /// <param name="entityEnumerator">用于驱动实体行的枚举器。</param>
        internal EntityQueryRowEnumerator(EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前枚举到的实体行（在 MoveNext 返回 true 后有效）。
        /// 对于此非泛型行枚举器，Current 为 Entity（仅包含实体句柄）。
        /// </summary>
        public Entity Current { get; private set; }

        /// <summary>
        /// 将枚举器推进到下一个实体行。 若存在下一个实体则返回 true，并将 Current 更新为该实体；
        /// 否则返回 false 表示枚举结束。
        /// </summary>
        /// <returns>存在下一个实体返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            if (_entityEnumerator.MoveNext())
            {
                Current = _entityEnumerator.Current;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 行枚举器：单组件查询行的结构体枚举器，用于 foreach 语法。
    /// 枚举时同时推进组件枚举器与实体枚举器，并在对齐时返回组合行。
    /// </summary>
    public struct EntityQueryRowEnumerator<T1>
        where T1 : struct
    {
        private ArchetypeAccessor<T1>.RefRWEnumerator _enum1;
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        internal EntityQueryRowEnumerator(ArchetypeAccessor<T1>.RefRWEnumerator enum1, EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _enum1 = enum1;
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前行（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public EntityQueryRow<T1> Current { get; private set; }

        /// <summary>
        /// 将枚举推进到下一行。 当所有相关枚举器同时能够前进时返回 true，并生成对应的行对象。
        /// </summary>
        public bool MoveNext()
        {
            if (_enum1.MoveNext() && _entityEnumerator.MoveNext())
            {
                Current = new(_entityEnumerator.Current, _enum1.Current);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 行枚举器：双组件查询行的结构体枚举器。
    /// </summary>
    public struct EntityQueryRowEnumerator<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private ArchetypeAccessor<T1>.RefRWEnumerator _enum1;
        private ArchetypeAccessor<T2>.RefRWEnumerator _enum2;
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        internal EntityQueryRowEnumerator(ArchetypeAccessor<T1>.RefRWEnumerator enum1, ArchetypeAccessor<T2>.RefRWEnumerator enum2, EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前行（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public EntityQueryRow<T1, T2> Current { get; private set; }

        /// <summary>
        /// 将枚举推进到下一行。 只有当所有组件枚举器与实体枚举器均能前进时才返回 true。
        /// </summary>
        public bool MoveNext()
        {
            if (_enum1.MoveNext() && _enum2.MoveNext() && _entityEnumerator.MoveNext())
            {
                Current = new(_entityEnumerator.Current, _enum1.Current, _enum2.Current);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 行枚举器：三组件查询行的结构体枚举器。
    /// </summary>
    public struct EntityQueryRowEnumerator<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private ArchetypeAccessor<T1>.RefRWEnumerator _enum1;
        private ArchetypeAccessor<T2>.RefRWEnumerator _enum2;
        private ArchetypeAccessor<T3>.RefRWEnumerator _enum3;
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        internal EntityQueryRowEnumerator(ArchetypeAccessor<T1>.RefRWEnumerator enum1, ArchetypeAccessor<T2>.RefRWEnumerator enum2, ArchetypeAccessor<T3>.RefRWEnumerator enum3, EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前行（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public EntityQueryRow<T1, T2, T3> Current { get; private set; }

        /// <summary>
        /// 将枚举推进到下一行。 只有当所有组件枚举器与实体枚举器均能前进时才返回 true。
        /// </summary>
        public bool MoveNext()
        {
            if (_enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext() && _entityEnumerator.MoveNext())
            {
                Current = new(_entityEnumerator.Current, _enum1.Current, _enum2.Current, _enum3.Current);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 行枚举器：四组件查询行的结构体枚举器。
    /// </summary>
    public struct EntityQueryRowEnumerator<T1, T2, T3, T4>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        private ArchetypeAccessor<T1>.RefRWEnumerator _enum1;
        private ArchetypeAccessor<T2>.RefRWEnumerator _enum2;
        private ArchetypeAccessor<T3>.RefRWEnumerator _enum3;
        private ArchetypeAccessor<T4>.RefRWEnumerator _enum4;
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        internal EntityQueryRowEnumerator(ArchetypeAccessor<T1>.RefRWEnumerator enum1, ArchetypeAccessor<T2>.RefRWEnumerator enum2, ArchetypeAccessor<T3>.RefRWEnumerator enum3, ArchetypeAccessor<T4>.RefRWEnumerator enum4, EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前行（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public EntityQueryRow<T1, T2, T3, T4> Current { get; private set; }

        /// <summary>
        /// 将枚举推进到下一行。 只有当所有组件枚举器与实体枚举器均能前进时才返回 true。
        /// </summary>
        public bool MoveNext()
        {
            if (_enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext() && _enum4.MoveNext() && _entityEnumerator.MoveNext())
            {
                Current = new(_entityEnumerator.Current, _enum1.Current, _enum2.Current, _enum3.Current, _enum4.Current);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 行枚举器：五组件查询行的结构体枚举器。
    /// </summary>
    public struct EntityQueryRowEnumerator<T1, T2, T3, T4, T5>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        private ArchetypeAccessor<T1>.RefRWEnumerator _enum1;
        private ArchetypeAccessor<T2>.RefRWEnumerator _enum2;
        private ArchetypeAccessor<T3>.RefRWEnumerator _enum3;
        private ArchetypeAccessor<T4>.RefRWEnumerator _enum4;
        private ArchetypeAccessor<T5>.RefRWEnumerator _enum5;
        private EntityAccessor.EntityEnumerator _entityEnumerator;

        internal EntityQueryRowEnumerator(ArchetypeAccessor<T1>.RefRWEnumerator enum1, ArchetypeAccessor<T2>.RefRWEnumerator enum2, ArchetypeAccessor<T3>.RefRWEnumerator enum3, ArchetypeAccessor<T4>.RefRWEnumerator enum4, ArchetypeAccessor<T5>.RefRWEnumerator enum5, EntityAccessor.EntityEnumerator entityEnumerator)
        {
            _enum1 = enum1;
            _enum2 = enum2;
            _enum3 = enum3;
            _enum4 = enum4;
            _enum5 = enum5;
            _entityEnumerator = entityEnumerator;
            Current = default;
        }

        /// <summary>
        /// 当前行（在 MoveNext 返回 true 后有效）。
        /// </summary>
        public EntityQueryRow<T1, T2, T3, T4, T5> Current { get; private set; }

        /// <summary>
        /// 将枚举推进到下一行。 只有当所有组件枚举器与实体枚举器均能前进时才返回 true。
        /// </summary>
        public bool MoveNext()
        {
            if (_enum1.MoveNext() && _enum2.MoveNext() && _enum3.MoveNext() && _enum4.MoveNext() && _enum5.MoveNext() && _entityEnumerator.MoveNext())
            {
                Current = new(_entityEnumerator.Current, _enum1.Current, _enum2.Current, _enum3.Current, _enum4.Current, _enum5.Current);
                return true;
            }

            return false;
        }
    }
}