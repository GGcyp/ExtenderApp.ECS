using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtenderApp.ECS.Systems
{
    public struct SystemHandle
    {
        private readonly SystemSchedulerContext _cache;


        internal SystemHandle(SystemSchedulerContext cache)
        {
            _cache = cache;
        }
    }
}