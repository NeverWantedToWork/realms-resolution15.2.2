using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheHive : World
    {
        public TheHive()
        {
            Name = "The Hive";
            ClientWorldName = "The Hive";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.hive.jm", MapType.Json);
        }
    }
}