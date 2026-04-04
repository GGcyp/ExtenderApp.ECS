using System.Runtime.CompilerServices;
using ExtenderApp.ECS;
using ExtenderApp.ECS.Archetypes;

namespace ExtenderApp.ECS.Components
{
    /// <summary>
    /// �������������������ڲ�ʹ�ã��� ʹ�÷��й��ڴ�� <see cref="Chunk" /> Ϊÿ������������ͱ��� 1 ��ʵ���� ������ȫ�ֹ�������Ƶ���¡���Ƶ��ȡ�����ݣ�����������ա�ȫ�����ÿ��ա�ʱ�����ŵȣ���
    ///
    /// Լ��������������ͱ���Ϊ struct�� �߳�ģ�ͣ�д�루Set/RemoveAt/Clear�����������̣߳���ȡ��TryGet/Get/Has�����������̵߳��á�
    /// </summary>
    internal sealed class SharedComponentManager : DisposableObject
    {
        private readonly ArchetypeChunk?[] _chunks;
        private SharedComponentMask _mask;

        public SharedComponentManager()
        {
            _chunks = new ArchetypeChunk[SharedComponentRegistry.MaxSharedCount];
            _mask = new();
        }

        /// <summary>
        /// �������ӹ�����������Ѵ�����ֱ�Ӹ��ǡ�
        /// </summary>
        public bool TryAddComponent<T>(in T value)
        {
            var type = SharedComponentType.Create<T>();
            if (_mask.On(type))
                return TrySetComponent(value);

            var chunk = ArchetypeChunkProvider.GetOrCreate<T>(true).Rent();
            chunk.Initialize(1);
            chunk.SetComponent(0, value);
            _chunks[GetArrayIndex(type)] = chunk;
            _mask.Add(type);
            return true;
        }

        /// <summary>
        /// ���Ը����Ѵ��ڵĹ��������
        /// </summary>
        public bool TrySetComponent<T>(in T value)
        {
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            int index = GetArrayIndex(type);
            var chunk = _chunks[index];
            if (chunk == null)
                return false;

            var chunkT = chunk as ArchetypeChunk<T>;
            chunkT?.SetComponent(0, value);
            return chunkT != null;
        }

        /// <summary>
        /// ���Զ�ȡ���������
        /// </summary>
        public bool TryGetComponent<T>(out T value)
        {
            value = default!;
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            var chunk = _chunks[GetArrayIndex(type)];
            if (chunk == null)
                return false;

            var chunkT = chunk as ArchetypeChunk<T>;
            if (chunkT == null)
                return false;

            value = chunkT.GetComponent(0);
            return true;
        }

        /// <summary>
        /// ���ù���������������������������򸲸ǡ�
        /// </summary>
        public void Set<T>(in T value)
        {
            if (!TryAddComponent(value))
            {
                TrySetComponent(value);
            }
        }

        /// <summary>
        /// ���Զ�ȡ���������������������
        /// </summary>
        public bool TryGet<T>(out T value) => TryGetComponent(out value);

        /// <summary>
        /// ��ȡ������������������׳��쳣��
        /// </summary>
        public T Get<T>()
        {
            if (TryGetComponent<T>(out var value))
                return value;

            throw new KeyNotFoundException($"������� {typeof(T).Name} �����ڡ�");
        }

        /// <summary>
        /// �Ƿ����ָ�����������
        /// </summary>
        public bool Has<T>() => _mask.On<T>();

        /// <summary>
        /// �Ƴ����������
        /// </summary>
        public bool Remove<T>()
        {
            var type = SharedComponentType.Create<T>();
            if (!_mask.On(type))
                return false;

            int index = GetArrayIndex(type);
            var chunk = _chunks[index];
            if (chunk != null)
            {
                chunk.Return();
                _chunks[index] = null;
            }

            _mask.Remove(type);
            return true;
        }

        /// <summary>
        /// ��ȡ��������������е��������� 0 ��ʼ�������� SharedComponentType �� TypeIndex �� 1 ��ʼ�������Ҫ�� 1��
        /// </summary>
        /// <param name="type">����������͡�</param>
        /// <returns>��������������е�������</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetArrayIndex(SharedComponentType type) => type.TypeIndex - 1;

        protected override void DisposeManagedResources()
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = _chunks[i];
                if (chunk != null)
                {
                    chunk.Return();
                    _chunks[i] = null;
                }
            }

            base.DisposeManagedResources();
        }
    }
}