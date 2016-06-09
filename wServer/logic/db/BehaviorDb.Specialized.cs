﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wServer.logic.behaviors;
using wServer.logic.transitions;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Specialized = () => Behav()
            .Init("Spirit Prism Bomb",
                new State(
                    new State("Idle",
                        new TimedTransition(1000, "Explode")
                    ),
                    new State("Explode",
                        new Prioritize(
                            new StayCloseToSpawn(3, 3)
                        ),
                        new State("Explode 1",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new ChangeSize(100, 0),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new TimedTransition(100, "Explode 2")
                        ),
                        new State("Explode 2",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new TimedTransition(100, "Explode 3")
                        ),
                        new State("Explode 3",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new TimedTransition(100, "Explode 4")
                        ),
                        new State("Explode 4",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new TimedTransition(100, "Explode 5")
                        ),
                        new State("Explode 5",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new TimedTransition(100, "Explode 6")
                        ),
                        new State("Explode 6",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(1, false, 40, 90, false, 0xFF9933),
                            new Decay(0)
                        )
                    )
                )
            )
            .Init("Big Firecracker",
                new State(
                    new State("Explode",
                        new Prioritize(
                            new StayCloseToSpawn(3, 3)
                        ),
                        new State("Explode 1",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(250, "Explode 2")
                        ),
                        new State("Explode 2",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 3")
                        ),
                        new State("Explode 3",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 4")
                        ),
                        new State("Explode 4",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 5")
                        ),
                        new State("Explode 5",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 6")
                        ),
                        new State("Explode 6",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 7")
                        ),
                        new State("Explode 7",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 8")
                        ),
                        new State("Explode 8",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 9")
                        ),
                        new State("Explode 9",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 10")
                        ),
                        new State("Explode 10",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(2, 4), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new Decay(0)
                        )
                    )
                )
            )
            .Init("Lil Firecracker",
                new State(
                    new State("Explode",
                        new Prioritize(
                            new StayCloseToSpawn(3, 3)
                        ),
                        new State("Explode 1",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 2")
                        ),
                        new State("Explode 2",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 3")
                        ),
                        new State("Explode 3",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 4")
                        ),
                        new State("Explode 4",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 5")
                        ),
                        new State("Explode 5",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 6")
                        ),
                        new State("Explode 6",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 7")
                        ),
                        new State("Explode 7",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 8")
                        ),
                        new State("Explode 8",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 9")
                        ),
                        new State("Explode 9",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new TimedTransition(100, "Explode 10")
                        ),
                        new State("Explode 10",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(Random.Next(1, 2), false, 100, 100, true, (uint)Random.Next(0x0000000, 0xFFFFFF)),
                            new Decay(0)
                        )
                    )
                )
            )
            .Init("Rock Candy Grenade",
                new State(
                    new State("Explode",
                        new Prioritize(
                            new StayCloseToSpawn(3, 3)
                        ),
                        new State("Explode 1",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 2")
                        ),
                        new State("Explode 2",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 3")
                        ),
                        new State("Explode 3",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 4")
                        ),
                        new State("Explode 4",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 5")
                        ),
                        new State("Explode 5",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 6")
                        ),
                        new State("Explode 6",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 7")
                        ),
                        new State("Explode 7",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 8")
                        ),
                        new State("Explode 8",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 9")
                        ),
                        new State("Explode 9",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new TimedTransition(100, "Explode 10")
                        ),
                        new State("Explode 10",
                            new JumpToRandomOffset(-2, 2, -2, 2),
                            new PlaySound(),
                            new Aoe(2, false, 100, 200, true, 0xFF6633),
                            new Decay(0)
                        )
                    )
                )
            )
        .Init("Xion Charge Bomb",
                new State(
                    new State("Idle",
                        new Orbit(0.5, 2, 10),
                        new TimedTransition(4200, "Explode")
                    ),
                    new State("Explode",
                        new State("Explode 1",
                            new PlaySound(),
                            new Aoe(5, false, 350, 475, false, 0xFF00FF),
                            new TimedTransition(50, "die")
                            ),
                        new State("die",
                            new Decay(0)
                        )
                    )
                )
            )
        .Init("Blessing Beacon",
                new State(
                    new State("Idle",
                        new Flash(0xFFFFFF, 1, 1),
                        new ChangeSize(50, 155),
                        new TimedTransition(1600, "HealEm")
                    ),
                    new State("HealEm",
                        new State("Heal1",
                            new NexusHealHp(4, 20, 450),
                            new TimedTransition(500, "Heal2")
                            ),
                        new State("Heal2",
                            new NexusHealHp(4, 40, 450),
                            new TimedTransition(500, "Heal3")
                            ),
                        new State("Heal3",
                            new NexusHealHp(4, 60, 450),
                            new TimedTransition(500, "Heal4")
                            ),
                        new State("Heal4",
                            new NexusHealHp(4, 80, 450),
                            new TimedTransition(500, "Charge")
                            ),
                        new State("Charge",
                            new Flash(0xFFFFFF, 1, 1),
                            new TimedTransition(1000, "Heal5")
                            ),
                        new State("Heal5",
                            new NexusHealHp(5, 240, 450),
                            new TimedTransition(600, "die")
                            ),
                        new State("die",
                            new PlaySound(),
                            new Decay(0)
                        )
                    )
                )
            )
            .Init("Zombie Trickster",
                new State(
                    new Wander(1)
                )
            )
            .Init("Realm Portal Opener",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible, true)
                )
            );
    }
}