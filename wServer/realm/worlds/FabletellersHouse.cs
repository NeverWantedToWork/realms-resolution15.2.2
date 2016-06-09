using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class FabletellersHouse : World
    {
        public FabletellersHouse()
        {
            Name = "Fableteller's House";
            ClientWorldName = "Fableteller's House";
            Background = 0;
            Difficulty = 0;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.fbhouse1.jm", MapType.Json);
        }
    }
}