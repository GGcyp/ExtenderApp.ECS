using System.Linq.Expressions;
using System.Reflection;
using ExtenderApp.ECS.Accessors;
using ExtenderApp.ECS.Queries.Rows;

namespace ExtenderApp.ECS.Queries
{
    /// <summary>
    /// EntityQuery 的委托执行器。
    /// 用于根据委托参数类型，将当前查询中的组件列自动映射到值枚举、<see cref="RefRO{T}" /> 或 <see cref="RefRW{T}" /> 访问器，
    /// 并通过表达式树生成一次性执行逻辑后缓存，避免重复反射开销。
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
            where T1 : struct
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
        /// 单组件查询的静态缓存。
        /// 每种委托签名只会编译一次执行器。
        /// </summary>
        private static class Cache<TDelegate>
            where TDelegate : Delegate
        {
            internal static readonly Action<EntityQuery, TDelegate> Invoker = Build();

            private static Action<EntityQuery, TDelegate> Build()
                => BuildInvoker<EntityQuery, TDelegate>(null);
        }

        /// <summary>
        /// 单组件查询的静态缓存。
        /// 每种委托签名只会编译一次执行器。
        /// </summary>
        private static class Cache<TDelegate, T1>
            where TDelegate : Delegate
            where T1 : struct
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
        /// 根据查询组件类型和委托签名构建执行器。
        /// 生成后的执行器会：
        /// 1. 为每个委托参数选择正确的枚举器；
        /// 2. 同步推进所有枚举器；
        /// 3. 按参数顺序调用目标委托。
        /// </summary>
        private static Action<TQuery, TDelegate> BuildInvoker<TQuery, TDelegate>(Type[] queryComponentTypes)
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

                var moveNextMethod = getEnumMethod.ReturnType.GetMethod(nameof(ArchetypeRowEnumerator.MoveNext));
                var currentProp = getEnumMethod.ReturnType.GetProperty(nameof(ArchetypeRowEnumerator.Current));
                if (moveNextMethod == null || currentProp == null)
                    throw new NotSupportedException("枚举器缺少 MoveNext/Current 方法或属性。");

                var breakLabel = Expression.Label("break_loop");

                var loopBody = Expression.Block(
                    Expression.Call(ap, invokeMethod, Expression.Property(enumeratorVar, currentProp)),
                    Expression.Empty()
                );

