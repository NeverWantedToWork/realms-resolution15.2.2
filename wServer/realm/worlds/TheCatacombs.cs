using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheCatacombs : World
    {
        public TheCatacombs()
        {
            Name = "The Catacombs";
            ClientWorldName = "The Catacombs";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.catacombs.jm", MapType.Json);
        }
    }
}
