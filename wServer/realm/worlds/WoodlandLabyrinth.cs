﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class WoodlandLabyrinth : World
    {
        public WoodlandLabyrinth()
        {
            Name = "Woodland Labyrinth";
            ClientWorldName = "Woodland Labyrinth";
            Background = 0;
            Difficulty = 5;
            AllowTeleport = true;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            LoadMap("wServer.realm.worlds.maps.woodlandlabyrinth.jm", MapType.Json);
        }
    }
}
