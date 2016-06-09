using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class ConcealmentoftheDreadnought : World
    {
        public ConcealmentoftheDreadnought()
        {
            Name = "Concealment of the Dreadnought";
            ClientWorldName = "Concealment of the Dreadnought";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.concealment.jm", MapType.Json);
        }
    }
}
