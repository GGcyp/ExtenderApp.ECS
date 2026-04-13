using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 单行查询结果：一列实体与一列 T1 组件；foreach 按值解构请用 <see cref="Deconstruct" />，需要 <see cref="RefRW{T}" /> 请用 <see cref="Deconstruct" />。
    /// </summary>
    public readonly struct EntityQueryRow<T1>
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;

        /// <summary>
        /// 由行枚举器构造，绑定当前实体与 T1 列的可写引用。
        /// </summary>
        internal EntityQueryRow(Entity entity, RefRW<T1> item1)
        {
            _entity = entity;
            _item1 = item1;
        }

        /// <summary>
        /// 解构出 T1 列的 <see cref="RefRW{T}" />。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1) => item1 = _item1;

        /// <summary>
        /// 解构出 T1 列的 <see cref="RefRW{T}" /> 与当前实体。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out Entity entity)
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
    /// 双列组件与一列实体的查询行；支持 <c>foreach ((T1, T2) in query)</c> 等元组遍历。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2>
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;

        /// <summary>
        /// 由行枚举器构造，绑定当前实体与各列可写引用。
        /// </summary>
        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" />。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2)
        {
            item1 = _item1;
            item2 = _item2;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" /> 与当前实体。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            entity = _entity;
        }

        public static implicit operator T1(EntityQueryRow<T1, T2> row) => row._item1;

        public static implicit operator T2(EntityQueryRow<T1, T2> row) => row._item2;

        public static implicit operator (T1, T2)(EntityQueryRow<T1, T2> row) => (row._item1, row._item2);

        public static implicit operator RefRW<T1>(EntityQueryRow<T1, T2> row) => row._item1;

        public static implicit operator RefRW<T2>(EntityQueryRow<T1, T2> row) => row._item2;

        public static implicit operator (RefRW<T1> item1, Entity entity)(EntityQueryRow<T1, T2> row) => (row._item1, row._entity);

        public static implicit operator RefRO<T1>(EntityQueryRow<T1, T2> row) => row._item1;

        public static implicit operator RefRO<T2>(EntityQueryRow<T1, T2> row) => row._item2;

        public static implicit operator Entity(EntityQueryRow<T1, T2> row) => row._entity;
    }

    /// <summary>
    /// 三列组件与一列实体的查询行。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3>
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;

        /// <summary>
        /// 由行枚举器构造，绑定当前实体与各列可写引用。
        /// </summary>
        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2, RefRW<T3> item3)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" />。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" /> 与当前实体。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            entity = _entity;
        }

        public static implicit operator T1(EntityQueryRow<T1, T2, T3> row) => row._item1;

        public static implicit operator T2(EntityQueryRow<T1, T2, T3> row) => row._item2;

        public static implicit operator T3(EntityQueryRow<T1, T2, T3> row) => row._item3;

        public static implicit operator (T1, T2, T3)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3);

        public static implicit operator RefRW<T1>(EntityQueryRow<T1, T2, T3> row) => row._item1;

        public static implicit operator RefRW<T2>(EntityQueryRow<T1, T2, T3> row) => row._item2;

        public static implicit operator RefRW<T3>(EntityQueryRow<T1, T2, T3> row) => row._item3;

        public static implicit operator RefRO<T1>(EntityQueryRow<T1, T2, T3> row) => row._item1;

        public static implicit operator RefRO<T2>(EntityQueryRow<T1, T2, T3> row) => row._item2;

        public static implicit operator RefRO<T3>(EntityQueryRow<T1, T2, T3> row) => row._item3;

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3);

        public static implicit operator (T1, T2, T3, Entity)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, Entity)(EntityQueryRow<T1, T2, T3> row) => (row._item1, row._item2, row._item3, row._entity);

        public static implicit operator Entity(EntityQueryRow<T1, T2, T3> row) => row._entity;
    }

    /// <summary>
    /// 四列组件与一列实体的查询行。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4>
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;
        private readonly RefRW<T4> _item4;

        /// <summary>
        /// 由行枚举器构造，绑定当前实体与各列可写引用。
        /// </summary>
        internal EntityQueryRow(Entity entity, RefRW<T1> item1, RefRW<T2> item2, RefRW<T3> item3, RefRW<T4> item4)
        {
            _entity = entity;
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
            _item4 = item4;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" />。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" /> 与当前实体。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            entity = _entity;
        }

        public static implicit operator T1(EntityQueryRow<T1, T2, T3, T4> row) => row._item1;

        public static implicit operator T2(EntityQueryRow<T1, T2, T3, T4> row) => row._item2;

        public static implicit operator T3(EntityQueryRow<T1, T2, T3, T4> row) => row._item3;

        public static implicit operator T4(EntityQueryRow<T1, T2, T3, T4> row) => row._item4;

        public static implicit operator (T1, T2, T3, T4)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4);

        public static implicit operator RefRW<T1>(EntityQueryRow<T1, T2, T3, T4> row) => row._item1;

        public static implicit operator RefRW<T2>(EntityQueryRow<T1, T2, T3, T4> row) => row._item2;

        public static implicit operator RefRW<T3>(EntityQueryRow<T1, T2, T3, T4> row) => row._item3;

        public static implicit operator RefRW<T4>(EntityQueryRow<T1, T2, T3, T4> row) => row._item4;

        public static implicit operator RefRO<T1>(EntityQueryRow<T1, T2, T3, T4> row) => row._item1;

        public static implicit operator RefRO<T2>(EntityQueryRow<T1, T2, T3, T4> row) => row._item2;

        public static implicit operator RefRO<T3>(EntityQueryRow<T1, T2, T3, T4> row) => row._item3;

        public static implicit operator RefRO<T4>(EntityQueryRow<T1, T2, T3, T4> row) => row._item4;

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4);

        public static implicit operator (T1, T2, T3, T4, Entity)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, Entity)(EntityQueryRow<T1, T2, T3, T4> row) => (row._item1, row._item2, row._item3, row._item4, row._entity);

        public static implicit operator Entity(EntityQueryRow<T1, T2, T3, T4> row) => row._entity;
    }

    /// <summary>
    /// 五列组件与一列实体的查询行。
    /// </summary>
    public readonly struct EntityQueryRow<T1, T2, T3, T4, T5>
    {
        private readonly Entity _entity;
        private readonly RefRW<T1> _item1;
        private readonly RefRW<T2> _item2;
        private readonly RefRW<T3> _item3;
        private readonly RefRW<T4> _item4;
        private readonly RefRW<T5> _item5;

        /// <summary>
        /// 由行枚举器构造，绑定当前实体与各列可写引用。
        /// </summary>
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
        /// 解构出各列的 <see cref="RefRW{T}" />。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out RefRW<T5> item5)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            item5 = _item5;
        }

        /// <summary>
        /// 解构出各列的 <see cref="RefRW{T}" /> 与当前实体。
        /// </summary>
        public readonly void Deconstruct(out RefRW<T1> item1, out RefRW<T2> item2, out RefRW<T3> item3, out RefRW<T4> item4, out RefRW<T5> item5, out Entity entity)
        {
            item1 = _item1;
            item2 = _item2;
            item3 = _item3;
            item4 = _item4;
            item5 = _item5;
            entity = _entity;
        }

        public static implicit operator T1(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item1;

        public static implicit operator T2(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item2;

        public static implicit operator T3(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item3;

        public static implicit operator T4(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item4;

        public static implicit operator T5(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item5;

        public static implicit operator (T1, T2, T3, T4, T5)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5);

        public static implicit operator RefRW<T1>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item1;

        public static implicit operator RefRW<T2>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item2;

        public static implicit operator RefRW<T3>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item3;

        public static implicit operator RefRW<T4>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item4;

        public static implicit operator RefRW<T5>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item5;

        public static implicit operator RefRO<T1>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item1;

        public static implicit operator RefRO<T2>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item2;

        public static implicit operator RefRO<T3>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item3;

        public static implicit operator RefRO<T4>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item4;

        public static implicit operator RefRO<T5>(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._item5;

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, RefRW<T5>)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5);

        public static implicit operator (T1, T2, T3, T4, T5, Entity)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5, row._entity);

        public static implicit operator (RefRW<T1>, RefRW<T2>, RefRW<T3>, RefRW<T4>, RefRW<T5>, Entity)(EntityQueryRow<T1, T2, T3, T4, T5> row) => (row._item1, row._item2, row._item3, row._item4, row._item5, row._entity);

        public static implicit operator Entity(EntityQueryRow<T1, T2, T3, T4, T5> row) => row._entity;
    }
}