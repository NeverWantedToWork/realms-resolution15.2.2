using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class KeepingofAldragine : World
    {
        public KeepingofAldragine()
        {
            Name = "Keeping of Aldragine";
            ClientWorldName = "Keeping of Aldragine";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.keeping.jm", MapType.Json);
        }
    }
}