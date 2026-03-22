using System.Linq.Expressions;
using System.Reflection;
using ExtenderApp.ECS.Accessors;

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
                var enumeratorVariable = Expression.Variable(enumeratorMethod.ReturnType, $"enumerator{i + 1}");
                variables.Add(enumeratorVariable);
                setupExpressions.Add(Expression.Assign(enumeratorVariable, Expression.Call(queryParameter, enumeratorMethod)));

                var moveNextExpression = Expression.Call(enumeratorVariable, enumeratorMethod.ReturnType.GetMethod(nameof(EntityQueryAccessor<int>.Enumerator.MoveNext))!);
                moveNextCondition = moveNextCondition == null
                    ? moveNextExpression
                    : Expression.AndAlso(moveNextCondition, moveNextExpression);

                var currentExpression = Expression.Property(enumeratorVariable, nameof(EntityQueryAccessor<int>.Enumerator.Current));
                switch (descriptors[i].PassKind)
                {
                    case PassKind.ByValue:
                        invokeArguments[i] = currentExpression;
                        break;
                    case PassKind.In:
                    {
                        var tempVariable = Expression.Variable(descriptors[i].ComponentType, $"arg{i + 1}");
                        variables.Add(tempVariable);
                        prepareExpressions.Add(Expression.Assign(
                            tempVariable,
                            Expression.Convert(currentExpression, descriptors[i].ComponentType)));
                        invokeArguments[i] = tempVariable;
                        break;
                    }
                    case PassKind.Ref:
                    {
                        var tempVariable = Expression.Variable(descriptors[i].ComponentType, $"arg{i + 1}");
                        variables.Add(tempVariable);
                        prepareExpressions.Add(Expression.Assign(
                            tempVariable,
                            Expression.Convert(currentExpression, descriptors[i].ComponentType)));
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

            var breakLabel = Expression.Label("break_loop");
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
                        Expression.Break(breakLabel)),
                    breakLabel));

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
        /// </summary>
        private static MethodInfo GetEnumeratorMethod(Type queryType, ParameterDescriptor descriptor)
        {
            string methodName = queryType.GenericTypeArguments.Length == 1
                ? descriptor.AccessorKind switch
                {
                    AccessorKind.Value => nameof(EntityQuery<int>.GetValues),
                    AccessorKind.RefRO => nameof(EntityQuery<int>.GetRefROs),
                    AccessorKind.RefRW => nameof(EntityQuery<int>.GetRefRWs),
                    _ => throw new ArgumentOutOfRangeException(nameof(descriptor))
                }
                : descriptor.AccessorKind switch
                {
                    AccessorKind.Value => $"GetEnumeratorForT{descriptor.QueryIndex + 1}",
                    AccessorKind.RefRO => $"GetRefROsForT{descriptor.QueryIndex + 1}",
                    AccessorKind.RefRW => $"GetRefRWsForT{descriptor.QueryIndex + 1}",
                    _ => throw new ArgumentOutOfRangeException(nameof(descriptor))
                };

            return queryType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                ?? throw new NotSupportedException($"未找到方法 {queryType.FullName}.{methodName}。");
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

        private enum PassKind : byte
        {
            ByValue,
            In,
            Ref,
        }
    }
}