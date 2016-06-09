using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TreasureofZol : World
    {
        public TreasureofZol()
        {
            Name = "Treasure of Zol";
            ClientWorldName = "Treasure of Zol";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.keepingtroom.jm", MapType.Json);
        }
    }
}