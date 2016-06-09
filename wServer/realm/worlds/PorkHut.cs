using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class PorkHut : World
    {
        public PorkHut()
        {
            Name = "Pork Hut";
            ClientWorldName = "Pork Hut";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.porkhut.jm", MapType.Json);
        }
    }
}