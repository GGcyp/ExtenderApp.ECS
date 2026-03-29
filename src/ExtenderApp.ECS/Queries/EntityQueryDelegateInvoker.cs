using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using ExtenderApp.ECS.Accessors;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// EntityQuery 的委托执行器。 用于根据委托参数类型，将当前查询中的组件列自动映射到值枚举、 <see cref="RefRO{T}" /> 或 <see cref="RefRW{T}" /> 访问器， 并通过表达式树生成一次性执行逻辑后缓存，避免重复反射开销。
    /// </summary>
    internal static class EntityQueryDelegateInvoker
    {
        private static readonly MethodInfo ReadRefROValueMethod = typeof(EntityQueryDelegateInvoker)
            .GetMethod(nameof(ReadRefROValue), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo ReadRefRWValueMethod = typeof(EntityQueryDelegateInvoker)
            .GetMethod(nameof(ReadRefRWValue), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo WriteRefRWValueMethod = typeof(EntityQueryDelegateInvoker)
            .GetMethod(nameof(WriteRefRWValue), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static T ReadRefROValue<T>(RefRO<T> value) where T : struct => value.Value;

        private static T ReadRefRWValue<T>(RefRW<T> value) where T : struct => value.Value;

        private static void WriteRefRWValue<T>(RefRW<T> value, T component) where T : struct => value.Value = component;

        /// <summary>
        /// 执行实体查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate>(EntityQuery query, TDelegate @delegate)
            where TDelegate : Delegate
            => Cache<TDelegate>.Invoker(query, @delegate);

        /// <summary>
        /// 执行单组件查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate, T1>(EntityQuery<T1> query, TDelegate @delegate)
            where TDelegate : Delegate
            => Cache<TDelegate, T1>.Invoker(query, @delegate);

        /// <summary>
        /// 执行双组件查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate, T1, T2>(EntityQuery<T1, T2> query, TDelegate @delegate)
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            => Cache<TDelegate, T1, T2>.Invoker(query, @delegate);

        /// <summary>
        /// 执行三组件查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate, T1, T2, T3>(EntityQuery<T1, T2, T3> query, TDelegate @delegate)
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
            => Cache<TDelegate, T1, T2, T3>.Invoker(query, @delegate);

        /// <summary>
        /// 执行四组件查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate, T1, T2, T3, T4>(EntityQuery<T1, T2, T3, T4> query, TDelegate @delegate)
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            => Cache<TDelegate, T1, T2, T3, T4>.Invoker(query, @delegate);

        /// <summary>
        /// 执行五组件查询对应的委托。
        /// </summary>
        public static void Invoke<TDelegate, T1, T2, T3, T4, T5>(EntityQuery<T1, T2, T3, T4, T5> query, TDelegate @delegate)
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            => Cache<TDelegate, T1, T2, T3, T4, T5>.Invoker(query, @delegate);

        /// <summary>
        /// 非泛型 EntityQuery 的调用器缓存入口。 延迟根据实际查询的组件类型构建并缓存执行器，避免在类型初始化时期就触发编译。
        /// </summary>
        private static class Cache<TDelegate>
            where TDelegate : Delegate
        {
            internal static readonly Action<EntityQuery, TDelegate> Invoker = (query, del) =>
            {
                var inv = CacheInvoker.GetOrAddInvoker(query.QueryDesc.Query);
                inv(query, del);
            };

            private static class CacheInvoker
            {
                private static readonly ConcurrentDictionary<string, Action<EntityQuery, TDelegate>> _map = new();

                public static Action<EntityQuery, TDelegate> GetOrAddInvoker(ComponentMask mask)
                {
                    string key = mask.IsEmpty
                        ? "<empty>"
                        : $"{typeof(TDelegate).AssemblyQualifiedName}\0{BuildCanonicalMaskKey(mask)}";

                    return _map.GetOrAdd(key, _ =>
                    {
                        if (mask.IsEmpty)
                            return BuildInvoker<EntityQuery, TDelegate>(null);

                        Type[] types = ResolveEntityQueryRowComponentOrder(mask);

                        // Build a thin wrapper that constructs a typed EntityQuery<TSystem...> from EntityQuery.Core
                        // and forwards to the corresponding generic Cache<TDelegate, TSystem...>.Invoker delegate.
                        var outer = typeof(EntityQueryDelegateInvoker);
                        Type cacheTypeDef = outer.GetNestedType($"Cache`{1 + types.Length}", BindingFlags.NonPublic)!
                            ?? throw new InvalidOperationException("Cannot find nested Cache type definition.");

                        var genericArgs = new Type[1 + types.Length];
                        genericArgs[0] = typeof(TDelegate);
                        for (int i = 0; i < types.Length; i++) genericArgs[i + 1] = types[i];

                        var closedCacheType = cacheTypeDef.MakeGenericType(genericArgs);
                        var invokerField = closedCacheType.GetField("Invoker", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new InvalidOperationException("Invoker field not found on cache type.");

                        var invokerDelegate = (Delegate)invokerField.GetValue(null)!;

                        // Build Expression: (EntityQuery q, TDelegate d) => invokerDelegate(new EntityQuery<TSystem...>(q.Core), d)
                        var qParam = Expression.Parameter(typeof(EntityQuery), "q");
                        var dParam = Expression.Parameter(typeof(TDelegate), "d");

                        // Get EntityQueryCore property
                        var coreProp = typeof(EntityQuery).GetProperty("Core", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
                        var coreExpr = Expression.Property(qParam, coreProp);

                        // 构造 EntityQuery<T1,...>(core, skipUnchanged: false)。元数据上 ctor 为 (EntityQueryCore, bool)，无单独单参构造函数。
                        Expression typedQueryExpr;
                        var ctorParamTypes = new[] { typeof(EntityQueryCore), typeof(bool) };
                        var skipUnchangedExpr = Expression.Constant(false);

                        if (types.Length == 1)
                        {
                            var tq = typeof(EntityQuery<>).MakeGenericType(types[0]);
                            var ctor = tq.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ctorParamTypes, null)
                                       ?? throw new InvalidOperationException($"Typed EntityQuery ctor not found: {tq.Name}.");
                            typedQueryExpr = Expression.New(ctor, coreExpr, skipUnchangedExpr);
                        }
                        else if (types.Length == 2)
                        {
                            var tq = typeof(EntityQuery<,>).MakeGenericType(types[0], types[1]);
                            var ctor = tq.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ctorParamTypes, null)
                                       ?? throw new InvalidOperationException($"Typed EntityQuery ctor not found: {tq.Name}.");
                            typedQueryExpr = Expression.New(ctor, coreExpr, skipUnchangedExpr);
                        }
                        else if (types.Length == 3)
                        {
                            var tq = typeof(EntityQuery<,,>).MakeGenericType(types[0], types[1], types[2]);
                            var ctor = tq.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ctorParamTypes, null)
                                       ?? throw new InvalidOperationException($"Typed EntityQuery ctor not found: {tq.Name}.");
                            typedQueryExpr = Expression.New(ctor, coreExpr, skipUnchangedExpr);
                        }
                        else if (types.Length == 4)
                        {
                            var tq = typeof(EntityQuery<,,,>).MakeGenericType(types[0], types[1], types[2], types[3]);
                            var ctor = tq.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ctorParamTypes, null)
                                       ?? throw new InvalidOperationException($"Typed EntityQuery ctor not found: {tq.Name}.");
                            typedQueryExpr = Expression.New(ctor, coreExpr, skipUnchangedExpr);
                        }
                        else if (types.Length == 5)
                        {
                            var tq = typeof(EntityQuery<,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], types[4]);
                            var ctor = tq.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ctorParamTypes, null)
                                       ?? throw new InvalidOperationException($"Typed EntityQuery ctor not found: {tq.Name}.");
                            typedQueryExpr = Expression.New(ctor, coreExpr, skipUnchangedExpr);
                        }
                        else
                        {
                            // fallback to building with BuildInvoker (less efficient)
                            return BuildInvoker<EntityQuery, TDelegate>(types);
                        }

                        var invokeConst = Expression.Constant(invokerDelegate);
                        var invokeCall = Expression.Invoke(invokeConst, typedQueryExpr, dParam);

                        var lambda = Expression.Lambda<Action<EntityQuery, TDelegate>>(invokeCall, qParam, dParam).Compile();
                        return lambda;
                    });
                }

                /// <summary>
                /// 将 Query 掩码中的组件类型排序后拼接，供缓存键区分不同查询（与委托类型组合使用）。
                /// </summary>
                private static string BuildCanonicalMaskKey(ComponentMask mask)
                {
                    var names = new List<string>();
                    foreach (var ct in mask)
                    {
                        var t = ct.TypeInstance ?? throw new InvalidOperationException("组件掩码中存在无运行时类型的项。");
                        names.Add(t.AssemblyQualifiedName ?? t.FullName ?? t.Name);
                    }
                    names.Sort(StringComparer.Ordinal);
                    return string.Join(",", names);
                }

                /// <summary>
                /// 按 <typeparamref name="TDelegate"/> 的 Invoke 参数顺序解析组件 CLR 类型，并与 Query 掩码做多重集校验；该顺序即 <c>EntityQuery&lt;T1,...&gt;</c> 行类型顺序。
                /// </summary>
                private static Type[] ResolveEntityQueryRowComponentOrder(ComponentMask mask)
                {
                    var invoke = typeof(TDelegate).GetMethod(nameof(Action.Invoke), BindingFlags.Instance | BindingFlags.Public)
                        ?? throw new NotSupportedException($"无法找到委托 {typeof(TDelegate)} 的 Invoke 方法。");
                    if (invoke.ReturnType != typeof(void))
                        throw new NotSupportedException($"委托 {typeof(TDelegate)} 必须返回 void。");
                    ParameterInfo[] parameters = invoke.GetParameters();
                    if (parameters.Length != mask.ComponentCount)
                        throw new NotSupportedException(
                            $"非泛型 EntityQuery.Query：委托参数数量 ({parameters.Length}) 须等于 Query 掩码组件数 ({mask.ComponentCount})。");
                    var types = new Type[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        types[i] = ExtractComponentTypeFromQueryDelegateParameter(parameters[i]);
                    if (!MaskHasSameComponentTypesAsOrderedList(mask, types))
                        throw new NotSupportedException(
                            "委托参数中的组件类型集合与当前查询的 Query 掩码不一致。");
                    return types;
                }

                private static Type ExtractComponentTypeFromQueryDelegateParameter(ParameterInfo parameter)
                {
                    var parameterType = parameter.ParameterType;
                    if (parameterType.IsByRef)
                    {
                        if (parameter.IsOut)
                            throw new NotSupportedException($"参数 {parameter.Name} 不支持 out。");
                        var elementType = parameterType.GetElementType()
                            ?? throw new NotSupportedException($"无法解析参数 {parameter.Name} 的 ByRef 元素类型。");
                        if (elementType.IsGenericType)
                        {
                            var def = elementType.GetGenericTypeDefinition();
                            if (def == typeof(RefRO<>))
                                return elementType.GenericTypeArguments[0];
                            if (def == typeof(RefRW<>))
                                return elementType.GenericTypeArguments[0];
                        }
                        if (!elementType.IsValueType)
                            throw new NotSupportedException($"组件类型 {elementType} 必须是 struct。");
                        return elementType;
                    }
                    if (parameterType.IsGenericType)
                    {
                        var def = parameterType.GetGenericTypeDefinition();
                        if (def == typeof(RefRO<>))
                            return parameterType.GenericTypeArguments[0];
                        if (def == typeof(RefRW<>))
                            return parameterType.GenericTypeArguments[0];
                    }
                    if (!parameterType.IsValueType)
                        throw new NotSupportedException($"组件类型 {parameterType} 必须是 struct。");
                    return parameterType;
                }

                private static bool MaskHasSameComponentTypesAsOrderedList(ComponentMask mask, Type[] ordered)
                {
                    var counts = new Dictionary<Type, int>();
                    foreach (var ct in mask)
                    {
                        var t = ct.TypeInstance ?? throw new InvalidOperationException("组件掩码中存在无运行时类型的项。");
                        counts.TryGetValue(t, out var n);
                        counts[t] = n + 1;
                    }
                    var temp = new Dictionary<Type, int>(counts);
                    foreach (var t in ordered)
                    {
                        if (!temp.TryGetValue(t, out var n) || n == 0)
                            return false;
                        temp[t] = n - 1;
                    }
                    foreach (var (_, n) in temp)
                    {
                        if (n != 0)
                            return false;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// 单组件查询的静态缓存。 每种委托签名只会编译一次执行器。
        /// </summary>
        private static class Cache<TDelegate, T1>
            where TDelegate : Delegate
        {
            internal static readonly Action<EntityQuery<T1>, TDelegate> Invoker = Build();

            private static Action<EntityQuery<T1>, TDelegate> Build()
                => BuildInvoker<EntityQuery<T1>, TDelegate>(
                    [typeof(T1)]);
        }

        /// <summary>
        /// 双组件查询的静态缓存。
        /// </summary>
        private static class Cache<TDelegate, T1, T2>
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
        {
            internal static readonly Action<EntityQuery<T1, T2>, TDelegate> Invoker = Build();

            private static Action<EntityQuery<T1, T2>, TDelegate> Build()
                => BuildInvoker<EntityQuery<T1, T2>, TDelegate>(
                    [typeof(T1), typeof(T2)]);
        }

        /// <summary>
        /// 三组件查询的静态缓存。
        /// </summary>
        private static class Cache<TDelegate, T1, T2, T3>
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            internal static readonly Action<EntityQuery<T1, T2, T3>, TDelegate> Invoker = Build();

            private static Action<EntityQuery<T1, T2, T3>, TDelegate> Build()
                => BuildInvoker<EntityQuery<T1, T2, T3>, TDelegate>(
                    [typeof(T1), typeof(T2), typeof(T3)]);
        }

        /// <summary>
        /// 四组件查询的静态缓存。
        /// </summary>
        private static class Cache<TDelegate, T1, T2, T3, T4>
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            internal static readonly Action<EntityQuery<T1, T2, T3, T4>, TDelegate> Invoker = Build();

            private static Action<EntityQuery<T1, T2, T3, T4>, TDelegate> Build()
                => BuildInvoker<EntityQuery<T1, T2, T3, T4>, TDelegate>(
                    [typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        }

        /// <summary>
        /// 五组件查询的静态缓存。
        /// </summary>
        private static class Cache<TDelegate, T1, T2, T3, T4, T5>
            where TDelegate : Delegate
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
        {
            internal static readonly Action<EntityQuery<T1, T2, T3, T4, T5>, TDelegate> Invoker = Build();

            private static Action<EntityQuery<T1, T2, T3, T4, T5>, TDelegate> Build()
                => BuildInvoker<EntityQuery<T1, T2, T3, T4, T5>, TDelegate>(
                    [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)]);
        }

        /// <summary>
        /// 在行类型上查找唯一匹配的 <c>op_Implicit</c>（返回 <paramref name="returnType"/>）。
        /// </summary>
        private static MethodInfo FindUniqueRowImplicitOrThrow(Type rowType, Type returnType)
        {
            MethodInfo? found = null;
            foreach (var m in rowType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "op_Implicit" || m.ReturnType != returnType || m.GetParameters().Length != 1 || m.GetParameters()[0].ParameterType != rowType)
                    continue;
                if (found != null)
                    throw new NotSupportedException($"行类型 {rowType} 上存在多个指向 {returnType} 的 op_Implicit，无法消歧。");
                found = m;
            }

            return found ?? throw new NotSupportedException($"未找到 {rowType} -> {returnType} 的 op_Implicit。");
        }

        /// <summary>
        /// 根据查询组件类型和委托签名构建执行器。 生成后的执行器会：
        /// 1. 为每个委托参数选择正确的枚举器；
        /// 2. 同步推进所有枚举器；
        /// 3. 按参数顺序调用目标委托。
        /// </summary>
        private static Action<TQuery, TDelegate> BuildInvoker<TQuery, TDelegate>(Type[]? queryComponentTypes)
            where TDelegate : Delegate
        {
            var invokeMethod = typeof(TDelegate).GetMethod(nameof(Action.Invoke))
                ?? throw new NotSupportedException($"无法找到委托 {typeof(TDelegate)} 的 Invoke 方法。");

            if (invokeMethod.ReturnType != typeof(void))
                throw new NotSupportedException($"委托 {typeof(TDelegate)} 必须返回 void。");

            var parameters = invokeMethod.GetParameters();
            if (parameters.Length == 0)
                throw new NotSupportedException($"委托 {typeof(TDelegate)} 至少需要一个参数。");

            // Special-case: queryComponentTypes == null means this is the non-generic EntityQuery (only entities)
            if (queryComponentTypes == null)
            {
                // Delegate must accept exactly one parameter of type Entity
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Entity))
                    throw new NotSupportedException($"对于只查询实体的 Query，委托必须形如: void (Entity e)。");

                var qp = Expression.Parameter(typeof(TQuery), "query");
                var ap = Expression.Parameter(typeof(TDelegate), "action");

                // Call query.GetEnumerator()
                var getEnumMethod = typeof(TQuery).GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new NotSupportedException($"未找到方法 {typeof(TQuery)}.GetEnumerator()。");

                var enumeratorVar = Expression.Variable(getEnumMethod.ReturnType, "_list");
                var assignEnum = Expression.Assign(enumeratorVar, Expression.Call(qp, getEnumMethod));

                var moveNextMethod = getEnumMethod.ReturnType.GetMethod(nameof(GlobalRowEnumerator.MoveNext));
                var currentProp = getEnumMethod.ReturnType.GetProperty(nameof(GlobalRowEnumerator.Current));
                if (moveNextMethod == null || currentProp == null)
                    throw new NotSupportedException("枚举器缺少 MoveNext/Current 方法或属性。");

                var breakLabel1 = Expression.Label("break_loop");

                var loopBody1 = Expression.Block(
                    Expression.Call(ap, invokeMethod, Expression.Property(enumeratorVar, currentProp)),
                    Expression.Empty()
                );

                var loop1 = Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Call(enumeratorVar, moveNextMethod),
                        loopBody1,
                        Expression.Break(breakLabel1)
                    ),
                    breakLabel1
                );

                var body1 = Expression.Block(new[] { enumeratorVar }, assignEnum, loop1);

                return Expression.Lambda<Action<TQuery, TDelegate>>(body1, qp, ap).Compile();
            }

            var descriptors = CreateDescriptors(parameters, queryComponentTypes);
            var queryParameter = Expression.Parameter(typeof(TQuery), "query");
            var actionParameter = Expression.Parameter(typeof(TDelegate), "action");

            // 使用 query.GetEnumerator() 返回的行枚举器（GlobalRowEnumerator或其泛型版本）作为单一枚举器。
            var getEnumMethod2 = typeof(TQuery).GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new NotSupportedException($"未找到方法 {typeof(TQuery)}.GetEnumerator()。");

            var enumeratorVar2 = Expression.Variable(getEnumMethod2.ReturnType, "_enumerator2");
            var assignEnum2 = Expression.Assign(enumeratorVar2, Expression.Call(queryParameter, getEnumMethod2));

            var moveNextMethod2 = getEnumMethod2.ReturnType.GetMethod("MoveNext")
                ?? throw new NotSupportedException("枚举器缺少 MoveNext 方法。");
            var currentProp2 = getEnumMethod2.ReturnType.GetProperty("Current")
                ?? throw new NotSupportedException("枚举器缺少 Current 属性。");

            // 准备临时变量与表达式集合
            var variables = new List<ParameterExpression> { enumeratorVar2 };
            var setupExpressions = new List<Expression> { assignEnum2 };
            var prepareExpressions = new List<Expression>();
            var writeBackExpressions = new List<Expression>();
            var invokeArguments = new Expression[descriptors.Length];

            // 当前行（EntityQueryRow<...>）表达式
            var rowExpr = Expression.Property(enumeratorVar2, currentProp2);
            var rowType = currentProp2.PropertyType;

            for (int i = 0; i < descriptors.Length; i++)
            {
                var desc = descriptors[i];
                var slotType = queryComponentTypes[desc.QueryIndex];
                if (slotType != desc.ComponentType)
                    throw new NotSupportedException($"内部错误：查询第 {desc.QueryIndex} 列类型为 {slotType}，与委托参数组件类型 {desc.ComponentType} 不一致。");

                var refRwSlotType = typeof(RefRW<>).MakeGenericType(slotType);
                var toRefRwFromRow = FindUniqueRowImplicitOrThrow(rowType, refRwSlotType);
                Expression refRwExpr = Expression.Call(toRefRwFromRow, rowExpr);

                Expression ComponentExprForInvoke()
                {
                    switch (desc.PassKind, desc.AccessorKind)
                    {
                        case (PassKind.ByValue, AccessorKind.Value):
                            return Expression.Call(ReadRefRWValueMethod.MakeGenericMethod(desc.ComponentType), refRwExpr);
                        case (PassKind.ByValue, AccessorKind.RefRO):
                            {
                                var refRoType = typeof(RefRO<>).MakeGenericType(desc.ComponentType);
                                var toRefRo = FindUniqueRowImplicitOrThrow(rowType, refRoType);
                                return Expression.Call(toRefRo, rowExpr);
                            }
                        case (PassKind.ByValue, AccessorKind.RefRW):
                            return refRwExpr;
                        case (PassKind.In, _):
                            return Expression.Call(ReadRefRWValueMethod.MakeGenericMethod(desc.ComponentType), refRwExpr);
                        case (PassKind.Ref, _):
                            return Expression.Call(ReadRefRWValueMethod.MakeGenericMethod(desc.ComponentType), refRwExpr);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Expression componentAccessExpr = ComponentExprForInvoke();

                switch (desc.PassKind)
                {
                    case PassKind.ByValue:
                        invokeArguments[i] = componentAccessExpr;
                        break;

                    case PassKind.In:
                        {
                            var tempVar = Expression.Variable(desc.ComponentType, $"arg{i + 1}");
                            variables.Add(tempVar);
                            prepareExpressions.Add(Expression.Assign(tempVar, componentAccessExpr));
                            invokeArguments[i] = tempVar;
                            break;
                        }

                    case PassKind.Ref:
                        {
                            var tempVar = Expression.Variable(desc.ComponentType, $"arg{i + 1}");
                            variables.Add(tempVar);
                            prepareExpressions.Add(Expression.Assign(tempVar, componentAccessExpr));
                            invokeArguments[i] = tempVar;
                            writeBackExpressions.Add(Expression.Call(
                                WriteRefRWValueMethod.MakeGenericMethod(desc.ComponentType),
                                refRwExpr,
                                tempVar));
                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // 组装循环体
            var breakLabel3 = Expression.Label("break_loop");
            var loopBodyExpressions = new List<Expression>();
            loopBodyExpressions.AddRange(prepareExpressions);
            loopBodyExpressions.Add(Expression.Call(actionParameter, invokeMethod, invokeArguments));
            loopBodyExpressions.AddRange(writeBackExpressions);

            var loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.Call(enumeratorVar2, moveNextMethod2),
                    Expression.Block(loopBodyExpressions),
                    Expression.Break(breakLabel3)
                ),
                breakLabel3);

            var body = Expression.Block(variables, setupExpressions.Concat(new[] { loop }));

            return Expression.Lambda<Action<TQuery, TDelegate>>(body, queryParameter, actionParameter).Compile();
        }

        /// <summary>
        /// 将委托参数解析为内部参数描述，并映射到当前查询中的组件索引。
        /// </summary>
        private static ParameterDescriptor[] CreateDescriptors(ParameterInfo[] parameters, Type[] queryComponentTypes)
        {
            var descriptors = new ParameterDescriptor[parameters.Length];
            HashSet<int> mappedIndices = new();

            for (int i = 0; i < parameters.Length; i++)
            {
                var descriptor = ParameterDescriptor.Create(parameters[i]);
                int index = Array.FindIndex(queryComponentTypes, type => type == descriptor.ComponentType);
                if (index < 0)
                    throw new NotSupportedException($"委托参数 {parameters[i].Name} 的组件类型 {descriptor.ComponentType} 不在当前查询中。");

                if (!mappedIndices.Add(index))
                    throw new NotSupportedException($"委托参数中重复映射了查询中的组件类型 {descriptor.ComponentType}。");

                descriptors[i] = descriptor with { QueryIndex = index };
            }

            return descriptors;
        }

        /// <summary>
        /// 根据参数描述选择当前查询类型上对应的枚举器方法。 改动：统一使用可写枚举器（RefRW），当参数为 RefRO 时在表达式中做转换以避免额外的枚举器方法。
        /// </summary>
        private static MethodInfo GetEnumeratorMethod(Type queryType, ParameterDescriptor descriptor)
        {
            // 统一使用 EntityQuery 上的泛型方法 GetRefRWsFor<T1>()，避免为不同组件位次暴露 GetRefRWsForT1..T5。
            var genericMethod = queryType.GetMethod(
                "GetRefRWsFor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (genericMethod == null)
                throw new NotSupportedException($"未找到方法 {queryType.FullName}.GetRefRWsFor<T1>()。");

            if (!genericMethod.IsGenericMethodDefinition)
                throw new NotSupportedException($"方法 {queryType.FullName}.GetRefRWsFor 必须是泛型方法定义。");

            return genericMethod.MakeGenericMethod(descriptor.ComponentType);
        }

        private static Expression GetComponentValueExpression(Expression currentExpression, Type componentType)
        {
            if (currentExpression.Type == componentType)
                return currentExpression;

            if (currentExpression.Type.IsGenericType)
            {
                var genericTypeDefinition = currentExpression.Type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(RefRO<>))
                {
                    return Expression.Call(
                        ReadRefROValueMethod.MakeGenericMethod(componentType),
                        currentExpression);
                }

                if (genericTypeDefinition == typeof(RefRW<>))
                {
                    return Expression.Call(
                        ReadRefRWValueMethod.MakeGenericMethod(componentType),
                        currentExpression);
                }
            }

            return Expression.Convert(currentExpression, componentType);
        }

        /// <summary>
        /// 委托参数描述。 记录组件类型、访问方式以及其在查询中的列索引。
        /// </summary>
        private readonly record struct ParameterDescriptor(Type ComponentType, AccessorKind AccessorKind, PassKind PassKind, int QueryIndex)
        {
            public static ParameterDescriptor Create(ParameterInfo parameter)
            {
                var parameterType = parameter.ParameterType;
                if (parameterType.IsByRef)
                {
                    if (parameter.IsOut)
                        throw new NotSupportedException($"参数 {parameter.Name} 不支持 out。"
                        );

                    var componentType = parameterType.GetElementType()
                        ?? throw new NotSupportedException($"无法解析参数 {parameter.Name} 的 ByRef 元素类型。");

                    return Create(componentType, parameter.IsIn ? AccessorKind.RefRO : AccessorKind.RefRW, parameter.IsIn ? PassKind.In : PassKind.Ref);
                }

                if (parameterType.IsGenericType)
                {
                    var genericTypeDefinition = parameterType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(RefRO<>))
                        return Create(parameterType.GenericTypeArguments[0], AccessorKind.RefRO, PassKind.ByValue);

                    if (genericTypeDefinition == typeof(RefRW<>))
                        return Create(parameterType.GenericTypeArguments[0], AccessorKind.RefRW, PassKind.ByValue);
                }

                return Create(parameterType, AccessorKind.Value, PassKind.ByValue);
            }

            private static ParameterDescriptor Create(Type componentType, AccessorKind accessorKind, PassKind passKind)
            {
                if (!componentType.IsValueType)
                    throw new NotSupportedException($"组件类型 {componentType} 必须是 struct。");

                return new ParameterDescriptor(componentType, accessorKind, passKind, -1);
            }
        }

        /// <summary>
        /// 访问器种类。
        /// </summary>
        private enum AccessorKind : byte
        {
            Value,
            RefRO,
            RefRW,
        }

        /// <summary>
        /// 跳过、按值传递或按引用传递。
        /// </summary>
        private enum PassKind : byte
        {
            ByValue,
            In,
            Ref,
        }
    }
}