namespace MultiLock
{
    internal interface IMultiLock
    {
        public IDisposable AcquireLock(params string[] keys);
    }

    internal class UnMultiLock : IDisposable
    {
        private readonly IEnumerable<object> _locks;
        public UnMultiLock(IEnumerable<object> locks) => _locks = locks;

        public void Dispose()
        {
            foreach (var obj in _locks)
                if (Monitor.IsEntered(obj))
                    Monitor.Exit(obj);
        }
    }

    internal class MultiLock : IMultiLock
    {
        private readonly Dictionary<string, object> _keyLock = new Dictionary<string, object>();

        public IDisposable AcquireLock(params string[] keys)
        {
            try
            {
                foreach (var key in keys.OrderBy(w => w).ToList())
                {
                    lock (_keyLock)
                        if (!_keyLock.ContainsKey(key))
                            _keyLock[key] = new object();
                    Monitor.Enter(_keyLock[key]);
                }
                return new UnMultiLock(keys.Select(k => _keyLock[k]));
            }
            catch (ThreadAbortException)
            {
                var taken = keys.Where(Monitor.IsEntered).ToList();
                var index = 0;
                for (; index < taken.Count; index++)
                    Monitor.Exit(_keyLock[taken[index]]);

                throw;
            }
        }
    }
}