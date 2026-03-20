namespace ExtenderApp.ECS.Threading
{
    /// <summary>
    /// 用于记录并检测应用的“主线程”。
    ///
    /// 说明：
    /// - 在应用启动的早期（例如程序入口或框架初始化时）调用 <see cref="Initialize"/> 来记录主线程的 id。
    /// - 随后可以通过 <see cref="IsMainThread"/> 快速判断当前调用是否来自主线程。
    /// - 为保证线程安全，初始化使用原子操作，避免竞态条件。
    /// </summary>
    internal static class MainThreadDetector
    {
        // 记录主线程的 ManagedThreadId。0 表示未初始化。
        private static int _mainThreadId;

        /// <summary>
        /// 指示是否已经记录了主线程 id。
        /// </summary>
        public static bool IsInitialized => _mainThreadId != 0;

        /// <summary>
        /// 将当前线程设置为主线程（仅在首次调用时生效）。
        /// </summary>
        /// <returns>如果成功设置（这是第一次设置），返回 true；否则返回 false（已由其他线程设置）。</returns>
        public static bool Initialize()
        {
            var current = Thread.CurrentThread.ManagedThreadId;
            // 仅当 _mainThreadId 为 0 时设置为 current，使用原子操作避免竞态。
            return Interlocked.CompareExchange(ref _mainThreadId, current, 0) == 0;
        }

        /// <summary>
        /// 判断当前线程是否为已记录的主线程。
        /// </summary>
        /// <returns>如果已初始化且当前线程 id 与记录的主线程 id 相同则返回 true；否则返回 false。</returns>
        public static bool IsMainThread()
        {
            var id = _mainThreadId;
            if (id == 0) return false;
            return Thread.CurrentThread.ManagedThreadId == id;
        }

        /// <summary>
        /// 判断当前线程是否为主线程，如果不是则抛出 InvalidOperationException 异常。
        /// </summary>
        /// <exception cref="InvalidOperationException">"只能在主线程调用该方法。"</exception>
        public static void ThrowIfNotMainThread()
        {
            if (!IsMainThread())
                throw new InvalidOperationException("只能在主线程调用该方法。");
        }

#if DEBUG

        /// <summary>
        /// 强制将当前线程设置为主线程，覆盖之前的记录（仅在确有必要时使用，例如测试场景）。
        /// </summary>
        public static void ForceSetCurrentThreadAsMain()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 重置记录的主线程 id（内部/测试用）。
        /// </summary>
        internal static void Reset()
        {
            _mainThreadId = 0;
        }

#endif
    }
}