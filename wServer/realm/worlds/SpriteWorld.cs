using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.worlds
{
    public class SpriteWorld : World
    {
        public SpriteWorld()
        {
            Name = "Sprite World";
            ClientWorldName = "dungeons.Sprite_World";
            Background = 0;
            Difficulty = 2;
            AllowTeleport = true;
        }

        public override bool NeedsPortalKey => true;

        protected override void Init()
        {
            Random r = new Random();
            LoadMap("wServer.realm.worlds.maps.spriteworld" + r.Next(1, 5 + 1).ToString() + ".wmap", MapType.Wmap);
        }
    }
}
