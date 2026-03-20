namespace ExtenderApp.ECS.Abstract
{
    /// <summary>
    /// 轻量结构体枚举器接口。用于对返回值类型为 <typeparamref name="T"/> 的 struct 枚举器进行统一约束，
    /// 以便在泛型上下文中传递并避免装箱。
    /// </summary>
    /// <typeparam name="T">枚举器返回项的类型。</typeparam>
    public interface IStructEnumerator<T>
    {
        bool MoveNext();

        T Current { get; }
    }

    /// <summary>
    /// 用于实现任何结构体枚举器的通用包装器 <see cref="IStructEnumerator{TItem}"/>.
    /// 使用范型约束可以在保持 struct 值语义的同时以统一类型访问不同枚举器。
    /// </summary>
    public readonly struct GenericEnumerator<TEnum, TItem>
        where TEnum : struct, IStructEnumerator<TItem>
    {
        private readonly TEnum _inner;

        public GenericEnumerator(TEnum inner) => _inner = inner;

        public bool MoveNext() => _inner.MoveNext();

        public TItem Current => _inner.Current;
    }
}