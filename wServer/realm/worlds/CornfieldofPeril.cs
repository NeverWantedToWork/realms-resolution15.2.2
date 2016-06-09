using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class CornfieldofPeril : World
    {
        public CornfieldofPeril()
        {
            Name = "Cornfield of Peril";
            ClientWorldName = "Cornfield of Peril";
            Background = 0;
            Difficulty = 4;
            AllowTeleport = false;
        }

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.cornfield.jm", MapType.Json);
        }
    }
}