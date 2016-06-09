using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class SincryersGate : World
    {
        public SincryersGate()
        {
            Name = "Sincryer's Gate";
            ClientWorldName = "Sincryer's Gate";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.sincryersgate.jm", MapType.Json);
        }
    }
}