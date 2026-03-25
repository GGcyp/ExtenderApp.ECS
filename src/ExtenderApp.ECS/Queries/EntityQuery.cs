using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// 实体查询（只返回实体句柄）的轻量包装。
    ///
    /// 用途：
    /// - 适用于仅需要遍历实体句柄的场景（不访问任何组件数据）；
    /// - 提供 foreach 语法的枚举支持（返回 Entity），以及通过委托对每个实体执行操作的能力。
    ///
    /// 语义与线程：该查询基于内部的 <see cref="QueryCore"/> 构建，枚举/回调应在主线程上执行以保证数据一致性。
    /// </summary>
    public readonly struct EntityQuery
    {
        /// <summary>
        /// 获取当前查询的核心数据结构，包含查询条件、匹配的 Archetype/Chunk 信息等。该属性提供对查询核心的只读访问，供内部使用和调试参考。
        /// </summary>
        internal QueryCore Core { get; }

        /// <summary>
        /// 获取当前查询的描述信息，包含查询条件、匹配的 Archetype/Chunk 信息等。该属性提供对查询核心描述的只读访问，供内部使用和调试参考。
        /// </summary>
        internal ref readonly EntityQueryDesc QueryDesc => ref Core.QueryDesc;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => Core == null;

        internal EntityQuery(QueryCore core)
        {
            Core = core;
        }

        /// <summary>
        /// 获取按行输出的查询行枚举器（用于 foreach）。
        /// 返回的枚举器会按 Archetype/Chunk 顺序遍历所有匹配实体，并在每行中只包含实体句柄（Entity）。
        /// </summary>
        public EntityQueryRowEnumerator GetEnumerator() => new(Core.GetEntityEnumerator());

        /// <summary>
        /// 使用指定的委托对查询中的每一行执行操作。
        /// 委托签名应为 <c>void (Entity)</c>；调用时会通过 <see cref="EntityQueryDelegateInvoker"/> 生成并缓存的调用器执行，避免反射开销。
        /// </summary>
        /// <typeparam name="TDelegate">要执行的委托类型，必须返回 void 且接收单个 Entity 参数。</typeparam>
        /// <param name="delegate">要执行的委托实例。</param>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }
    }

    /// <summary>
    /// 单组件实体查询包装。
    /// 负责基于 <see cref="QueryCore" /> 创建指定组件类型的访问器与枚举器。
    /// 可通过 <see cref="GetEnumerator"/> 与 foreach 语法遍历查询结果行，或使用 <see cref="Query{TDelegate}(TDelegate)"/> 传入委托进行批量处理。
    /// </summary>
    /// <typeparam name="T1">查询的组件类型。</typeparam>
    public readonly struct EntityQuery<T1> where T1 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        /// <summary>
        /// 当前查询结果中匹配的实体数量。该值由内部的 <see cref="QueryCore"/> 维护，反映当前查询条件下的实体总数。
        /// </summary>
        public int Count => _core.Count;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => _core == null;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 返回一个新的查询副本并设置是否在遍历时跳过未变化的块。
        /// 用于在对性能敏感的场景中排除未发生变更的数据块。
        /// </summary>
        /// <param name="skip">是否跳过未变化块，默认 true。</param>
        /// <returns>返回配置后的查询对象。</returns>
        public EntityQuery<T1> SkipUnchanged(bool skip = true) => new(_core, skip);

        /// <summary>
        /// 获取按行输出的查询行枚举器（用于 foreach）。
        /// 返回的枚举器会按 Archetype/Chunk 顺序遍历所有匹配实体，并在每行中包含组件访问器与实体信息。
        /// </summary>
        public EntityQueryRowEnumerator<T1> GetEnumerator()
            => new(GetRefRWsFor<T1>(), _core.GetEntityEnumerator());

        /// <summary>
        /// 内部辅助：根据类型 T1 获取对应的 ArchetypeAccessor。
        /// </summary>
        private ArchetypeAccessor<T> GetAccessorFor<T>() where T : struct => _core.GetAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 内部辅助：获取指定类型的 RefRW 枚举器，用于按行访问可写引用包装（RefRW&lt;T1&gt;）。
        /// 供委托调用器通过统一泛型方法反射调用。
        /// </summary>
        internal ArchetypeAccessor<T>.RefRWEnumerator GetRefRWsFor<T>() where T : struct => GetAccessorFor<T>().GetRefRWs();

        /// <summary>
        /// 使用指定的委托对查询中的每一行执行操作。
        /// 委托会被通过 <see cref="EntityQueryDelegateInvoker"/> 生成并缓存的调用器执行，避免反射开销。
        /// </summary>
        /// <typeparam name="TDelegate">要执行的委托类型，必须返回 void。</typeparam>
        /// <param name="delegate">要执行的委托实例。</param>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public static implicit operator EntityQuery(EntityQuery<T1> query) => new(query._core);
    }

    /// <summary>
    /// 双组件实体查询包装。
    /// 提供按行枚举与委托执行的能力，支持链式设置是否跳过未变化块。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    public readonly struct EntityQuery<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        /// <summary>
        /// 当前查询结果中匹配的实体数量。该值由内部的 <see cref="QueryCore"/> 维护，反映当前查询条件下的实体总数。
        /// </summary>
        public int Count => _core.Count;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => _core == null;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 返回一个新的查询副本并设置是否在遍历时跳过未变化的块。
        /// </summary>
        public EntityQuery<T1, T2> SkipUnchanged(bool skip = true) => new(_core, skip);

        /// <summary>
        /// 获取用于 foreach 的行枚举器，枚举每一行中的两个组件访问器及实体信息。
        /// </summary>
        public EntityQueryRowEnumerator<T1, T2> GetEnumerator()
            => new(GetRefRWsFor<T1>(), GetRefRWsFor<T2>(), _core.GetEntityEnumerator());

        /// <summary>
        /// 内部辅助：根据类型 T1 获取对应的 ArchetypeAccessor。
        /// </summary>
        private ArchetypeAccessor<T> GetAccessorFor<T>() where T : struct => _core.GetAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 内部辅助：获取指定类型的 RefRW 枚举器，用于按行访问可写引用包装（RefRW&lt;T1&gt;）。
        /// 供委托调用器通过统一泛型方法反射调用。
        /// </summary>
        internal ArchetypeAccessor<T>.RefRWEnumerator GetRefRWsFor<T>() where T : struct => GetAccessorFor<T>().GetRefRWs();

        /// <summary>
        /// 使用指定的委托对查询结果进行处理。
        /// </summary>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public static implicit operator EntityQuery(EntityQuery<T1, T2> query) => new(query._core);
    }

    /// <summary>
    /// 三组件实体查询包装。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    public readonly struct EntityQuery<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        /// <summary>
        /// 当前查询结果中匹配的实体数量。该值由内部的 <see cref="QueryCore"/> 维护，反映当前查询条件下的实体总数。
        /// </summary>
        public int Count => _core.Count;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => _core == null;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3> SkipUnchanged(bool skip = true) => new(_core, skip);

        /// <summary>
        /// 获取用于 foreach 的行枚举器，枚举每一行中的三个组件访问器及实体信息。
        /// </summary>
        public EntityQueryRowEnumerator<T1, T2, T3> GetEnumerator()
            => new(GetRefRWsFor<T1>(), GetRefRWsFor<T2>(), GetRefRWsFor<T3>(), _core.GetEntityEnumerator());

        /// <summary>
        /// 内部辅助：根据类型 T1 获取对应的 ArchetypeAccessor。
        /// </summary>
        private ArchetypeAccessor<T> GetAccessorFor<T>() where T : struct => _core.GetAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 内部辅助：获取指定类型的 RefRW 枚举器，用于按行访问可写引用包装（RefRW&lt;T1&gt;）。
        /// 供委托调用器通过统一泛型方法反射调用。
        /// </summary>
        internal ArchetypeAccessor<T>.RefRWEnumerator GetRefRWsFor<T>() where T : struct => GetAccessorFor<T>().GetRefRWs();

        /// <summary>
        /// 使用指定的委托对查询结果进行处理。
        /// </summary>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public static implicit operator EntityQuery(EntityQuery<T1, T2, T3> query) => new(query._core);
    }

    /// <summary>
    /// 四组件实体查询包装。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    public readonly struct EntityQuery<T1, T2, T3, T4>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        /// <summary>
        /// 当前查询结果中匹配的实体数量。该值由内部的 <see cref="QueryCore"/> 维护，反映当前查询条件下的实体总数。
        /// </summary>
        public int Count => _core.Count;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => _core == null;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4> SkipUnchanged(bool skip = true) => new(_core, skip);

        /// <summary>
        /// 获取用于 foreach 的行枚举器，枚举每一行中的四个组件访问器及实体信息。
        /// </summary>
        public EntityQueryRowEnumerator<T1, T2, T3, T4> GetEnumerator()
            => new(GetRefRWsFor<T1>(), GetRefRWsFor<T2>(), GetRefRWsFor<T3>(), GetRefRWsFor<T4>(), _core.GetEntityEnumerator());

        /// <summary>
        /// 内部辅助：根据类型 T1 获取对应的 ArchetypeAccessor。
        /// </summary>
        private ArchetypeAccessor<T> GetAccessorFor<T>() where T : struct => _core.GetAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 内部辅助：获取指定类型的 RefRW 枚举器，用于按行访问可写引用包装（RefRW&lt;T1&gt;）。
        /// 供委托调用器通过统一泛型方法反射调用。
        /// </summary>
        internal ArchetypeAccessor<T>.RefRWEnumerator GetRefRWsFor<T>() where T : struct => GetAccessorFor<T>().GetRefRWs();

        /// <summary>
        /// 使用指定的委托对查询结果进行处理。
        /// </summary>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public static implicit operator EntityQuery(EntityQuery<T1, T2, T3, T4> query) => new(query._core);
    }

    /// <summary>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// 五组件实体查询包装。
    /// </summary>
    /// <typeparam name="T1">第一个组件类型。</typeparam>
    /// <typeparam name="T2">第二个组件类型。</typeparam>
    /// <typeparam name="T3">第三个组件类型。</typeparam>
    /// <typeparam name="T4">第四个组件类型。</typeparam>
    /// <typeparam name="T5">第五个组件类型。</typeparam>
    public readonly struct EntityQuery<T1, T2, T3, T4, T5>
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        private readonly QueryCore _core;
        private readonly bool _skipUnchanged;

        /// <summary>
        /// 当前查询结果中匹配的实体数量。该值由内部的 <see cref="QueryCore"/> 维护，反映当前查询条件下的实体总数。
        /// </summary>
        public int Count => _core.Count;

        /// <summary>
        /// 获取当前实体查询是否为空（即未正确构建或已被销毁）。如果查询核心为 null 则表示查询无效或未初始化。
        /// </summary>
        public bool IsEmpty => _core == null;

        internal EntityQuery(QueryCore core, bool skipUnchanged = false)
        {
            _core = core;
            _skipUnchanged = skipUnchanged;
        }

        /// <summary>
        /// 设置是否跳过未变化块。
        /// </summary>
        public EntityQuery<T1, T2, T3, T4, T5> SkipUnchanged(bool skip = true) => new(_core, skip);

        /// <summary>
        /// 获取用于 foreach 的行枚举器，枚举每一行中的五个组件访问器及实体信息。
        /// </summary>
        public EntityQueryRowEnumerator<T1, T2, T3, T4, T5> GetEnumerator()
            => new(GetRefRWsFor<T1>(), GetRefRWsFor<T2>(), GetRefRWsFor<T3>(), GetRefRWsFor<T4>(), GetRefRWsFor<T5>(), _core.GetEntityEnumerator());

        /// <summary>
        /// 内部辅助：根据类型 T1 获取对应的 ArchetypeAccessor。
        /// </summary>
        private ArchetypeAccessor<T> GetAccessorFor<T>() where T : struct => _core.GetAccessor<T>(_skipUnchanged);

        /// <summary>
        /// 内部辅助：获取指定类型的 RefRW 枚举器，用于按行访问可写引用包装（RefRW&lt;T1&gt;）。
        /// 供委托调用器通过统一泛型方法反射调用。
        /// </summary>
        internal ArchetypeAccessor<T>.RefRWEnumerator GetRefRWsFor<T>() where T : struct => GetAccessorFor<T>().GetRefRWs();

        /// <summary>
        /// 使用指定的委托对查询结果进行处理。
        /// </summary>
        public void Query<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(@delegate);
            EntityQueryDelegateInvoker.Invoke(this, @delegate);
        }

        public static implicit operator EntityQuery(EntityQuery<T1, T2, T3, T4, T5> query) => new(query._core);
    }
}