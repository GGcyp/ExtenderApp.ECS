using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 查询结果行，包含一个组件。
    /// </summary>
    public readonly struct EntityQueryRow<T1>
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
    /// 查询结果行，包含两个组件。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2>
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
    /// 查询结果行，包含三个组件。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3>
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
    /// 查询结果行，包含四个组件。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4>
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
    /// 查询结果行，包含五个组件。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4, T5>
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
}