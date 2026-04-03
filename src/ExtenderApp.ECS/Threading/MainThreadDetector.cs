namespace ExtenderApp.ECS.Threading
{
    /// <summary>
    /// 用于记录并校验「应用主线程」。
    ///
    /// 说明：
    /// - 宿主应在应用启动期（或主线程首次初始化时）调用 <see cref="Initialize"/> 记录主线程 Id；
    /// - 其它逻辑可通过 <see cref="IsMainThread"/> 判断当前线程是否为主线程；
    /// - 为保证线程安全，初始化使用原子操作避免竞态。
    /// </summary>
    internal static class MainThreadDetector
    {
        // 记录主线程的 ManagedThreadId；0 表示尚未初始化。
        private static int _mainThreadId;

        /// <summary>
        /// 指示是否已记录主线程 Id。
        /// </summary>
        public static bool IsInitialized => _mainThreadId != 0;

        /// <summary>
        /// 将当前线程登记为主线程；仅在首次调用时生效。
        /// </summary>
        /// <returns>若为首次登记成功则返回 true；若主线程已被登记则返回 false。</returns>
        public static bool Initialize()
        {
            var current = Thread.CurrentThread.ManagedThreadId;
            // 仅当 _mainThreadId 为 0 时写为 current；使用原子操作避免竞态。
            return Interlocked.CompareExchange(ref _mainThreadId, current, 0) == 0;
        }

        /// <summary>
        /// 判断当前线程是否为已登记的主线程。
        /// </summary>
        /// <returns>若已初始化且当前线程 Id 与登记的主线程 Id 相同则返回 true；否则返回 false。</returns>
        public static bool IsMainThread()
        {
            var id = _mainThreadId;
            if (id == 0) return false;
            return Thread.CurrentThread.ManagedThreadId == id;
        }

        /// <summary>
        /// 判断当前线程是否为主线程；若不是则抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        /// <exception cref="InvalidOperationException">只能在主线程调用该方法。</exception>
        public static void ThrowIfNotMainThread()
        {
            if (!IsMainThread())
                throw new InvalidOperationException("只能在主线程调用该方法。");
        }

        /// <summary>
        /// 强制将当前线程记录为主线程（覆盖已有记录），仅用于测试或合法边界下的线程同步。
        /// </summary>
        internal static void ForceSetCurrentThreadAsMain()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

#if DEBUG
        /// <summary>
        /// 清除已登记的主线程 Id（仅内部/测试使用）。
        /// </summary>
        internal static void Reset()
        {
            _mainThreadId = 0;
        }
#endif
    }
}
