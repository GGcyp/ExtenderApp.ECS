using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtenderApp.ECS.Worlds
{
    internal class WorldVersionManager
    {
        public ulong WorldVersion { get; private set; }
        public ulong ArchetypeVersion { get; private set; }

        public void IncrementWorldVersion()
        {
            WorldVersion++;
        }

        public void IncrementArchetypeVersion()
        {
            ArchetypeVersion++;
        }
    }
}