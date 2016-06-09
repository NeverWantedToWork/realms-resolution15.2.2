#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using db;
using MySql.Data.MySqlClient;
using wServer.networking;
using wServer.networking.cliPackets;
using wServer.networking.svrPackets;


#endregion

namespace wServer.realm.entities.player
{
    partial class Player
    {
        private static readonly ConditionEffect[] NegativeEffs =
        {
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Slowed,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Paralyzed,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Weak,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Stunned,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Confused,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Blind,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Quiet,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.ArmorBroken,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Bleeding,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Dazed,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Sick,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Drunk,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Hallucinating,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Hexed,
                DurationMS = 0
            },
            new ConditionEffect
            {
                Effect = ConditionEffectIndex.Unstable,
                DurationMS = 0
            }
        };

        public static int oldstat { get; set; }

        public static Position targetlink { get; set; }

        public static void ActivateHealHp(Player player, int amount, List<Packet> pkts)
        {
            int maxHp = player.Stats[0] + player.Boost[0];
            int newHp = Math.Min(maxHp, player.HP + amount);
            if (newHp != player.HP)
            {
                pkts.Add(new ShowEffectPacket
                {
                    EffectType = EffectType.Potion,
                    TargetId = player.Id,
                    Color = new ARGB(0xffffffff)
                });
                pkts.Add(new NotificationPacket
                {
                    Color = new ARGB(0xff00ff00),
                    ObjectId = player.Id,
                    Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"+" + (newHp - player.HP) + "\"}}"
                    //"+" + (newHp - player.HP)
                });
                player.HP = newHp;
                player.UpdateCount++;
            }
        }

        private static void ActivateHealMp(Player player, int amount, List<Packet> pkts)
        {
            int maxMp = player.Stats[1] + player.Boost[1];
            int newMp = Math.Min(maxMp, player.Mp + amount);
            if (newMp != player.Mp)
            {
                pkts.Add(new ShowEffectPacket
                {
                    EffectType = EffectType.Potion,
                    TargetId = player.Id,
                    Color = new ARGB(0x6084e0)
                });
                pkts.Add(new NotificationPacket
                {
                    Color = new ARGB(0x6084e0),
                    ObjectId = player.Id,
                    Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"+" + (newMp - player.Mp) + "\"}}"
                });
                player.Mp = newMp;
                player.UpdateCount++;
            }
        }

        private static void ActivateBoostStat(Player player, int idxnew, List<Packet> pkts)
        {
            var OriginalStat = 0;
            OriginalStat = player.Stats[idxnew] + OriginalStat;
            oldstat = OriginalStat;
        }

        private void ActivateShoot(RealmTime time, Item item, Position target)
        {
            double arcGap = item.ArcGap*Math.PI/180;
            double startAngle = Math.Atan2(target.Y - Y, target.X - X) - (item.NumProjectiles - 1)/2*arcGap;
            ProjectileDesc prjDesc = item.Projectiles[0]; //Assume only one

            for (int i = 0; i < item.NumProjectiles; i++)
            {
                Projectile proj = CreateProjectile(prjDesc, item.ObjectType,
                    (int) StatsManager.GetAttackDamage(prjDesc.MinDamage, prjDesc.MaxDamage),
                    time.tickTimes, new Position {X = X, Y = Y}, (float) (startAngle + arcGap*i));
                Owner.EnterWorld(proj);
                FameCounter.Shoot(proj);
            }
        }

