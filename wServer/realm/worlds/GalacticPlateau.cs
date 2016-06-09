using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class GalacticPlateau : World
    {
        public GalacticPlateau()
        {
            Name = "Galactic Plateau";
            ClientWorldName = "Galactic Plateau";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.gplateau.jm", MapType.Json);
        }
    }
}