using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class AdminsArena : World
    {
        public AdminsArena()
        {
            Name = "Admins Arena";
            ClientWorldName = "Admins Arena";
            Background = 0;
            Difficulty = 0;
            AllowTeleport = true;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.adminsarena.jm", MapType.Json);
        }
    }
}