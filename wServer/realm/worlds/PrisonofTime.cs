using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class PrisonofTime : World
    {
        public PrisonofTime()
        {
            Name = "Prison of Time";
            ClientWorldName = "Prison of Time";
            Background = 1;
            Difficulty = 4;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.prison.jm", MapType.Json);
        }
    }
}