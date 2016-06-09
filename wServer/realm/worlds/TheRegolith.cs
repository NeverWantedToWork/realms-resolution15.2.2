using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheRegolith : World
    {
        public TheRegolith()
        {
            Name = "The Regolith";
            ClientWorldName = "The Regolith";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.regolith.jm", MapType.Json);
        }
    }
}