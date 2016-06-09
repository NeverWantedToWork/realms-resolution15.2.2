using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheNontridus : World
    {
        public TheNontridus()
        {
            Name = "The Nontridus";
            ClientWorldName = "The Nontridus";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.nontridus.jm", MapType.Json);
        }
    }
}