                var loop = Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Call(enumeratorVar, moveNextMethod),
                        loopBody,
                        Expression.Break(breakLabel)
                    ),
                    breakLabel
                );

                var body = Expression.Block(new[] { enumeratorVar }, assignEnum, loop);

                return Expression.Lambda<Action<TQuery, TDelegate>>(body, qp, ap).Compile();
            }

            var descriptors = CreateDescriptors(parameters, queryComponentTypes);
            var queryParameter = Expression.Parameter(typeof(TQuery), "query");
            var actionParameter = Expression.Parameter(typeof(TDelegate), "action");

            var variables = new List<ParameterExpression>(descriptors.Length * 2);
            var setupExpressions = new List<Expression>(descriptors.Length);
            var invokeArguments = new Expression[descriptors.Length];
            var prepareExpressions = new List<Expression>(descriptors.Length);
            var writeBackExpressions = new List<Expression>(descriptors.Length);
            Expression? moveNextCondition = null;

            for (int i = 0; i < descriptors.Length; i++)
            {
                var enumeratorMethod = GetEnumeratorMethod(typeof(TQuery), descriptors[i]);
                var enumeratorVariable = Expression.Variable(enumeratorMethod.ReturnType, $"_list{i + 1}");
                variables.Add(enumeratorVariable);
                setupExpressions.Add(Expression.Assign(enumeratorVariable, Expression.Call(queryParameter, enumeratorMethod)));

                var moveNextExpression = Expression.Call(enumeratorVariable, enumeratorMethod.ReturnType.GetMethod(nameof(ArchetypeAccessorEnumerator<int>.MoveNext))!);
                moveNextCondition = moveNextCondition == null
                    ? moveNextExpression
                    : Expression.AndAlso(moveNextCondition, moveNextExpression);

                var currentExpression = Expression.Property(enumeratorVariable, nameof(ArchetypeAccessorEnumerator<int>.Current));
                var componentValueExpression = GetComponentValueExpression(currentExpression, descriptors[i].ComponentType);
                switch (descriptors[i].PassKind)
                {
                    case PassKind.ByValue:
                        // 若委托参数为值类型（非 RefRO/RefRW），则传递 componentValueExpression（即 T 的副本）。
                        // 若委托参数为 RefRO/RefRW，这里需要传递包装类型。我们统一让底层返回 RefRW，并在需要只读包装时进行类型转换。
                        if (descriptors[i].AccessorKind == AccessorKind.Value)
                        {
                            invokeArguments[i] = componentValueExpression;
                        }
                        else if (descriptors[i].AccessorKind == AccessorKind.RefRO)
                        {
                            // 将底层的 RefRW<T> 转换为 RefRO<T>（存在隐式转换），在表达式树中使用显式转换。
                            var refROType = typeof(RefRO<>).MakeGenericType(descriptors[i].ComponentType);
                            invokeArguments[i] = Expression.Convert(currentExpression, refROType);
                        }
                        else // RefRW
                        {
                            invokeArguments[i] = currentExpression;
                        }
                        break;

                    case PassKind.In:
                        {
                            var tempVariable = Expression.Variable(descriptors[i].ComponentType, $"arg{i + 1}");
                            variables.Add(tempVariable);
                            prepareExpressions.Add(Expression.Assign(tempVariable, componentValueExpression));
                            invokeArguments[i] = tempVariable;
                            break;
                        }
                    case PassKind.Ref:
                        {
                            var tempVariable = Expression.Variable(descriptors[i].ComponentType, $"arg{i + 1}");
                            variables.Add(tempVariable);
                            prepareExpressions.Add(Expression.Assign(tempVariable, componentValueExpression));
                            invokeArguments[i] = tempVariable;
                            writeBackExpressions.Add(Expression.Call(
                                WriteRefRWValueMethod.MakeGenericMethod(descriptors[i].ComponentType),
                                currentExpression,
                                tempVariable));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var breakLabel2 = Expression.Label("break_loop");
            var loopBodyExpressions = new List<Expression>(prepareExpressions.Count + 1 + writeBackExpressions.Count);
            loopBodyExpressions.AddRange(prepareExpressions);
            loopBodyExpressions.Add(Expression.Call(actionParameter, invokeMethod, invokeArguments));
            loopBodyExpressions.AddRange(writeBackExpressions);

            var expressions = new List<Expression>(setupExpressions.Count + 1);
            expressions.AddRange(setupExpressions);
            expressions.Add(
                Expression.Loop(
                    Expression.IfThenElse(
                        moveNextCondition!,
                        Expression.Block(loopBodyExpressions),
                        Expression.Break(breakLabel2)),
                    breakLabel2));

            return Expression.Lambda<Action<TQuery, TDelegate>>(
                Expression.Block(variables, expressions),
                queryParameter,
                actionParameter).Compile();
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
        /// 根据参数描述选择当前查询类型上对应的枚举器方法。
        /// 改动：统一使用可写枚举器（RefRW），当参数为 RefRO 时在表达式中做转换以避免额外的枚举器方法。
        /// </summary>
        private static MethodInfo GetEnumeratorMethod(Type queryType, ParameterDescriptor descriptor)
        {
            // 统一使用 EntityQuery 上的泛型方法 GetRefRWsFor<T>()，避免为不同组件位次暴露 GetRefRWsForT1..T5。
            var genericMethod = queryType.GetMethod(
                "GetRefRWsFor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (genericMethod == null)
                throw new NotSupportedException($"未找到方法 {queryType.FullName}.GetRefRWsFor<T>()。");

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
        /// 委托参数描述。
        /// 记录组件类型、访问方式以及其在查询中的列索引。
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