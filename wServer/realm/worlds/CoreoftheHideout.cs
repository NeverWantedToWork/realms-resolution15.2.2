using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class CoreoftheHideout : World
    {
        public CoreoftheHideout()
        {
            Name = "Core of the Hideout";
            ClientWorldName = "Core of the Hideout";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.core.jm", MapType.Json);
        }
    }
}