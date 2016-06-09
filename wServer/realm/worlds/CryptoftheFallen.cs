using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class CryptoftheFallen : World
    {
        public CryptoftheFallen()
        {
            Name = "Crypt of the Fallen";
            ClientWorldName = "Crypt of the Fallen";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.cryptofthefallen.jm", MapType.Json);
        }
    }
}
