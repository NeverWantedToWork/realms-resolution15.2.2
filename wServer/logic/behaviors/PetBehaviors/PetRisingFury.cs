using Mono.Game;
using wServer.realm;
using System.Linq;
using wServer.realm.entities;
using System.Collections.Generic;
using wServer.networking.svrPackets;
using wServer.realm.entities.player;
using wServer.realm.worlds;
using wServer.realm;
using wServer.networking;
using MySql.Data.MySqlClient;
using wServer.networking.cliPackets;
using wServer.networking.svrPackets;

namespace wServer.logic.behaviors.PetBehaviors
{
    internal class PetRisingFury : Behavior
    {
        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = 0;
        }
        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            if (state == null) return;
            int cool = (int)state;

            if (cool <= 0)
            {
                PetLevel level = null;
                if (host is Pet)
                {
                    Pet p = host as Pet;
                    level = p.GetPetLevelFromAbility(Ability.RisingFury, true);
                }
                else
                {
                    return;
                }
                if (level == null) return;
                Position hpos = new Position { X = host.X, Y = host.Y };
                Enemy[] targets = host.GetNearestEntitieIsGroup(4, "AHLogic").OfType<Enemy>().ToArray();
                List<Packet> pkts = new List<Packet>();
                foreach (Enemy e in targets)
                {
                    pkts.Add(new ShowEffectPacket
                    {
                        EffectType = EffectType.Diffuse,
                        Color = new ARGB(0x006FFF00),
                        TargetId = host.Id,
                        PosA = hpos,
                        PosB = new Position { X = e.X + 4, Y = e.Y }
                    });
                    host.Owner.BroadcastPackets(pkts, null);


                    e.Damage(null, time, getDamage(host as Pet, level), true);

                }
                cool = getCooldown(host as Pet, level);
            }
            else
                cool -= time.thisTickTimes;

            state = cool;
        }
        private int getCooldown(Pet host, PetLevel type)
        {
            if (type.Level <= 30)
            {
                double cool = 7000;
                for (int i = 0; i < type.Level; i++)
                    cool -= 16.6666666666666;
                return (int)cool;
            }
            else if (type.Level > 89)
            {
                double cool = 3000;
                for (int i = 0; i < type.Level - 90; i++)
                    cool -= 40;
                return (int)cool;
            }
            else
            {
                double cool = 5000;
                for (int i = 0; i < type.Level - 30; i++)
                    cool -= 25;
                return (int)cool;
            }
        }
        private int getDamage(Pet host, PetLevel type)
        {
            if (type.Level <= 30)
            {
                return 45;
            }
            else if (type.Level > 89)
            {
                return 200;
            }
            else
            {
                return 100;
            }
        }
    }
}
