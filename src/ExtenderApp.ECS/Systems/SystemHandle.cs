using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtenderApp.ECS.Systems
{
    public struct SystemHandle
    {
        private readonly SystemCache _cache;

        public bool Enabled
        {
            get => _cache.Enabled;
            set => _cache.Enabled = value;
        }

        internal SystemHandle(SystemCache cache)
        {
            _cache = cache;
        }
    }
}