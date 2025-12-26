using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using StarMap.Types.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMap
{
    internal class SoloGameFacade : IGameFacade
    {
        public Task<Any> RequestData(IMessage request)
        {
            return Task.FromResult(new Any());
        }
    }
}
