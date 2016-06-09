using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class AbandonedBasement : World
    {
        public AbandonedBasement()
        {
            Name = "Abandoned Basement";
            ClientWorldName = "Abandoned Basement";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.basement.jm", MapType.Json);
        }
    }
}