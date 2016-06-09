using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheStronghold : World
    {
        public TheStronghold()
        {
            Name = "The Stronghold";
            ClientWorldName = "The Stronghold";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = false;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            Random r = new Random();
            LoadMap("wServer.realm.worlds.maps.stronghold" + r.Next(1, 3 + 1).ToString() + ".jm", MapType.Json);
        }
    }
}
