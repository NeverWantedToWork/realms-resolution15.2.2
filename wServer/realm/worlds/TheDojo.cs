using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheDojo : World
    {
        public TheDojo()
        {
            Name = "The Dojo";
            ClientWorldName = "The Dojo";
            Background = 0;
            Difficulty = 3;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.dojo.jm", MapType.Json);
        }
    }
}
