using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class AldraginesHideout : World
    {
        public AldraginesHideout()
        {
            Name = "Aldragine's Hideout";
            ClientWorldName = "Aldragine's Hideout";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.aldragine.jm", MapType.Json);
        }
    }
}