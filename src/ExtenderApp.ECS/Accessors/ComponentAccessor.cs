using ExtenderApp.ECS.Abstract;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Accessors
{
    /// <summary>
    /// 按列访问指定组件类型 <c>T</c> 的轻量访问器。
    ///
    /// 语义：当调用方已经知道某列的组件类型为 <c>T</c> 时，使用此类型可以直接 从对应的 <see cref="ArchetypeChunk{T}" /> 读取组件副本或创建对组件的引用包装（ <see cref="RefRO{T}" /> / <see
    /// cref="RefRW{T}" />）， 避免在热路径中频繁进行运行时类型转换或产生堆分配。
    /// </summary>
    /// <typeparam name="T">组件类型（值类型，且实现 <see cref="IComponent" />）。</typeparam>
    public readonly struct ComponentAccessor<T> where T : struct
    {
        /// <summary>
        /// 对应的组件列块，构造时由上层传入（已为 <see cref="ArchetypeChunk{T}" />）。
        /// </summary>
        private readonly ArchetypeChunk<T> _chunk;

        /// <summary>
        /// 获取当前访问器中组件的数量（即块内实体数量）。调用方应确保在访问前块已正确初始化，否则可能返回未定义值或抛出异常。
        /// </summary>
        public int Count => _chunk.Count;

        /// <summary>
        /// 使用指定的 <see cref="ArchetypeChunk{T}" /> 创建访问器实例。 请确保传入的 current 类型与 T 对应且已被正确初始化（已调用 Initialize）。
        /// </summary>
        /// <param name="chunk">已初始化且类型匹配的组件块。</param>
        internal ComponentAccessor(ArchetypeChunk<T> chunk) => _chunk = chunk;

        /// <summary>
        /// 按值读取指定局部索引处的组件副本。
        /// </summary>
        /// <param name="index">块内的局部索引（0-based）。</param>
        /// <returns>指定位置的组件副本。若索引越界或块未初始化，可能抛出异常或返回未定义值，调用方应负责保证索引正确。</returns>
        public T GetValue(int index) => _chunk.GetComponent(index);

        /// <summary>
        /// 创建只读引用包装（ <see cref="RefRO{T}" />），用于以 <c>ref readonly</c> 语义读取组件而不产生拷贝。
        /// </summary>
        /// <param name="index">块内的局部索引（0-based）。</param>
        /// <returns>返回对应位置的 <see cref="RefRO{T}" />。</returns>
        public RefRO<T> GetRefRO(int index) => new(_chunk, index);

        /// <summary>
        /// 创建可写引用包装（ <see cref="RefRW{T}" />），用于以 <c>ref</c> 语义直接修改组件数据，避免拷贝开销。
        /// </summary>
        /// <param name="index">块内的局部索引（0-based）。</param>
        /// <returns>返回对应位置的 <see cref="RefRW{T}" />。</returns>
        public RefRW<T> GetRefRW(int index) => new(_chunk, index);

        /// <summary>
        /// 获取结构体枚举器，用于遍历该单个块内的所有现有元素（不跨块链）。 返回的枚举器为 struct，可与 foreach 一起使用以避免堆分配。
        /// </summary>
        /// <returns>组件枚举器（返回可写引用）。</returns>
        public Enumerator GetEnumerator() => new Enumerator(_chunk);

        /// <summary>
        /// 获取返回只读包装的枚举器（每个元素以 <see cref="RefRO{T}" /> 形式返回），枚举仅限当前块。
        /// </summary>
        public RefROEnumerator GetRefROs() => new(_chunk);

        /// <summary>
        /// 获取返回可写包装的枚举器（每个元素以 <see cref="RefRW{T}" /> 形式返回），枚举仅限当前块。
        /// </summary>
        public RefRWEnumerator GetRefRWs() => new(_chunk);

        /// <summary>
        /// 组件枚举器：按单块内顺序枚举当前块中所有已占用的槽位，并返回对应元素的副本（值语义）。 注意：在持有返回的引用期间不要对底层 Chunk 做结构性修改（如归还、释放或重新初始化），否则可能导致悬挂引用或未定义行为。
        /// </summary>
        public struct Enumerator : IStructEnumerator<T>
        {
            private readonly ArchetypeChunk<T> _chunk;
            private int _localIndex;
            public readonly T Current => _chunk.GetComponent(_localIndex);

            internal Enumerator(ArchetypeChunk<T> chunk)
            {
                _chunk = chunk;
                _localIndex = -1;
            }

            public bool MoveNext()
            {
                var cur = _chunk;
                if (cur == null) return false;

                int idx = _localIndex + 1;
                if (idx < cur.Count)
                {
                    _localIndex = idx;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 枚举器：返回每个元素的只读包装（RefRO&lt;T&gt;），仅遍历当前块。
        /// </summary>
        public struct RefROEnumerator : IStructEnumerator<RefRO<T>>
        {
            private readonly ArchetypeChunk<T> _currentChunk;
            private int _localIndex;
            public RefRO<T> Current => new RefRO<T>(_currentChunk, _localIndex);

            internal RefROEnumerator(ArchetypeChunk<T> chunk)
            {
                _currentChunk = chunk;
                _localIndex = -1;
            }

            public bool MoveNext()
            {
                var cur = _currentChunk;
                if (cur == null) return false;

                int idx = _localIndex + 1;
                if (idx < cur.Count)
                {
                    _localIndex = idx;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 枚举器：返回每个元素的可写包装（RefRW&lt;T&gt;），仅遍历当前块。
        /// </summary>
        public struct RefRWEnumerator : IStructEnumerator<RefRW<T>>
        {
            private readonly ArchetypeChunk<T> _currentChunk;
            private int _localIndex;
            public RefRW<T> Current => new RefRW<T>(_currentChunk, _localIndex);

            internal RefRWEnumerator(ArchetypeChunk<T> chunk)
            {
                _currentChunk = chunk;
                _localIndex = -1;
            }

            public bool MoveNext()
            {
                var cur = _currentChunk;
                if (cur == null) return false;

                int idx = _localIndex + 1;
                if (idx < cur.Count)
                {
                    _localIndex = idx;
                    return true;
                }

                return false;
            }
        }
    }
}