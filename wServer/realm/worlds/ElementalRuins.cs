using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class ElementalRuins : World
    {
        public ElementalRuins()
        {
            Name = "Elemental Ruins";
            ClientWorldName = "Elemental Ruins";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = true;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.ruins.jm", MapType.Json);
        }
    }
}
