using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class DomainoftheDarkKnight : World
    {
        public DomainoftheDarkKnight()
        {
            Name = "Domain of the Dark Knight";
            ClientWorldName = "Domain of the Dark Knight";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.domain.jm", MapType.Json);
        }
    }
}
