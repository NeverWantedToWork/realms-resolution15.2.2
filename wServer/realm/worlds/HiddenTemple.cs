using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class HiddenTemple : World
    {
        public HiddenTemple()
        {
            Name = "Hidden Temple";
            ClientWorldName = "Hidden Temple";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.hiddentemple.jm", MapType.Json);
        }
    }
}
