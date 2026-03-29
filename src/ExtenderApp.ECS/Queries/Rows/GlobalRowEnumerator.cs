namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    public struct GlobalRowEnumerator
    {
        private GlobalQueryEntityEnumerator entityEnumerator;

        public Entity Current => entityEnumerator.Current;

        internal GlobalRowEnumerator(GlobalQueryEntityEnumerator entityEnumerator) => this.entityEnumerator = entityEnumerator;

        public bool MoveNext() => entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    public struct GlobalRowEnumerator<T1>
    {
        private GlobalQueryEnumerator<T1> enum1;
        private GlobalQueryEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1> Current => new(entityEnumerator.Current, enum1.Current);

        internal GlobalRowEnumerator(GlobalQueryEnumerator<T1> enum1, GlobalQueryEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    public struct GlobalRowEnumerator<T1, T2>
    {
        private GlobalQueryEnumerator<T1> enum1;
        private GlobalQueryEnumerator<T2> enum2;
        private GlobalQueryEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current);

        internal GlobalRowEnumerator(GlobalQueryEnumerator<T1> enum1, GlobalQueryEnumerator<T2> enum2, GlobalQueryEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    public struct GlobalRowEnumerator<T1, T2, T3>
    {
        private GlobalQueryEnumerator<T1> enum1;
        private GlobalQueryEnumerator<T2> enum2;
        private GlobalQueryEnumerator<T3> enum3;
        private GlobalQueryEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current);

        internal GlobalRowEnumerator(GlobalQueryEnumerator<T1> enum1, GlobalQueryEnumerator<T2> enum2, GlobalQueryEnumerator<T3> enum3, GlobalQueryEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    public struct GlobalRowEnumerator<T1, T2, T3, T4>
    {
        private GlobalQueryEnumerator<T1> enum1;
        private GlobalQueryEnumerator<T2> enum2;
        private GlobalQueryEnumerator<T3> enum3;
        private GlobalQueryEnumerator<T4> enum4;
        private GlobalQueryEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3, T4> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current);

        internal GlobalRowEnumerator(GlobalQueryEnumerator<T1> enum1, GlobalQueryEnumerator<T2> enum2, GlobalQueryEnumerator<T3> enum3, GlobalQueryEnumerator<T4> enum4, GlobalQueryEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && entityEnumerator.MoveNext();
    }

    /// <summary>
    /// 全局行枚举器，用于在全局查询中枚举实体和组件数据。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    /// <typeparam name="T5">第五个组件类型。</typeparam>
    public struct GlobalRowEnumerator<T1, T2, T3, T4, T5>
    {
        private GlobalQueryEnumerator<T1> enum1;
        private GlobalQueryEnumerator<T2> enum2;
        private GlobalQueryEnumerator<T3> enum3;
        private GlobalQueryEnumerator<T4> enum4;
        private GlobalQueryEnumerator<T5> enum5;
        private GlobalQueryEntityEnumerator entityEnumerator;

        public EntityQueryRow<T1, T2, T3, T4, T5> Current => new(entityEnumerator.Current, enum1.Current, enum2.Current, enum3.Current, enum4.Current, enum5.Current);

        internal GlobalRowEnumerator(GlobalQueryEnumerator<T1> enum1, GlobalQueryEnumerator<T2> enum2, GlobalQueryEnumerator<T3> enum3, GlobalQueryEnumerator<T4> enum4, GlobalQueryEnumerator<T5> enum5, GlobalQueryEntityEnumerator entityEnumerator)
        {
            this.enum1 = enum1;
            this.enum2 = enum2;
            this.enum3 = enum3;
            this.enum4 = enum4;
            this.enum5 = enum5;
            this.entityEnumerator = entityEnumerator;
        }

        public bool MoveNext() => enum1.MoveNext() && enum2.MoveNext() && enum3.MoveNext() && enum4.MoveNext() && enum5.MoveNext() && entityEnumerator.MoveNext();
    }
}