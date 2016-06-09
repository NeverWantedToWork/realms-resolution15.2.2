﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class TheIvoryWyvern : World
    {
        public TheIvoryWyvern()
        {
            Name = "The Ivory Wyvern";
            ClientWorldName = "The Ivory Wyvern";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = false;
        }
        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.ivory.jm", MapType.Json);
        }
    }
}
