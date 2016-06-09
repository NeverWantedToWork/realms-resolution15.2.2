
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class LairofShaitan : World
    {
        public LairofShaitan()
        {
            Name = "Lair of Shaitan";
            ClientWorldName = "Lair of Shaitan";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }


        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.shaitan.jm", MapType.Json);
        }
    }
}