        private void PoisonEnemy(Enemy enemy, ActivateEffect eff)
        {
            try
            {
                if (eff.ConditionEffect != null)
                    enemy.ApplyConditionEffect(new[]
                    {
                        new ConditionEffect
                        {
                            Effect = (ConditionEffectIndex) eff.ConditionEffect,
                            DurationMS = (int) eff.EffectDuration
                        }
                    });
                int remainingDmg = (int) StatsManager.GetDefenseDamage(enemy, eff.TotalDamage, enemy.ObjectDesc.Defense);
                int perDmg = remainingDmg*1000/eff.DurationMS;
                WorldTimer tmr = null;
                int x = 0;
                tmr = new WorldTimer(100, (w, t) =>
                {
                    if (enemy.Owner == null) return;
                    w.BroadcastPacket(new ShowEffectPacket
                    {
                        EffectType = EffectType.Dead,
                        TargetId = enemy.Id,
                        Color = new ARGB(0xffddff00)
                    }, null);

                    if (x%10 == 0)
                    {
                        int thisDmg;
                        if (remainingDmg < perDmg) thisDmg = remainingDmg;
                        else thisDmg = perDmg;

                        enemy.Damage(this, t, thisDmg, true);
                        remainingDmg -= thisDmg;
                        if (remainingDmg <= 0) return;
                    }
                    x++;

                    tmr.Reset();

                    Manager.Logic.AddPendingAction(_ => w.Timers.Add(tmr), PendingPriority.Creation);
                });
                Owner.Timers.Add(tmr);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public bool Activate(RealmTime time, Item item, UseItemPacket pkt)
        {
            bool endMethod = false;
            Position target = pkt.ItemUsePos;
            Mp -= item.MpCost;
            if (HP < item.HpCost) HP = item.HpCost;
            else
                HP -= item.HpCost;
            IContainer con = Owner.GetEntity(pkt.SlotObject.ObjectId) as IContainer;
            if(con == null) return true;

            if (pkt.SlotObject.SlotId != 255 && pkt.SlotObject.SlotId != 254 && con.Inventory[pkt.SlotObject.SlotId] != item)
            {
                log.FatalFormat("Cheat engine detected for player {0},\nItem should be {1}, but its {2}.",
                    Name, Inventory[pkt.SlotObject.SlotId].ObjectId, item.ObjectId);
                foreach (Player player in Owner.Players.Values)
                    if (player.Client.Account.Rank >= 2)
                        player.SendInfo(String.Format("Cheat engine detected for player {0},\nItem should be {1}, but its {2}.",
                    Name, Inventory[pkt.SlotObject.SlotId].ObjectId, item.ObjectId));
                Client.Disconnect();
                return true;
            }

            if (item.IsBackpack)
            {
                if (HasBackpack != 0) return true;
                Client.Character.Backpack = new [] {-1, -1, -1, -1, -1, -1, -1, -1};
                HasBackpack = 1;
                Client.Character.HasBackpack = 1;
                Manager.Database.DoActionAsync(db =>
                    db.SaveBackpacks(Client.Character, Client.Account));
                Array.Resize(ref inventory, 20);
                int[] slotTypes =
                    Utils.FromCommaSepString32(
                        Manager.GameData.ObjectTypeToElement[ObjectType].Element("SlotTypes").Value);
                Array.Resize(ref slotTypes, 20);
                for (int i = 0; i < slotTypes.Length; i++)
                    if (slotTypes[i] == 0) slotTypes[i] = 10;
                SlotTypes = slotTypes;
                return false;
            }
            if (item.XpBooster)
            {
                if (!XpBoosted)
                {
                    XpBoostTimeLeft = (float)item.Timer;
                    XpBoosted = item.XpBooster;
                    xpFreeTimer = (float)item.Timer == -1.0 ? false : true;
                    return false;
                }
                else
                {
                    SendInfo("You have already an active XP Booster.");
                    return true;
                }
            }

            if (item.LootDropBooster)
            {
                if (!LootDropBoost)
                {
                    LootDropBoostTimeLeft = (float)item.Timer;
                    lootDropBoostFreeTimer = (float)item.Timer == -1.0 ? false : true;
                    return false;
                }
                else
                {
                    SendInfo("You have already an active Loot Drop Booster.");
                    return true;
                }
            }

            if (item.LootTierBooster)
            {
                if (!LootTierBoost)
                {
                    LootTierBoostTimeLeft = (float)item.Timer;
                    lootTierBoostFreeTimer = (float)item.Timer == -1.0 ? false : true;
                    return false;
                }
                else
                {
                    SendInfo("You have already an active Loot Tier Booster.");
                    return true;
                }
            }

            foreach (ActivateEffect eff in item.ActivateEffects)
            {
                switch (eff.Effect)
                {
                    case ActivateEffects.BulletNova:
                    {
                        ProjectileDesc prjDesc = item.Projectiles[0]; //Assume only one
                        Packet[] batch = new Packet[21];
                        uint s = Random.CurrentSeed;
                        Random.CurrentSeed = (uint)(s * time.tickTimes);
                        for (int i = 0; i < 20; i++)
                        {
                            Projectile proj = CreateProjectile(prjDesc, item.ObjectType,
                                (int)StatsManager.GetAttackDamage(prjDesc.MinDamage, prjDesc.MaxDamage),
                                time.tickTimes, target, (float)(i * (Math.PI * 2) / 20));
                            Owner.EnterWorld(proj);
                            FameCounter.Shoot(proj);
                            batch[i] = new Shoot2Packet()
                            {
                                BulletId = proj.ProjectileId,
                                OwnerId = Id,
                                ContainerType = item.ObjectType,
                                StartingPos = target,
                                Angle = proj.Angle,
                                Damage = (short)proj.Damage
                            };
                        }
                        Random.CurrentSeed = s;
                        batch[20] = new ShowEffectPacket()
                        {
                            EffectType = EffectType.Trail,
                            PosA = target,
                            TargetId = Id,
                            Color = new ARGB(0xFFFF00AA)
                        };
                        BroadcastSync(batch, p => this.Dist(p) < 35);
                    } break;
                    case ActivateEffects.Shoot:
                    {
                        ActivateShoot(time, item, target);
                    }
                        break;

                    case ActivateEffects.StatBoostSelf:
                    {
                        int idx = -1;

                        if (eff.Stats == StatsType.MaximumHP) idx = 0;
                        else if (eff.Stats == StatsType.MaximumMP) idx = 1;
                        else if (eff.Stats == StatsType.Attack) idx = 2;
                        else if (eff.Stats == StatsType.Defense) idx = 3;
                        else if (eff.Stats == StatsType.Speed) idx = 4;
                        else if (eff.Stats == StatsType.Vitality) idx = 5;
                        else if (eff.Stats == StatsType.Wisdom) idx = 6;
                        else if (eff.Stats == StatsType.Dexterity) idx = 7;

                        List<Packet> pkts = new List<Packet>();

                        ActivateBoostStat(this, idx, pkts);
                        int OGstat = oldstat;
                        int bit = idx + 39;

                        int s = eff.Amount;
                        Boost[idx] += s;
                        ApplyConditionEffect(new ConditionEffect
                        {
                            DurationMS = eff.DurationMS,
                            Effect = (ConditionEffectIndex)bit
                        });
                        UpdateCount++;
                        Owner.Timers.Add(new WorldTimer(eff.DurationMS, (world, t) =>
                        {
                            Boost[idx] = OGstat;
                            UpdateCount++;
                        }));
                        Owner.BroadcastPacket(new ShowEffectPacket
                        {
                            EffectType = EffectType.Potion,
                            TargetId = Id,
                            Color = new ARGB(0xffffffff)
                        }, null);
                    }
                        break;

                    case ActivateEffects.StatBoostAura:
                        {
                            int idx = -1;

                            if(eff.Stats == StatsType.MaximumHP) idx = 0;
                            if(eff.Stats == StatsType.MaximumMP) idx = 1;
                            if(eff.Stats == StatsType.Attack) idx = 2;
                            if(eff.Stats == StatsType.Defense) idx = 3;
                            if(eff.Stats == StatsType.Speed) idx = 4;
                            if(eff.Stats == StatsType.Vitality) idx = 5;
                            if(eff.Stats == StatsType.Wisdom) idx = 6;
                            if(eff.Stats == StatsType.Dexterity) idx = 7;
                            
                            int bit = idx + 39;

                            var amountSBA = eff.Amount;
                            var durationSBA = eff.DurationMS;
                            var rangeSBA = eff.Range;
                            if (eff.UseWisMod)
                            {
                                amountSBA = (int)UseWisMod(eff.Amount, 0);
                                durationSBA = (int)(UseWisMod(eff.DurationSec) * 1000);
                                rangeSBA = UseWisMod(eff.Range);
                            }
                            
                            this.Aoe(rangeSBA, true, player =>
                            {
                                // TODO support for noStack StatBoostAura attribute (paladin total hp increase / insta heal)
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    DurationMS = durationSBA,
                                    Effect = (ConditionEffectIndex)bit
                                });
                                (player as Player).Boost[idx] += amountSBA;
                                player.UpdateCount++;
                                Owner.Timers.Add(new WorldTimer(durationSBA, (world, t) =>
                                {
                                    (player as Player).Boost[idx] -= amountSBA;
                                    player.UpdateCount++;
                                }));
                            });
                            BroadcastSync(new ShowEffectPacket()
                            {
                                EffectType = EffectType.AreaBlast,
                                TargetId = Id,
                                Color = new ARGB(0xffffffff),
                                PosA = new Position() { X = rangeSBA }
                            }, p => this.Dist(p) < 25);
                        } break;

                    case ActivateEffects.ConditionEffectSelf:
                    {
                        var durationCES = eff.DurationMS;
                        if (eff.UseWisMod)
                            durationCES = (int) (UseWisMod(eff.DurationSec)*1000);

                        var color = 0xffffffff;
                        switch (eff.ConditionEffect.Value)
                        {
                            case ConditionEffectIndex.Damaging:
                                color = 0xffff0000;
                                break;
                            case ConditionEffectIndex.Berserk:
                                color = 0x808080;
                                break;
                        }

                        ApplyConditionEffect(new ConditionEffect
                        {
                            Effect = eff.ConditionEffect.Value,
                            DurationMS = durationCES
                        });
                        Owner.BroadcastPacket(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(color),
                            PosA = new Position {X = 2F}
                        }, null);
                    }
                        break;

                    case ActivateEffects.ConditionEffectAura:
                    {
                        var durationCEA = eff.DurationMS;
                        var rangeCEA = eff.Range;
                        if (eff.UseWisMod)
                        {
                            durationCEA = (int)(UseWisMod(eff.DurationSec) * 1000);
                            rangeCEA = UseWisMod(eff.Range);
                        }

                        this.Aoe(rangeCEA, true, player =>
                        {
                            player.ApplyConditionEffect(new ConditionEffect
                            {
                                Effect = eff.ConditionEffect.Value,
                                DurationMS = durationCEA
                            });
                        });

                        var color = 0xffffffff;
                        switch (eff.ConditionEffect.Value)
                        {
                            case ConditionEffectIndex.Damaging:
                                color = 0xffff0000;
                                break;
                            case ConditionEffectIndex.Berserk:
                                color = 0x808080;
                                break;
                        }

                        BroadcastSync(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(color),
                            PosA = new Position {X = rangeCEA}
                        }, p => this.Dist(p) < 25);
                    }
                        break;

                    case ActivateEffects.Heal:
                    {
                        List<Packet> pkts = new List<Packet>();
                        ActivateHealHp(this, eff.Amount, pkts);
                        Owner.BroadcastPackets(pkts, null);
                    }
                        break;

                    case ActivateEffects.HealNova:
                    {
                        var amountHN = eff.Amount;
                        var rangeHN = eff.Range;
                        if (eff.UseWisMod)
                        {
                            amountHN = (int)UseWisMod(eff.Amount, 0);
                            rangeHN = UseWisMod(eff.Range);
                        }
                        
                        List<Packet> pkts = new List<Packet>();
                        this.Aoe(rangeHN, true, player => { ActivateHealHp(player as Player, amountHN, pkts); });
                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(0xffffffff),
                            PosA = new Position {X = rangeHN}
                        });
                        BroadcastSync(pkts, p => this.Dist(p) < 25);
                    }
                        break;
                    case ActivateEffects.HealNovaSigil:
                        {
                            ActivateShoot(time, item, target);
                            var amountHN = eff.Amount;
                            var rangeHN = eff.Range;
                            if (eff.UseWisMod)
                            {
                                amountHN = (int)UseWisMod(eff.Amount, 0);
                                rangeHN = UseWisMod(eff.Range);
                            }

                            List<Packet> pkts = new List<Packet>();
                            this.Aoe(rangeHN, true, player => { ActivateHealHp(player as Player, amountHN, pkts); });
                            pkts.Add(new ShowEffectPacket
                            {
                                EffectType = EffectType.AreaBlast,
                                TargetId = Id,
                                Color = new ARGB(0xffffffff),
                                PosA = new Position { X = rangeHN }
                            });
                            BroadcastSync(pkts, p => this.Dist(p) < 25);
                        }
                        break;

                    case ActivateEffects.Magic:
                    {
                        List<Packet> pkts = new List<Packet>();
                        ActivateHealMp(this, eff.Amount, pkts);
                        Owner.BroadcastPackets(pkts, null);
                    }
                        break;

                    case ActivateEffects.MagicNova:
                    {
                        List<Packet> pkts = new List<Packet>();
                        this.Aoe(eff.Range/2, true, player => { ActivateHealMp(player as Player, eff.Amount, pkts); });
                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(0xffffffff),
                            PosA = new Position {X = eff.Range}
                        });
                        Owner.BroadcastPackets(pkts, null);
                    }
                        break;

                    case ActivateEffects.Teleport:
                    {
                        Move(target.X, target.Y);
                        UpdateCount++;
                        Owner.BroadcastPackets(new Packet[]
                        {
                            new GotoPacket
                            {
                                ObjectId = Id,
                                Position = new Position
                                {
                                    X = X,
                                    Y = Y
                                }
                            },
                            new ShowEffectPacket
                            {
                                EffectType = EffectType.Teleport,
                                TargetId = Id,
                                PosA = new Position
                                {
                                    X = X,
                                    Y = Y
                                },
                                Color = new ARGB(0xFFFFFFFF)
                            }
                        }, null);
                    }
                        break;

                    case ActivateEffects.VampireBlast:
                    {
                        List<Packet> pkts = new List<Packet>();
                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.Trail,
                            TargetId = Id,
                            PosA = target,
                            Color = new ARGB(0xFFFF0000)
                        });
                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.Diffuse,
                            Color = new ARGB(0xFFFF0000),
                            TargetId = Id,
                            PosA = target,
                            PosB = new Position {X = target.X + eff.Radius, Y = target.Y}
                        });

                        int totalDmg = 0;
                        List<Enemy> enemies = new List<Enemy>();
                        Owner.Aoe(target, eff.Radius, false, enemy =>
                        {
                            enemies.Add(enemy as Enemy);
                            totalDmg += (enemy as Enemy).Damage(this, time, eff.TotalDamage, false);
                        });
                        List<Player> players = new List<Player>();
                        this.Aoe(eff.Radius, true, player =>
                        {
                            players.Add(player as Player);
                            ActivateHealHp(player as Player, totalDmg, pkts);
                        });

                        if (enemies.Count > 0)
                        {
                            Random rand = new Random();
                            for (int i = 0; i < 5; i++)
                            {
                                Enemy a = enemies[rand.Next(0, enemies.Count)];
                                Player b = players[rand.Next(0, players.Count)];
                                pkts.Add(new ShowEffectPacket
                                {
                                    EffectType = EffectType.Flow,
                                    TargetId = b.Id,
                                    PosA = new Position {X = a.X, Y = a.Y},
                                    Color = new ARGB(0xffffffff)
                                });
                            }
                        }

                        BroadcastSync(pkts, p => this.Dist(p) < 25);
                    }
                        break;
                    case ActivateEffects.Trap:
                    {
                        BroadcastSync(new ShowEffectPacket
                        {
                            EffectType = EffectType.Throw,
                            Color = new ARGB(0xff9000ff),
                            TargetId = Id,
                            PosA = target
                        }, p => this.Dist(p) < 25);
                        Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
                        {
                            Trap trap = new Trap(
                                this,
                                eff.Radius,
                                eff.TotalDamage,
                                eff.ConditionEffect ?? ConditionEffectIndex.Slowed,
                                eff.EffectDuration);
                            trap.Move(target.X, target.Y);
                            world.EnterWorld(trap);
                        }));
                    }
                        break;

                    case ActivateEffects.RoyalTrap:
                        {
                            BroadcastSync(new ShowEffectPacket
                            {
                                EffectType = EffectType.Throw,
                                Color = new ARGB(0xff9900),
                                TargetId = Id,
                                PosA = target
                            }, p => this.Dist(p) < 25);
                            Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
                            {
                                Trap trap = new Trap(
                                    this,
                                    eff.Radius,
                                    eff.TotalDamage,
                                    eff.ConditionEffect ?? ConditionEffectIndex.Slowed,
                                    eff.EffectDuration);
                                trap.Move(target.X, target.Y);
                                world.EnterWorld(trap);
                            }));
                        }
                        break;

                    case ActivateEffects.StasisBlast:
                    {
                        List<Packet> pkts = new List<Packet>();

                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.Concentrate,
                            TargetId = Id,
                            PosA = target,
                            PosB = new Position {X = target.X + 3, Y = target.Y},
                            Color = new ARGB(0xFF00D0)
                        });
                        Owner.Aoe(target, 3, false, enemy =>
                        {
                            if (IsSpecial(enemy.ObjectType)) return;

                            if (enemy.HasConditionEffect(ConditionEffectIndex.StasisImmune))
                            {
                                if (!enemy.HasConditionEffect(ConditionEffectIndex.Invincible))
                                {
                                    pkts.Add(new NotificationPacket
                                    {
                                        ObjectId = enemy.Id,
                                        Color = new ARGB(0xff00ff00),
                                        Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"Immune\"}}"
                                    });
                                }
                            }
                            else if (!enemy.HasConditionEffect(ConditionEffectIndex.Stasis))
                            {
                                enemy.ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Stasis,
                                    DurationMS = eff.DurationMS
                                });
                                Owner.Timers.Add(new WorldTimer(eff.DurationMS, (world, t) =>
                                {
                                    enemy.ApplyConditionEffect(new ConditionEffect
                                    {
                                        Effect = ConditionEffectIndex.StasisImmune,
                                        DurationMS = 3000
                                    });
                                }));
                                pkts.Add(new NotificationPacket
                                {
                                    ObjectId = enemy.Id,
                                    Color = new ARGB(0xffff0000),
                                    Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"Stasis\"}}"
                                });
                            }
                        });
                        BroadcastSync(pkts, p => this.Dist(p) < 25);
                    }
                        break;
                        case ActivateEffects.BigStasisBlast:
                    {
                        List<Packet> pkts = new List<Packet>();

                        pkts.Add(new ShowEffectPacket
                        {
                            EffectType = EffectType.Concentrate,
                            TargetId = Id,
                            PosA = target,
                            PosB = new Position {X = target.X + 6, Y = target.Y},
                            Color = new ARGB(0x00FF00)
                        });
                        Owner.Aoe(target, 6, false, enemy =>
                        {
                            if (IsSpecial(enemy.ObjectType)) return;

                            if (enemy.HasConditionEffect(ConditionEffectIndex.StasisImmune))
                            {
                                if (!enemy.HasConditionEffect(ConditionEffectIndex.Invincible))
                                {
                                    pkts.Add(new NotificationPacket
                                    {
                                        ObjectId = enemy.Id,
                                        Color = new ARGB(0xff00ff00),
                                        Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"Immune\"}}"
                                    });
                                }
                            }
                            else if (!enemy.HasConditionEffect(ConditionEffectIndex.Stasis))
                            {
                                enemy.ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Stasis,
                                    DurationMS = eff.DurationMS
                                });
                                Owner.Timers.Add(new WorldTimer(eff.DurationMS, (world, t) =>
                                {
                                    enemy.ApplyConditionEffect(new ConditionEffect
                                    {
                                        Effect = ConditionEffectIndex.StasisImmune,
                                        DurationMS = 3000
                                    });
                                }));
                                pkts.Add(new NotificationPacket
                                {
                                    ObjectId = enemy.Id,
                                    Color = new ARGB(0xffff0000),
                                    Text = "{\"key\":\"blank\",\"tokens\":{\"data\":\"Stasis\"}}"
                                });
                            }
                        });
                        BroadcastSync(pkts, p => this.Dist(p) < 25);
                    }
                        break;
                    case ActivateEffects.Decoy:
                    {
                        Decoy decoy = new Decoy(Manager, this, eff.DurationMS, StatsManager.GetSpeed());
                        decoy.Move(X, Y);
                        Owner.EnterWorld(decoy);
                    }
                        break;

                    case ActivateEffects.Lightning:
                    {
                        Enemy start = null;
                        double angle = Math.Atan2(target.Y - Y, target.X - X);
                        double diff = Math.PI/3;
                        Owner.Aoe(target, 6, false, enemy =>
                        {
                            if (!(enemy is Enemy)) return;
                            double x = Math.Atan2(enemy.Y - Y, enemy.X - X);
                            if (Math.Abs(angle - x) < diff)
                            {
                                start = enemy as Enemy;
                                diff = Math.Abs(angle - x);
                            }
                        });
                        if (start == null)
                            break;

                        Enemy current = start;
                        Enemy[] targets = new Enemy[eff.MaxTargets];
                        for (int i = 0; i < targets.Length; i++)
                        {
                            targets[i] = current;
                            Enemy next = current.GetNearestEntity(8, false,
                                enemy =>
                                    enemy is Enemy &&
                                    Array.IndexOf(targets, enemy) == -1 &&
                                    this.Dist(enemy) <= 6) as Enemy;

                            if (next == null) break;
                            current = next;
                        }

                        List<Packet> pkts = new List<Packet>();
                        for (int i = 0; i < targets.Length; i++)
                        {
                            if (targets[i] == null) break;
                            if(targets[i].HasConditionEffect(ConditionEffectIndex.Invincible)) continue;
                            Entity prev = i == 0 ? (Entity) this : targets[i - 1];
                            targets[i].Damage(this, time, eff.TotalDamage, false);
                            if (eff.ConditionEffect != null)
                                targets[i].ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = eff.ConditionEffect.Value,
                                    DurationMS = (int) (eff.EffectDuration*1000)
                                });
                            pkts.Add(new ShowEffectPacket
                            {
                                EffectType = EffectType.Lightning,
                                TargetId = prev.Id,
                                Color = new ARGB(0xffff0088),
                                PosA = new Position
                                {
                                    X = targets[i].X,
                                    Y = targets[i].Y
                                },
                                PosB = new Position {X = 350}
                            });
                        }
                        BroadcastSync(pkts, p => this.Dist(p) < 25);
                    }
                        break;

                    case ActivateEffects.PoisonGrenade:
                    {
                        try
                        {
                            BroadcastSync(new ShowEffectPacket
                            {
                                EffectType = EffectType.Throw,
                                Color = new ARGB(0xffddff00),
                                TargetId = Id,
                                PosA = target
                            }, p => this.Dist(p) < 25);
                            Placeholder x = new Placeholder(Manager, 1500);
                            x.Move(target.X, target.Y);
                            Owner.EnterWorld(x);
                            try
                            {
                                Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
                                {
                                    world.BroadcastPacket(new ShowEffectPacket
                                    {
                                        EffectType = EffectType.AreaBlast,
                                        Color = new ARGB(0xffddff00),
                                        TargetId = x.Id,
                                        PosA = new Position { X = eff.Radius }
                                    }, null);
                                    world.Aoe(target, eff.Radius, false,
                                        enemy => PoisonEnemy(enemy as Enemy, eff));
                                }));
                            }
                            catch (Exception ex)
                            {
                                log.ErrorFormat("Poison ShowEffect:\n{0}", ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.ErrorFormat("Poisons General:\n{0}", ex);
                        }
                    }
                        break;
                    case ActivateEffects.RemoveNegativeConditions:
                    {
                        this.Aoe(eff.Range/2, true, player => { ApplyConditionEffect(NegativeEffs); });
                        BroadcastSync(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(0xffffffff),
                            PosA = new Position {X = eff.Range/2}
                        }, p => this.Dist(p) < 25);
                    }
                        break;

                    case ActivateEffects.RemoveNegativeConditionsSelf:
                    {
                        ApplyConditionEffect(NegativeEffs);
                        Owner.BroadcastPacket(new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetId = Id,
                            Color = new ARGB(0xffffffff),
                            PosA = new Position {X = 1}
                        }, null);
                    }
                        break;

                    case ActivateEffects.IncrementStat:
                    {
                        int idx = -1;

                        if (eff.Stats == StatsType.MaximumHP) idx = 0;
                        else if (eff.Stats == StatsType.MaximumMP) idx = 1;
                        else if (eff.Stats == StatsType.Attack) idx = 2;
                        else if (eff.Stats == StatsType.Defense) idx = 3;
                        else if (eff.Stats == StatsType.Speed) idx = 4;
                        else if (eff.Stats == StatsType.Vitality) idx = 5;
                        else if (eff.Stats == StatsType.Wisdom) idx = 6;
                        else if (eff.Stats == StatsType.Dexterity) idx = 7;

                        Stats[idx] += eff.Amount;
                        int limit =
                            int.Parse(
                                Manager.GameData.ObjectTypeToElement[ObjectType].Element(
                                    StatsManager.StatsIndexToName(idx))
                                    .Attribute("max")
                                    .Value);
                        if (Stats[idx] > limit)
                            Stats[idx] = limit;
                        UpdateCount++;
                    }
                        break;
                    case ActivateEffects.OPBUFF:
                        {
                            if (!ninjaShoot)
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Damaging,
                                    DurationMS = -1
                                });
                                ninjaFreeTimer = true;
                                ninjaShoot = true;
                            }
                            else
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Armored,
                                    DurationMS = -1
                                });
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Damaging,
                                    DurationMS = 0
                                });
                                ushort obj;
                                Manager.GameData.IdToObjectType.TryGetValue(item.ObjectId, out obj);
                                if (Mp >= item.MpEndCost)
                                {
                                    ActivateShoot(time, item, pkt.ItemUsePos);
                                    Mp -= (int)item.MpEndCost;
                                }
                                targetlink = target;
                                ninjaShoot = false;
                            }
                        }
                        break;
                    case ActivateEffects.TreasureActivate:
                        {

                            var db = new db.Database();
                            int a = eff.Amount;

                            Credits = db.UpdateCredit(Client.Account, a);
                            UpdateCount++;
                            db.Dispose();
                        }
                        break;
                    case ActivateEffects.FameActivate:
                        {

                            var db = new db.Database();
                            int a = eff.Amount;

                            CurrentFame = db.UpdateFame(Client.Account, a);
                            UpdateCount++;
                            db.Dispose();
                        }
                        break;
                    case ActivateEffects.SilentBox:
                        {
                            int LockboxChance = Random.Next(0, 20);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Dagger of Foul Malevolence!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Elemental Detonation Spell!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb23];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Apparitions!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb29];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Helm of the Great General");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Giantcatcher Trap");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb26];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Seal of the Blessed Champion");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb25];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Tome of Holy Guidance");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2372];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Excellent! You've obtained a Ghost Huntress Skin!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Ghost Huntress Skin from the Premium SilentLore Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5229];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Awesome! You've obtained a Fate Wand!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Fate Wand from the Premium SilentLore Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5228];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Dagger of Uranium!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Dagger of Uranium from the Premium SilentLore Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5227];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Wowtastic! You've obtained a Tome of Wild Wreckage!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Tome of Wild Wreckage from the Premium SilentLore Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                            }

                        }
                        break;
                    case ActivateEffects.RageReapBox:
                        {
                            int LockboxChance = Random.Next(0, 20);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Dagger of Foul Malevolence!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Elemental Detonation Spell!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb23];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Apparitions!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb29];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Helm of the Great General");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Giantcatcher Trap");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb26];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Seal of the Blessed Champion");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb25];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Tome of Holy Guidance");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5091];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Excellent! You've obtained a Spell of Sorrow!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Spell of Sorrow from the Premium RageReap Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x521c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Awesome! You've obtained a Scythe of Grim Memories!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Scythe of Grim Memories from the Premium RageReap Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x521e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Sword of Fierce Force!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Sword of Fierce Force from the Premium RageReap Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1599];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Spectactular! You've obtained a Battleplate of Sacred Warlords!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Battleplate of Sacred Warlords from the Premium RageReap Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                            }

                        }
                        break;
                    case ActivateEffects.AsiimovBox:
                        {
                            int LockboxChance = Random.Next(0, 40);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa89];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Emeraldshard Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1853];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("OH MY FUCKING GOD! KNEIF! KNEIF!!! KNEIFF! KNEEEIF!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Ring of Asiimov from the Asiimov Lockbox."
                                            };
                                           Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1200];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Purple Pinstripe Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1850];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("OH MY FUCKING GOD! KNEIF! KNEIF!!! KNEIFF! KNEEEIF!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Wand of Asiimov from the Asiimov Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1851];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("OH MY FUCKING GOD! KNEIF! KNEIF!!! KNEIFF! KNEEEIF!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Tome of Asiimov from the Asiimov Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Asiimov Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1852];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("OH MY FUCKING GOD! KNEIF! KNEIF!!! KNEIFF! KNEEEIF!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Robe of Asiimov from the Asiimov Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                            }

                        }
                        break;




                    case ActivateEffects.UnScroll:
                        {
                           
                            List<Packet> pkts = new List<Packet>();
                            int ScrollChance = Random.Next(0, 4);
                          
                        switch (ScrollChance)
                        {
                                case 0:
                                    
                                    this.Aoe(6, true, player => { ActivateHealMp(player as Player, 300, pkts); });
                                    pkts.Add(new ShowEffectPacket
                                    {
                                        EffectType = EffectType.AreaBlast,
                                        TargetId = Id,
                                        Color = new ARGB(0x00000fff),
                                        PosA = new Position { X = 6 }
                                    });
                                    Owner.BroadcastPackets(pkts, null);
                                    SendInfo("An area around you is magical!");

                                    break;
                                case 1:

                                    this.Aoe(6, true, player => { ActivateHealHp(player as Player, 300, pkts); });
                                    pkts.Add(new ShowEffectPacket
                                    {
                                        EffectType = EffectType.AreaBlast,
                                        TargetId = Id,
                                        Color = new ARGB(0xffffffff),
                                        PosA = new Position { X = 6 }
                                    });
                                    BroadcastSync(pkts, p => this.Dist(p) < 25);
                                    SendInfo("You heal everybody around you!");
                                    break;
                                case 2:
                                    SendInfo("The scroll did absolutely nothing.");
                                    break;
                                case 3:
                                    HP -= 100;
                                    SendInfo("Your hp lowered by 100!");
                                    break;
                                case 4:
                                   Mp -= 100;
                                    SendInfo("Your mp lowered by 100!");
                                    break;
                                case 5:
                                    Mp += 100;
                                    SendInfo("Your mp increased by 100!");
                                    break;
                                case 6:
                                    HP += 100;
                                    SendInfo("Your Hp increased by 100!");
                                    break;
                            }

                        }
                        break;

                    case ActivateEffects.BlackScroll:
                        {
                            

                            Move(Quest.X + 1.0f, Quest.Y + 1.0f);
                            if (Pet != null)
                                Pet.Move(Quest.X + 1.0f, Quest.Y + 1.0f);
                            UpdateCount++;
                            Owner.BroadcastPacket(new GotoPacket
                            {
                                ObjectId = Id,
                                Position = new Position
                                {
                                    X = Quest.X,
                                    Y = Quest.Y
                                }
                            }, null);

                        }
                        break;
                    case ActivateEffects.OvergrowthBox:
                        {
                            int LockboxChance = Random.Next(0, 62);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa89];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Emeraldshard Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5097];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wow! You obtained the Bow of Lost Promises");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Bow of Lost Promises from the Overgrowth Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1200];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Purple Pinstripe Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x178b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Fantastic. You obtained the Robe of Overgrowth.");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Robe of Overgrowth from the Overgrowth Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x177d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Katana of the Vicious Plot!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Katana of the Vicious Plot from the Overgrowth Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x179c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wonderful! You obtained Petrification Cloak!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Petrification Cloak from the Overgrowth Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x748C];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Ice Cave Key");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc88];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You have unboxed a Legendary Feline Egg");
                                            return false;
                                        }
                                    };
                                    break;
                                case 48:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc8c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You have unboxed a Legendary Canine Egg");
                                            return false;
                                        }
                                    };
                                    break;
                                case 49:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2290];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bella's Key");
                                            return false;
                                        }
                                    };
                                    break;
                                case 50:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2290];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bella's Key");
                                            return false;
                                        }
                                    };
                                    break;
                                case 51:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2290];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bella's Key");
                                            return false;
                                        }
                                    };
                                    break;
                                case 52:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2290];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bella's Key");
                                            return false;
                                        }
                                    };
                                    break;
                                case 53:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 54:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 55:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 56:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 57:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 58:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 59:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 60:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 61:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Overgrowth Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1638];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You just obtained a Helm of the Macrotitans!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Helm of the Macrotitans from the Overgrowth Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;


                            }

                        }
                        break;
                   case ActivateEffects.SpiderTrap:
                        {
                            BroadcastSync(new ShowEffectPacket
                            {
                                EffectType = EffectType.Throw,
                                Color = new ARGB(0xFFFFFF),
                                TargetId = Id,
                                PosA = target
                            }, p => this.Dist(p) < 25);
                            Owner.Timers.Add(new WorldTimer(1500, (world, t) =>
                            {
                                Trap trap = new Trap(
                                    this,
                                    eff.Radius,
                                    eff.TotalDamage,
                                    eff.ConditionEffect ?? ConditionEffectIndex.Slowed,
                                    eff.EffectDuration);
                                trap.Move(target.X, target.Y);
                                world.EnterWorld(trap);
                            }));
                        }
                        break;
                    case ActivateEffects.SunshineBox:
                        {
                            int LockboxChance = Random.Next(0, 63);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1575];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Amazing! You have obtained a Dagger of Brimstone!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Dagger of Brimstone from the Sunshine Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa89];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Emeraldshard Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2299];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Staff of the Rising Sun!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x120b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Futuristic Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1303];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x229e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Thousand Suns Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x229f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Robe of the Summer Solstice!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2300];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Ring of the Burning Sun!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5075];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Ring of the Wildfire!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Ring of the Wildfire from the Sunshine Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5050];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed The Unspeakable Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 48:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1478];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Exquisite! You obtained the Wildfire Crossbow!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Wildfire Crossbow from the Sunshine Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 49:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1676];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Exquisite! You obtained the Wand Of The Prohibited Fire!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Wand of the Prohibited Fire from the Sunshine Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 50:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x130b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Futuristic Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 51:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 52:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 53:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb02];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Covert Havens");
                                            return false;
                                        }
                                    };
                                    break;
                                case 54:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb02];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Covert Havens");
                                            return false;
                                        }
                                    };
                                    break;
                                case 55:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Innocent Blood");
                                            return false;
                                        }
                                    };
                                    break;
                                case 56:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5075];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Ring of the Wildfire!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 57:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quicksilver Sheath!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 58:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb23];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Apparitions!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 59:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb20];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Phantoms!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 60:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1479];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("wow. a beyond staff yaaaay");
                                            return false;
                                        }
                                    };
                                    break;
                                case 61:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb20];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Phantoms!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 62:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Sunshine Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.DareFistBox:
                        {
                            int LockboxChance = Random.Next(0, 22);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Dagger of Foul Malevolence!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Elemental Detonation Spell!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb23];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Prism of Apparitions!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb29];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Helm of the Great General");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Giantcatcher Trap");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb26];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Seal of the Blessed Champion");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb25];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Tome of Holy Guidance");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5178];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Excellent! You've obtained a Staff of the Golden Gate!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Staff of the Golden Gate from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5183];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Awesome! You've obtained a Farewell Armor!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Farewell Armor from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5184];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Wand of Goo!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Wand of Goo from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5179];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Quiver of Distortion!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Quiver of Distortion from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5181];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Sword of Uranium!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Sword of Uranium from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Premium Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5070];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Fantastic! You've obtained a Fairy Blade!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Fairy Blade from the Premium DareFist Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                            }

                        }
                        break;
                    case ActivateEffects.CrimsonBox:
                        {
                            int LockboxChance = Random.Next(0, 46);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa89];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Emeraldshard Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1691];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Truncheon of Immortal Demons");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Truncheon of Immortal Demons from the Crimson Steel Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1200];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Purple Pinstripe Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1692];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Coat of the Devil");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Coat of the Devil Demons from the Crimson Steel Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1690];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Skull of Hades!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Skull of Hades from the Crimson Steel Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1689];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained The Eye of Peril!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the The Eye of Peril from the Crimson Steel Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Crimson Steel Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.NeonBox:
                        {
                            int LockboxChance = Random.Next(0, 50);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x258c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Frostbite!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bloodsucker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaaf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Lifedrinker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Innocent Blood");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1609];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wonderful! You have obtained a Staff of the Soda Company!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Staff of the Soda Company from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57ad];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You got a Mirror Dagger!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Mirror Dagger from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc2f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Lab Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57bd];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wow! You obtained the Blueshade Shield");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Blueshade Shield the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x120d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Heart Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1252];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Hibiscus Beach Wrap Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x122e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Starry Night Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x132f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Lemon-Lime Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57bf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Great! You got the Flambred Robe!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Flambred Robe from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57b1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Magnificent! You got the Blackguard Seal!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Blackguard Seal from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57ae];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Phenomenon Disc");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Phenomenon Disc from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcac];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Aquatic Egg!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcb4];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Humanoid Egg!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 48:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57bc];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Amazing! You obtained the Staff of Indecisive Maneuvers");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Staff of Indecisive Maneuvers from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 49:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Neon Annihilation Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57b2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Superb! You obtained the Wand of Obscurity");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Wand of Obscurity from the Neon Annihilation Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.RevivementBox:
                        {
                            int LockboxChance = Random.Next(0, 50);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x258c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Frostbite!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bloodsucker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaaf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Lifedrinker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Innocent Blood");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb33];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You have unboxed a Scepter of Storms");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5658];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got a Sword of the Soda Company!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Sword of the Soda Company from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc6f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Cemetery Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wow! You obtained the Redshade Shield");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Redshade Shield from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x120d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Heart Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1252];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Hibiscus Beach Wrap Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x122e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Starry Night Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x132f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Lemon-Lime Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a3];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got the Swiftsword!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Swiftsword from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a5];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Magnificent! You got the Royalty Bow!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Royalty Bow from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Royalty Armor");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Royalty Armor from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcac];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Aquatic Egg!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcb4];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Humanoid Egg!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 48:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a7];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Fantastic! You obtained the Noble Heatblast Trap");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Noble Heatblast Trap from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 49:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Revivement Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x57a8];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Superb! You obtained the Royalty Ring");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Royalty Ring from the Revivement Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.BlizzardBox:
                        {
                            int LockboxChance = Random.Next(0, 48);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x258c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Frostbite!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bloodsucker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaaf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Lifedrinker Skull");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1491];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wonderful! You have obtained a Tundracian Frozen Armor!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Tundracian Frozen Armor from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcf8];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got an Iceman Skin!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Iceman Skin from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x748C];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Ice Cave Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x514f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Frozen Halo Armor");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Frozen Halo Armor from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x120d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Heart Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x122e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Starry Night Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x132f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Lemon-Lime Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcf0];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got the Santa Skin!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Santa Skin from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcf1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got the Little Helper Skin!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Little Helper Skin from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x514f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Frozen Halo Armor");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Frozen Halo Armor from the Blizzard Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcac];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Aquatic Egg!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Blizzard Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc90];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Legendary Avian Egg!");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                         case ActivateEffects.GPBox:
                        {
                            int LockboxChance = Random.Next(0, 45);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) 
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Kunoichi Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) 
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Crusader Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received The Unknown Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received the Pyramid Remnant Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Chemist Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Good Hobo Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x516b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Swordsman Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x516c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Funcromancer Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x516e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Hero Skin!");
                                            return false;
                                        }
                                    };
                                    break;
								case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)

                                        {
                                            Inventory[i] = Manager.GameData.Items[0x516f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a SpookySpasticSpistical Skin!");
                                            return false;
                                        }
                                    };
                                    break;
								case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xff4];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Sprig of the Copse!");
											TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Sprig of the Copse from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
								case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xff5];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Caduceus of Nature!");
											TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Caduceus of Nature from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
								case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xff6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Shroud of Sagebrush!");
											TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Shroud of Sagebrush from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
								case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xff7];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Trinket of the Groves!");
											TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Trinket of the Groves from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1776];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Sequel of the Soda Company!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Sequel of the Soda Company from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1687];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Quiver of the Onslaught!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Quiver of the Onslaught from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5060];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Heavy Armor of the Scrolls!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Heavy Armor of the Scrolls from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5061];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Light Armor of the Scrolls!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Light Armor of the Scrolls from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5062];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Robe of the Scrolls!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Robe of the Scrolls from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1566];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Trenchant Armor!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Trenchant Armor from the GoodDeal Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xccb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Good! You've received a Fries!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x517f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Experiment7 Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x517d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Robobuddi Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x517e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Marksman Skin!");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.VorvBox:
                        {
                            int LockboxChance = Random.Next(0, 21);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xced];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Sunshine Shiv!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcec];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Robobow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcea];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received KoalaPOW!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xceb];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received the Spicy Wand of Spice!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcdf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Doctor Swordsworth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x515f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Good Hobo Skin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xcee];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Arbiter's Wrath!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2328];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Toy Knife!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2327];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Lethargic Sentience!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)

                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2326];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You've received a Barely Attuned Magic Thingy!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5189];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("FAMTASTIC! You've received a Staff of the Hilarious Jester!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Staff of the Hilarious Jester from Vorv's Vanity Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2325];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Precisely Calibrated Stringstick!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2324];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Unstable Anomaly!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x914];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Useless Katana!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1496];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Wow! You've received a Staff of Energized Fusion!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Staff of Energized Fusion from Vorv's Vanity Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1479];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Beyond Staff!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x199a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Spacesuit!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x199b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Space Helm!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x3001];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Ruby Mystery Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null)
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x2373];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("You've received a Skeleton Warrior Skin!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Skeleton Warrior Skin from Vorv's Vanity Package."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;


                            }

                        }
                        break;
                    case ActivateEffects.BrownScroll:
                        {
                            int BrownScrollChance = Random.Next(0, 3);
                            switch (BrownScrollChance)
                            {
                                case 0:
                                    {

                                        var db = new db.Database();

                                        CurrentFame = db.UpdateFame(Client.Account, 20);
                                        UpdateCount++;
                                        db.Dispose();
                                    }
                                    break;
                                case 1:
                                    {

                                        var db = new db.Database();

                                        CurrentFame = db.UpdateFame(Client.Account, 40);
                                        UpdateCount++;
                                        db.Dispose();
                                    }
                                    break;
                                case 2:
                                    {

                                        var db = new db.Database();

                                        CurrentFame = db.UpdateFame(Client.Account, 60);
                                        UpdateCount++;
                                        db.Dispose();
                                    }
                                    break;

                            }

                        }
                        break;
                        case ActivateEffects.RandomGold:
                        {
                            int GoldChance = Random.Next(0, 3);
                            switch (GoldChance)
                            {
                                case 0:
                                    {

                                        var db = new db.Database();

                                        Credits = db.UpdateCredit(Client.Account, 500);
                                        UpdateCount++;
                                        db.Dispose();
                                        SendHelp("You have acquired 500 gold!");
                                    }
                                    break;
                                case 1:
                                    {

                                        var db = new db.Database();

                                        Credits = db.UpdateCredit(Client.Account, 1000);
                                        UpdateCount++;
                                        db.Dispose();
                                        SendHelp("You have acquired 1000 gold!");
                                    }
                                    break;
                                case 2:
                                    {

                                        var db = new db.Database();

                                        Credits = db.UpdateCredit(Client.Account, 1500);
                                        UpdateCount++;
                                        db.Dispose();
                                        SendHelp("You have acquired 1500 gold!");
                                    }
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.IdScroll:
                        {
                            int ScrollChance = Random.Next(0, 15);
                            switch (ScrollChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5065];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5066];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x506b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5067];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5068];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x5069];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x506a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x506c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x506d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x506e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x507a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x507b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x507c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x507d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Unidentified Scroll")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x507e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Your scroll has been identified!");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.WigWeekBox:
                        {
                            int LockboxChance = Random.Next(0, 37);
                        switch (LockboxChance)
                        {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x199a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice, You have unboxed an Spacesuit!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x199b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You've unboxed a Space Helm!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x169f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Great! You unboxed Victory Conquest Armor!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1568];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You got a Robe of the Fire Bird!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Robe of the Fire Bird from the Wig Week Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1628];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You unboxed Doge's Pendant!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed Doge's Pendant from the Wig Week Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1613];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You got a Ring of Cold Winter!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1611];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You got a Sword of Winter!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1609];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got a Staff of the Soda Company!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1603];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Wonderful! You have unboxed a Blue Ring!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1601];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You have obtained a Hylian Shield.");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1598];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Good! You got a Deadly Ring!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1591];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Cool! You got a Ring of Outstanding Speed!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x179a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Adroit Armor!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Adroit Armor from the Wig Week Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1200];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Purple Pinstripe Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1482];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Nice! You got a Potion of Agility!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Darkfalcoo's Super Fry 2");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Darkfalcoo's Super Fry");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Wig Week Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.MayhemBox:
                        {
                            int LockboxChance = Random.Next(0, 57);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed the Sword of Acclaim!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcerery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaf6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Recompense!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Ichimonji");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc4f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Muramasa");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa89];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Emeraldshard Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa64];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magesteel Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb28];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quiver of Elvish Mastery");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa0c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Mithril Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb22];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Colossus Shield");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc68];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Tier Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc69];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Loot Drop Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc41];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Transformation Potion!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf14];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Corrupted Cleaver!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xf13];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got a Staff of Horrific Knowledge!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1686];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Outstanding! You obtained the Magic Throwing Cards");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Magic Throwing Cards from the Mayhem Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1304];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Rainbow Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 32:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1202];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Blue Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 33:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1203];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Large Black Striped Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 34:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1305];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Starry Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 35:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Tan Diamond Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 36:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1307];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool! You got a Small Blue Wave Cloth!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 37:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1623];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Prime Chaos Ring.");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Prime Chaos Ring from the Mayhem Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 38:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1614];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained the Helm of the Blue Tyrant!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Helm of the Blue Tyrant from the Mayhem Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 39:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1677];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Excellent! You obtained Spell of the Wonder Aquamarine!");
                                            TextPacket packet = new TextPacket
                                            {
                                                BubbleTime = 0,
                                                Stars = -1,
                                                Name = "",
                                                Text = Name + " has unboxed the Spell of the Wonder Aquamarine from the Mayhem Lockbox."
                                            };
                                            Owner.BroadcastPacket(packet, null);
                                            return false;
                                        }
                                    };
                                    break;
                                case 40:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa46];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Banishment Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 41:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb2d];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Planefetter Orb");
                                            return false;
                                        }
                                    };
                                    break;
                                case 42:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb24];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Elemental Detonation Spell");
                                            return false;
                                        }
                                    };
                                    break;
                                case 43:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa51];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Magic Mushroom");
                                            return false;
                                        }
                                    };
                                    break;
                                case 44:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage");
                                            return false;
                                        }
                                    };
                                    break;
                                case 45:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1862];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Transcendence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 46:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 47:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa65];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Quiver");
                                            return false;
                                        }
                                    };
                                    break;
                                case 48:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa2];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Astral Knowledge");
                                            return false;
                                        }
                                    };
                                    break;
                                case 49:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb08];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of the Cosmic Whole");
                                            return false;
                                        }
                                    };
                                    break;
                                case 50:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1855];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Nice! You got an Elemental Ruins Key!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 51:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 52:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 53:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaff];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Dagger of Foul Malevolence");
                                            return false;
                                        }
                                    };
                                    break;
                                case 54:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc50];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Masamune");
                                            return false;
                                        }
                                    };
                                    break;
                                case 55:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc59];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Doom Circle");
                                            return false;
                                        }
                                    };
                                    break;
                                case 56:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Mayhem Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x1593];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendHelp("Awesome! You have obtained a Ring of Outstanding Wisdom!");
                                            return false;
                                        }
                                    };
                                    break;

                            }

                        }
                        break;
                    case ActivateEffects.BronzeLockbox:
                        {
                            int LockboxChance = Random.Next(0, 32);
                            switch (LockboxChance)
                            {
                                case 0:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa83];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Dragonsoul Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 1:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa84];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Archon Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 2:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa47];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Skysplitter Sword!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 3:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Quicksilver Sheath!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 4:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0x185f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Sheath of Demonic Rage!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 5:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb31];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Cloudflash Scepter!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 6:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xb32];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Scepter of Skybolts!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 7:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa85];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Deep Sorcery!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 8:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa86];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Shadow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 9:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 10:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa87];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Wand of Ancient Warning!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 11:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa9f];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Horror!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 12:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa0];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Necrotic Arcana!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 13:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 14:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa1];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Staff of Diabolic Secrets!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 15:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa8];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Nightwing Venom!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 16:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaa7];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Felwasp Toxin!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 17:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaad];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Soul Siphon Skull!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 18:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaae];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Essence Tap Skull!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 19:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xaaf];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Lifedrinker Skull!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 20:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xab5];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Demonhunter Trap!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 21:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xab6];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Dragonstalker Trap!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 22:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa1e];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Golden Bow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 23:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8b];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Verdant Bow!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 24:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Bow of Fey Magic!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 25:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa8a];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed an Agateclaw Dagger!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 26:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa88];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Ragetalon Dagger!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 27:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa88];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Ragetalon Dagger!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 28:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa19];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("You have unboxed a Fire Dagger!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 29:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xc42];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool You got a XP Booster!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 30:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa21];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool You got a Potion of Speed!");
                                            return false;
                                        }
                                    };
                                    break;
                                case 31:
                                    for (int i = 0; i < Inventory.Length; i++)
                                    {
                                        if (Inventory[i] == null) continue;
                                        if (Inventory[i].ObjectId == "Bronze Lockbox Key")
                                        {
                                            Inventory[i] = Manager.GameData.Items[0xa4c];
                                            UpdateCount++;
                                            SaveToCharacter();
                                            SendInfo("Cool You got a Potion of Dexterity!");
                                            return false;
                                        }
                                    };
                                    break;
                            }

                        }
                        break;

                    case ActivateEffects.SamuraiAbility:
                        {
                            if (!ninjaShoot)
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Berserk,
                                    DurationMS = -1
                                });
                                ninjaFreeTimer = true;
                                ninjaShoot = true;
                            }
                            else
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Berserk,
                                    DurationMS = 0
                                });
                                ushort obj;
                                Manager.GameData.IdToObjectType.TryGetValue(item.ObjectId, out obj);
                                if (Mp >= item.MpEndCost)
                                {
                                    List<Packet> pkts = new List<Packet>();
                                    this.Aoe(eff.Range / 2, false, enemy =>
                                    {
                                        (enemy as Enemy).Damage(this, time, (int)this.StatsManager.GetAttackDamage(eff.TotalDamage, eff.TotalDamage), false, new ConditionEffect[0]);
                                    });
                                    pkts.Add(new ShowEffectPacket()
                                    {
                                        EffectType = EffectType.AreaBlast,
                                        TargetId = Id,
                                        Color = new ARGB(eff.Color ?? 0xffffffff),
                                        PosA = new Position() { X = eff.Range / 2 }
                                    });
                                    BroadcastSync(pkts, p => this.Dist(p) < 25);
                                    Mp -= (int)item.MpEndCost;
                                }
                                targetlink = target;
                                ninjaShoot = false;
                            }
                        }
                        break;
                   case ActivateEffects.AstonAbility:
                        {
                            if (!ninjaShoot)
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Berserk,
                                    DurationMS = -1
                                });
                                ninjaFreeTimer = true;
                                ninjaShoot = true;
                            }
                            else
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Berserk,
                                    DurationMS = 0
                                });
                                ushort obj;
                                Manager.GameData.IdToObjectType.TryGetValue(item.ObjectId, out obj);
                                if (Mp >= item.MpEndCost)
                                {
                                    ActivateShoot(time, item, pkt.ItemUsePos);
                                    Mp -= (int)item.MpEndCost;
                                }
                                targetlink = target;
                                ninjaShoot = false;
                            }
                        }
                        break;
                    case ActivateEffects.SamuraiAbility2:
                        {
                            var ydist = target.Y - Y;
                            var xdist = target.X - X;
                            var xwalkable = target.X + xdist / 2;
                            var ywalkable = target.Y + ydist / 2;
                            var tile = Owner.Map[(int)xwalkable, (int)ywalkable];
                            Owner.BroadcastPacket(new ShowEffectPacket
                            {
                                EffectType = EffectType.Diffuse,
                                Color = new ARGB(0xFFFF0000),
                                TargetId = Id,
                                PosA = target,
                                PosB = new Position { X = target.X + eff.Radius, Y = target.Y }
                            }, null);
                            Owner.Aoe(target, eff.Radius, false, enemy =>
                            {
                                (enemy as Enemy).Damage(this, time, eff.TotalDamage, false, new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Bleeding,
                                    DurationMS = eff.DurationMS
                                });
                            });
                            Move(target.X + xdist / 2, target.Y + ydist / 2);
                            UpdateCount++;

                            Owner.BroadcastPackets(new Packet[]
                            {
                            new GotoPacket
                            {
                                ObjectId = Id,
                                Position = new Position
                                {
                                    X = X,
                                    Y = Y
                                }
                            },
                            new ShowEffectPacket
                            {
                                EffectType = EffectType.Teleport,
                                TargetId = Id,
                                PosA = new Position
                                {
                                    X = X,
                                    Y = Y
                                },
                                Color = new ARGB(0xFFFFFFFF)
                            }
                            }, null);
                            ApplyConditionEffect(new ConditionEffect
                            {
                                Effect = ConditionEffectIndex.Paralyzed,
                                DurationMS = eff.DurationMS2
                            });

                        }
                        break;
                    case ActivateEffects.AsiHeal:
                        {
                            var amountHN = eff.Amount;
                            var rangeHN = eff.Range;
                            if (eff.UseWisMod)
                            {
                                amountHN = (int)UseWisMod(eff.Amount, 0);
                                rangeHN = UseWisMod(eff.Range);
                            }

                            List<Packet> pkts = new List<Packet>();
                            this.Aoe(rangeHN, true, player => { ActivateHealHp(player as Player, amountHN, pkts); });
                            pkts.Add(new ShowEffectPacket
                            {
                                EffectType = EffectType.AreaBlast,
                                TargetId = Id,
                                Color = new ARGB(0xff8000),
                                PosA = new Position { X = rangeHN }
                            });
                            pkts.Add(new ShowEffectPacket
                            {
                                EffectType = EffectType.AreaBlast,
                                TargetId = Id,
                                Color = new ARGB(0xffffffff),
                                PosA = new Position { X = rangeHN }
                            });
                            BroadcastSync(pkts, p => this.Dist(p) < 25);
                        }
                        break;
                    case ActivateEffects.UnlockPortal:

                        Portal portal = this.GetNearestEntity(5, Manager.GameData.IdToObjectType[eff.LockedName]) as Portal;

                        Packet[] packets = new Packet[3];
                        packets[0] = new ShowEffectPacket
                        {
                            EffectType = EffectType.AreaBlast,
                            Color = new ARGB(0xFFFFFF),
                            PosA = new Position { X = 5 },
                            TargetId = Id
                        };
                        if (portal == null) break;

                        portal.Unlock(eff.DungeonName);

                        packets[1] = new NotificationPacket
                        {
                            Color = new ARGB(0x00FF00),
                            Text =
                                "{\"key\":\"blank\",\"tokens\":{\"data\":\"Unlocked by " +
                                Name + "\"}}",
                            ObjectId = Id
                        };

                        packets[2] = new TextPacket
                        {
                            BubbleTime = 0,
                            Stars = -1,
                            Name = "",
                            Text = eff.DungeonName + " Unlocked by " + Name + "."
                        };

                        BroadcastSync(packets);

                        break;
                    case ActivateEffects.Create: //this is a portal
                    {
                        ushort objType;
                        if (!Manager.GameData.IdToObjectType.TryGetValue(eff.Id, out objType) ||
                            !Manager.GameData.Portals.ContainsKey(objType))
                            break; // object not found, ignore
                        Entity entity = Resolve(Manager, objType);
                        World w = Manager.GetWorld(Owner.Id); //can't use Owner here, as it goes out of scope
                        int TimeoutTime = Manager.GameData.Portals[objType].TimeoutTime;
                        string DungName = Manager.GameData.Portals[objType].DungeonName;

                        ARGB c = new ARGB(0x00FF00);


                        entity.Move(X, Y);
                        w.EnterWorld(entity);

                        w.BroadcastPacket(new NotificationPacket
                        {
                            Color = c,
                            Text =
                                "{\"key\":\"blank\",\"tokens\":{\"data\":\"" + DungName + " opened by " +
                                Client.Account.Name + "\"}}",
                            ObjectId = Client.Player.Id
                        }, null);
                        
                        w.BroadcastPacket(new TextPacket
                        {
                            BubbleTime = 0,
                            Stars = -1,
                            Name = "",
                            Text = DungName + " opened by " + Client.Account.Name
                        }, null);
                        w.Timers.Add(new WorldTimer(TimeoutTime*1000,
                            (world, t) => //default portal close time * 1000
                            {
                                try
                                {
                                    w.LeaveWorld(entity);
                                }
                                catch (Exception ex)
                                    //couldn't remove portal, Owner became null. Should be fixed with RealmManager implementation
                                {
                                    log.ErrorFormat("Couldn't despawn portal.\n{0}", ex);
                                }
                            }));
                    }
                        break;

                    case ActivateEffects.Dye:
                    {
                        if (item.Texture1 != 0)
                        {
                            Texture1 = item.Texture1;
                        }
                        if (item.Texture2 != 0)
                        {
                            Texture2 = item.Texture2;
                        }
                        SaveToCharacter();
                    }
                        break;

                    case ActivateEffects.ShurikenAbility:
                        {
                            if (!ninjaShoot)
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Speedy,
                                    DurationMS = -1
                                });
                                ninjaFreeTimer = true;
                                ninjaShoot = true;
                            }
                            else
                            {
                                ApplyConditionEffect(new ConditionEffect
                                {
                                    Effect = ConditionEffectIndex.Speedy,
                                    DurationMS = 0
                                });
                                ushort obj;
                                Manager.GameData.IdToObjectType.TryGetValue(item.ObjectId, out obj);
                                if (Mp >= item.MpEndCost)
                                {
                                    ActivateShoot(time, item, pkt.ItemUsePos);
                                    Mp -= (int)item.MpEndCost;
                                }
                                targetlink = target;
                                ninjaShoot = false;
                            }
                        }
                        break;

                    case ActivateEffects.UnlockSkin:
                        if (!Client.Account.OwnedSkins.Contains(item.ActivateEffects[0].SkinType))
                        {
                            Manager.Database.DoActionAsync(db =>
                            {
                                Client.Account.OwnedSkins.Add(item.ActivateEffects[0].SkinType);
                                MySqlCommand cmd = db.CreateQuery();
                                cmd.CommandText = "UPDATE accounts SET ownedSkins=@ownedSkins WHERE id=@id";
                                cmd.Parameters.AddWithValue("@ownedSkins",
                                    Utils.GetCommaSepString(Client.Account.OwnedSkins.ToArray()));
                                cmd.Parameters.AddWithValue("@id", AccountId);
                                cmd.ExecuteNonQuery();
                                SendInfo(
                                    "New skin unlocked successfully. Change skins in your Vault, or start a new character to use.");
                                Client.SendPacket(new UnlockedSkinPacket
                                {
                                    SkinID = item.ActivateEffects[0].SkinType
                                });
                            });
                            endMethod = false;
                            break;
                        }
                        SendInfo("Error.alreadyOwnsSkin");
                        endMethod = true;
                        break;

                    case ActivateEffects.PermaPet: //Doesnt exist anymore
                    {
                        //psr.Character.Pet = XmlDatas.IdToType[eff.ObjectId];
                        //GivePet(XmlDatas.IdToType[eff.ObjectId]);
                        //UpdateCount++;
                    }
                        break;

                    case ActivateEffects.Pet:
                        Entity en = Entity.Resolve(Manager, eff.ObjectId);
                        en.Move(X, Y);
                        en.SetPlayerOwner(this);
                        Owner.EnterWorld(en);
                        Owner.Timers.Add(new WorldTimer(30 * 1000, (w, t) =>
                        {
                            w.LeaveWorld(en);
                        }));
                        break;
                    case ActivateEffects.CreatePet:
                        if (!Owner.Name.StartsWith("Pet Yard"))
                        {
                            SendInfo("server.use_in_petyard");
                            return true;
                        }
                        if (item.Rarity == Rarity.Common)
                        {
                            Pet.Create(Manager, this, item);
                            break;
                        }
                        else if (item.Rarity == Rarity.Uncommon && Owner.ClientWorldName == "{nexus.Pet_Yard_2}" || item.Rarity == Rarity.Uncommon && Owner.ClientWorldName == "{nexus.Pet_Yard_3}" || item.Rarity == Rarity.Uncommon && Owner.ClientWorldName == "{nexus.Pet_Yard_4}" || item.Rarity == Rarity.Uncommon && Owner.ClientWorldName == "{nexus.Pet_Yard_5}")
                        {
                            Pet.Create(Manager, this, item);
                            break;
                        }
                        else if (item.Rarity == Rarity.Rare && Owner.ClientWorldName == "{nexus.Pet_Yard_3}" || item.Rarity == Rarity.Rare && Owner.ClientWorldName == "{nexus.Pet_Yard_4}" || item.Rarity == Rarity.Rare && Owner.ClientWorldName == "{nexus.Pet_Yard_5}")
                        {
                            Pet.Create(Manager, this, item);
                            break;
                        }
                        else if (item.Rarity == Rarity.Legendary && Owner.ClientWorldName == "{nexus.Pet_Yard_4}" || item.Rarity == Rarity.Legendary && Owner.ClientWorldName == "{nexus.Pet_Yard_5}")
                        {
                            Pet.Create(Manager, this, item);
                            break;
                        }
                        SendInfo("You need to upgrade your Pet Yard first.");
                        return true;

                        break;
                    case ActivateEffects.MysteryPortal:
                        string[] dungeons = new []
                        {
                            "Pirate Cave Portal",
                            "Forest Maze Portal",
                            "Spider Den Portal",
                            "Snake Pit Portal",
                            "Glowing Portal",
                            "Forbidden Jungle Portal",
                            "Candyland Portal",
                            "Haunted Cemetery Portal",
                            "Undead Lair Portal",
                            "Davy Jones' Locker Portal",
                            "Manor of the Immortals Portal",
                            "Abyss of Demons Portal",
                            "Lair of Draconis Portal",
                            "Mad Lab Portal",
                            "Ocean Trench Portal",
                            "Tomb of the Ancients Portal",
                            "Beachzone Portal",
                            "The Shatters",
                            "Deadwater Docks",
                            "Woodland Labyrinth",
                            "The Crawling Depths",
                            "Treasure Cave Portal",
                            "Battle Nexus Portal",
                            "Belladonna's Garden Portal",
                            "Lair of Shaitan Portal"
                        };

                        var descs = Manager.GameData.Portals.Where(_ => dungeons.Contains<string>(_.Value.ObjectId)).Select(_ => _.Value).ToArray();
                        var portalDesc = descs[Random.Next(0, descs.Count())];
                        Entity por = Entity.Resolve(Manager, portalDesc.ObjectId);
                        por.Move(this.X, this.Y);
                        Owner.EnterWorld(por);

                        Client.SendPacket(new NotificationPacket
                        {
                            Color = new ARGB(0x00FF00),
                            Text =
                                "{\"key\":\"blank\",\"tokens\":{\"data\":\"" + portalDesc.DungeonName + " opened by " +
                                Client.Account.Name + "\"}}",
                            ObjectId = Client.Player.Id
                        });

                        Owner.BroadcastPacket(new TextPacket
                        {
                            BubbleTime = 0,
                            Stars = -1,
                            Name = "",
                            Text = portalDesc.ObjectId + " opened by " + Name
                        }, null);

                        Owner.Timers.Add(new WorldTimer(portalDesc.TimeoutTime * 1000, (w, t) => //default portal close time * 1000
                        {
                            try
                            {
                                w.LeaveWorld(por);
                            }
                            catch (Exception ex)
                            {
                                log.ErrorFormat("Couldn't despawn portal.\n{0}", ex);
                            }
                        }));
                        break;
                    case ActivateEffects.GenericActivate:
                        var targetPlayer = eff.Target.Equals("player");
                        var centerPlayer = eff.Center.Equals("player");
                        var duration = (eff.UseWisMod) ?
                            (int)(UseWisMod(eff.DurationSec) * 1000) :
                            eff.DurationMS;
                        var range = (eff.UseWisMod)
                            ? UseWisMod(eff.Range)
                            : eff.Range;
                        
                        Owner.Aoe((eff.Center.Equals("mouse")) ? target : new Position { X = X, Y = Y }, range, targetPlayer, entity =>
                        {
                            if (IsSpecial(entity.ObjectType)) return;
                            if (!entity.HasConditionEffect(ConditionEffectIndex.Stasis) &&
                                !entity.HasConditionEffect(ConditionEffectIndex.Invincible))
                            {
                                entity.ApplyConditionEffect(
                                new ConditionEffect()
                                {
                                    Effect = eff.ConditionEffect.Value,
                                    DurationMS = duration
                                });
                            }
                        });

                        // replaced this last bit with what I had, never noticed any issue with it. Perhaps I'm wrong?
                        BroadcastSync(new ShowEffectPacket()
                        {
                            EffectType = (EffectType)eff.VisualEffect,
                            TargetId = Id,
                            Color = new ARGB(eff.Color ?? 0xffffffff),
                            PosA = centerPlayer ? new Position { X = range } : target,
                            PosB = new Position(target.X - range, target.Y) //Its the range of the diffuse effect
                        }, p => this.DistSqr(p) < 25);
                        break;
                }
            }
            UpdateCount++;
            return endMethod;
        }

        private float UseWisMod(float value, int offset = 1)
        {
            double totalWisdom = Stats[6] + 2 * Boost[6];

            if (totalWisdom < 30)
                return value;

            double m = (value < 0) ? -1 : 1;
            double n = (value * totalWisdom / 150) + (value * m);
            n = Math.Floor(n * Math.Pow(10, offset)) / Math.Pow(10, offset);
            if (n - (int)n * m >= 1 / Math.Pow(10, offset) * m)
            {
                return ((int)(n * 10)) / 10.0f;
            }

            return (int)n;
        }

        private static bool IsSpecial(ushort objType)
        {
            return objType == 0x750d || objType == 0x750e || objType == 0x222c || objType == 0x222d;
        }
    }
}