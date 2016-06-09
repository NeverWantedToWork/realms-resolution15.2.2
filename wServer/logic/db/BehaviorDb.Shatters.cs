using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using wServer.realm;
using wServer.logic.behaviors;
using wServer.logic.loot;
using wServer.logic.transitions;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Shatters = () => Behav()
            .Init("shtrs Stone Paladin",
                new State(
                    new State("Idle",
                        new Prioritize(
                            new Wander(0.8)
                            ),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Reproduce(densityMax: 4),
                        new PlayerWithinTransition(8, "Attacking")
                        ),
                    new State("Attacking",
                        new State("Bullet",
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 90, coolDownOffset: 0, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 100, coolDownOffset: 200, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 110, coolDownOffset: 400, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 120, coolDownOffset: 600, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 130, coolDownOffset: 800, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 140, coolDownOffset: 1000, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 150, coolDownOffset: 1200, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 160, coolDownOffset: 1400, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 170, coolDownOffset: 1600, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 180, coolDownOffset: 1800, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 180, coolDownOffset: 2000, shootAngle: 45),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 180, coolDownOffset: 0, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 170, coolDownOffset: 200, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 160, coolDownOffset: 400, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 150, coolDownOffset: 600, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 140, coolDownOffset: 800, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 130, coolDownOffset: 1000, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 120, coolDownOffset: 1200, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 110, coolDownOffset: 1400, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 100, coolDownOffset: 1600, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 90, coolDownOffset: 1800, shootAngle: 90),
                            new Shoot(1, 4, coolDown: 10000, fixedAngle: 90, coolDownOffset: 2000, shootAngle: 22.5),
                            new TimedTransition(3000, "Wait")
                            ),
                        new State("Wait",
                            new Follow(0.4, range: 2),
                            new Flash(0xff00ff00, 0.1, 20),
                            new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                            new TimedTransition(2750, "Bullet")
                            ),
                        new NoPlayerWithinTransition(13, "Idle")
                        )
                    )
               )
            .Init("shtrs Wooden Gate",
                new State(
                    new State("Idle",
                        new EntityNotExistsTransition("shtrs Abandoned Switch 1", 10, "Despawn")
                        ),
                    new State("Despawn",
                        new Decay(0)
                        ))
            )
        .Init("shtrs Wooden Gate 2",
                new State(
                    new State("Idle",
                        new EntitiesNotExistsTransition(9999, "Despawn", "shtrs Abandoned Switch 2", "shtrs Abandoned Switch 3", "shtrs Abandoned Switch 4", "shtrs Abandoned Switch 5", "shtrs Abandoned Switch 6")
                        ),
                    new State("Despawn",
                        new Decay(0)
                        )
                    )
              )
            .Init("shtrs Stone Knight",
            new State(
                new State("Follow",
                        new Follow(0.6, 10, 5),
                        new Shoot(5, 5, projectileIndex: 0, coolDown: 2000),
                        new PlayerWithinTransition(4, "Charge")
                    ),
                    new State("Charge",
                        new TimedTransition(2000, "Follow"),
                        new Charge(4, 5),
                        new Shoot(5, 6, projectileIndex:0, coolDown:3000)
                        )
                    )
            )
            .Init("shtrs Ice Mage",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(8, "Main")
                        ),
                new State("Main",
                    new Follow(0.5, range: 1),
                    new Shoot(10, 5, 10, projectileIndex: 0, coolDown: 1500),
                    new TimedTransition(10000, "SpawnSphere")
                    ),
                    new State("SpawnSphere",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x0000FF, 2, 2),
                    new Spawn("shtrs Ice Shield", 1, 1, coolDown: 99999),
                    new TimedTransition(2500, "Main")
                    )
                )
            )
        .Init("shtrs Ice Shield",
            new State(
                new State("IceyMain",
                    new TimedTransition(4500, "RushAndRek"),
                    new Orbit(0.55, 2, target: "shtrs Ice Mage"),
                    new State("Quadforce1",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 0, coolDown: 300),
                            new TimedTransition(125, "Quadforce2")
                        ),
                        new State("Quadforce2",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 15, coolDown: 300),
                            new TimedTransition(125, "Quadforce3")
                        ),
                        new State("Quadforce3",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 30, coolDown: 300),
                            new TimedTransition(125, "Quadforce4")
                        ),
                        new State("Quadforce4",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 45, coolDown: 300),
                            new TimedTransition(125, "Quadforce5")
                        ),
                        new State("Quadforce5",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 60, coolDown: 300),
                            new TimedTransition(125, "Quadforce6")
                        ),
                        new State("Quadforce6",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 75, coolDown: 300),
                            new TimedTransition(125, "Quadforce7")
                        ),
                        new State("Quadforce7",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 90, coolDown: 300),
                            new TimedTransition(125, "Quadforce8")
                        ),
                        new State("Quadforce8",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 105, coolDown: 300),
                            new TimedTransition(125, "Quadforce9")
                        ),
                        new State("Quadforce9",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 120, coolDown: 300),
                            new TimedTransition(125, "Quadforce10")
                        ),
                        new State("Quadforce10",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 135, coolDown: 300),
                            new TimedTransition(125, "Quadforce11")
                        ),
                        new State("Quadforce11",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 150, coolDown: 300),
                            new TimedTransition(125, "Quadforce12")
                        ),
                        new State("Quadforce12",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 165, coolDown: 300),
                            new TimedTransition(125, "Quadforce13")
                        ),
                        new State("Quadforce13",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 180, coolDown: 300),
                            new TimedTransition(125, "Quadforce1")
                        )
                    ),
                   new State("RushAndRek",
                      new Charge(1.2, 8, coolDown: 1000),
                      new TimedTransition(1200, "Explode")
                    ),
                    new State("Explode",
                    new Shoot(10, count: 27, projectileIndex: 1, coolDown: 9999),
                    new Suicide()
                    )
                )
            )
        .Init("shtrs Fire Mage",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(8, "Main")
                        ),
                new State("Main",
                    new Follow(0.4, range: 1),
                    new Shoot(10, 5, 8, projectileIndex: 1, coolDown: 1),
                    new TimedTransition(2000, "StayBack")
                    ),
                    new State("StayBack",
                    new Shoot(10, 6, 22, projectileIndex: 0, coolDown: 1400),
                    new Prioritize(
                        new StayBack(0.4, 2),
                        new Wander(0.2)
                        ),
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(2000, "Main")
                    )
                )
            )
            .Init("shtrs Ice Adept",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(8, "Main")
                        ),
                new State("Main",
                    new TimedTransition(9000, "Throw"),
                    new Prioritize(
                        new StayAbove(1, 200),
                        new Follow(0.8, range: 8)
                        ),
                    new Shoot(10, 1, projectileIndex: 0, coolDown: 200, predictive: 1),
                    new Shoot(10, 2, projectileIndex: 0, coolDown: 473),
                    new Shoot(10, 3, projectileIndex: 1, shootAngle:10, coolDown: 4000, predictive: 1)
                ),
                new State("Throw",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0x0000FF, 2, 2),
                    new TossObject("shtrs Ice Portal", 5, coolDown: 8000),
                    new TimedTransition(2000, "Main")
                )
                  ))
        .Init("shtrs Fire Adept",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(8, "Main")
                        ),
                new State("Main",
                    new TimedTransition(9000, "Throw"),
                    new Prioritize(
                        new StayAbove(0.7, 200),
                        new Follow(0.8, range: 8)
                        ),
                    new Shoot(10, 3, shootAngle: 40, projectileIndex: 0, coolDown: 1000, predictive: 1),
                    new Shoot(10, 2, projectileIndex: 1, shootAngle: 60, coolDown: 2000)
                ),
                new State("Throw",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xFF0000, 2, 2),
                    new TossObject("shtrs Fire Portal", 5, coolDown: 8000),
                    new TimedTransition(2450, "Main")
                )
             ))
        .Init("shtrs Glassier Archmage",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(5, "Main")
                        ),
                new State("Main",
                    new TimedTransition(6000, "Main2"),
                    new StayBack(0.6, 5),
                    new Shoot(10, 8, projectileIndex: 1, coolDown: 2000, predictive: 1),
                    new Shoot(10, 1, projectileIndex: 2, coolDown: 175)
                ),
                new State("Main2",
                    new TimedTransition(5000, "Main"),
                    new Follow(0.5, 8, 1),
                    new Shoot(10, 6, projectileIndex: 1, coolDown: 2000, predictive: 1),
                    new Shoot(10, 3, projectileIndex: 0, shootAngle: 12, coolDown: 1000)
                )
             ))
        .Init("shtrs S Glassier Archmage",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(5, "Main")
                        ),
                new State("Main",
                    new TimedTransition(5000, "Main2"),
                    new StayBack(0.6, 5),
                    new Shoot(10, 8, projectileIndex: 1, coolDown: 2000, predictive: 1),
                    new Shoot(10, 1, projectileIndex: 2, coolDown: 175)
                ),
                new State("Main2",
                    new TimedTransition(5000, "Main"),
                    new Follow(0.5, 8, 1),
                    new Shoot(10, 6, projectileIndex: 1, coolDown: 2000, predictive: 1),
                    new Shoot(10, 3, projectileIndex: 0, shootAngle: 12, coolDown: 1000)
                )
             ))
        
        .Init("shtrs Archmage of Flame",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(5, "Main")
                        ),
                new State("Main",
                    new TimedTransition(8000, "Throw"),
                    new Follow(0.6, 8, 1),
                    new Shoot(10, 6, projectileIndex: 0, shootAngle: 60, coolDown: 1200, predictive: 1),
                    new Shoot(10, 4, projectileIndex: 1, coolDown: 3200)
                ),
                new State("Throw",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xFF0000, 2, 2),

                    //Fire Bomb  throwing
                    new TossObject("shtrs Firebomb", range: 5, angle: 0, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 20, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 40, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 75, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 180, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 200, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 7, angle: 140, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 360, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 300, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 5, angle: 270, coolDown: 99999),

                    new TimedTransition(1000, "Main")
                )
             ))
        .Init("shtrs S Archmage of Flame",
            new State(
                new State("GetClose",
                        new PlayerWithinTransition(5, "Main")
                        ),
                new State("Main",
                    new TimedTransition(8000, "Throw"),
                    new Prioritize(
                        new StayAbove(0.6, 200),
                        new Follow(0.5, range: 8)
                        ),
                    new Shoot(10, 6, projectileIndex: 0, shootAngle: 60, coolDown: 1200, predictive: 1),
                    new Shoot(10, 4, projectileIndex: 1, coolDown: 3200)
                ),
                new State("Throw",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xFF0000, 2, 2),

                    //Fire Bomb  throwing
                    new TossObject("shtrs Firebomb", range: 5, angle: 0, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 20, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 40, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 180, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 200, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 7, angle: 140, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 360, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 300, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 5, angle: 270, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 180, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 7, angle: 250, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 5, angle: 170, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 7, angle: 320, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 5, angle: 220, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 4, angle: 90, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 7, angle: 67, coolDown: 99999),
                    new TossObject("shtrs Firebomb", range: 6, angle: 120, coolDown: 99999),

                    new TimedTransition(1000, "Main")
                )
             ))
        .Init("shtrs Stone Mage",
            new State(
                new State("Main",
                    new TimedTransition(11000, "Throw"),
                    new Prioritize(
                        new StayAbove(0.4, 200),
                        new Follow(0.5, range: 8)
                        ),
                    new Shoot(10, 16, projectileIndex: 0, coolDown: 3500, predictive: 1),
                    new Shoot(10, 2, shootAngle: 7, projectileIndex: 1, coolDown: 1500)
                ),
                new State("Throw",
                    new Taunt(1.00, "PREPARE FOR MAGIKS!", "WE WILL CURSH YOUR BONES.", "YOUR LIFE IS AT AN END!"),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Flash(0xFFFFFF, 2, 2),

                    //Spike throwing
                    new TossObject("shtrs Spike", range: 3, angle: 0, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 2, angle: 20, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 4, angle: 40, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 4, angle: 180, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 3, angle: 200, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 7, angle: 140, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 2, angle: 360, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 4, angle: 300, coolDown: 99999),
                    new TossObject("shtrs Spike", range: 4, angle: 90, coolDown: 99999),

                    new TimedTransition(1000, "Main")
                )
             ))
        .Init("shtrs Firebomb",
                        new State(
                            new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new State("GonnaBlam",
                    new TimedTransition(2000, "Blam")
                        ),
                    new State("Blam",
                       new Shoot(8.4, count: 8, projectileIndex: 0),
                       new Suicide()
                    )))


                 .Init("shtrs Spike",
                        new State(
                    new State("GonnaBlam",
                    new TimedTransition(4500, "Blam")
                        ),
                    new State("Blam",
                       new Shoot(8.4, count: 7, projectileIndex: 0),
                       new Suicide()
                    )))

                .Init("shtrs Paladin Obelisk",
            new State(
                new State("idlespawn",
                     new Spawn("shtrs Stone Paladin", 1, 1, coolDown: 8750),
                     new PlayerWithinTransition(5, "active")
                    ),
                new State("active",
                    new NoPlayerWithinTransition(5, "idlespawn")
                    )
                )
            )
           .Init("shtrs Bridge Obelisk A",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose",
                    
                    new PlayerWithinTransition(11, "activeghost")
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ), 
                new State("active",
                    new Taunt(1.00, "DO NOT WAKE THE BRIDGE GUARDIAN!"),
                    new Order(999, "shtrs Bridge Obelisk B", "active"),
                    new Order(999, "shtrs Bridge Obelisk C", "active"),
                    new Order(999, "shtrs Bridge Obelisk D", "active"),
                    new Order(999, "shtrs Bridge Obelisk E", "active"),
                    new Order(999, "shtrs Bridge Obelisk F", "active"),
                    new Order(999, "shtrs Bridge Sentinel", "Close Bridge"),

                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Knight", 1, 1, coolDown: 99999),
                     new Spawn("shtrs Stone Mage", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            )
        .Init("shtrs Bridge Obelisk B",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose"
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ),
                new State("active",
                    new Taunt(1.00, "DO NOT WAKE THE BRIDGE GUARDIAN!"),
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Knight", 1, 1, coolDown: 99999),
                     new Spawn("shtrs Stone Mage", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            )
        .Init("shtrs Bridge Obelisk D",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose"
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ),
                new State("active",
                    new Taunt(1.00, "DO NOT WAKE THE BRIDGE GUARDIAN!"),
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Knight", 1, 1, coolDown: 99999),
                     new Spawn("shtrs Stone Mage", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            )
        .Init("shtrs Bridge Obelisk E",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose"
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ),
                new State("active",
                    new Taunt(1.00, "DO NOT WAKE THE BRIDGE GUARDIAN!"),
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Knight", 1, 1, coolDown: 99999),
                     new Spawn("shtrs Stone Mage", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            )
        .Init("shtrs Titanum",
            new State(
                new State("idlespawn",
                    new Spawn("shtrs Stone Knight", 1, 1, coolDown: 8000),
                     new Spawn("shtrs Stone Mage", 1, 1, coolDown: 9500),
                     
                     new PlayerWithinTransition(5, "active")
                    ),
                new State("active",
                    new NoPlayerWithinTransition(5, "idlespawn")
                    )
                )
            )

            .Init("shtrs MagiGenerators",
            new State(
                new State("Main",
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                    new Shoot(15, 10, coolDown:1000),
                    new Shoot(15, 1, projectileIndex:1, coolDown:2500)
                ),
                new State("Hide",
                    new SetAltTexture(1),
                    new ConditionalEffect(ConditionEffectIndex.Invulnerable)
                    ),
                new State("Despawn",
                    new Decay()
                    )
                  ))
            .Init("shtrs Ice Portal",
                new State(
                    new State("Idle",
                        new TimedTransition(2500, "Spin")
                    ),
                    new State("Spin",
                        new TimedTransition(2000, "Pause"),
                        new State("Quadforce1",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 0, coolDown: 300),
                            new TimedTransition(125, "Quadforce2")
                        ),
                        new State("Quadforce2",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 15, coolDown: 300),
                            new TimedTransition(125, "Quadforce3")
                        ),
                        new State("Quadforce3",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 30, coolDown: 300),
                            new TimedTransition(125, "Quadforce4")
                        ),
                        new State("Quadforce4",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 45, coolDown: 300),
                            new TimedTransition(125, "Quadforce5")
                        ),
                        new State("Quadforce5",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 60, coolDown: 300),
                            new TimedTransition(125, "Quadforce6")
                        ),
                        new State("Quadforce6",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 75, coolDown: 300),
                            new TimedTransition(125, "Quadforce7")
                        ),
                        new State("Quadforce7",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 90, coolDown: 300),
                            new TimedTransition(125, "Quadforce8")
                        ),
                        new State("Quadforce8",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 105, coolDown: 300),
                            new TimedTransition(125, "Quadforce1")
                        )
                    ),
                    new State("Pause",
                       new TimedTransition(3000, "Spin")
                    )
                )
            )
            .Init("shtrs Fire Portal",
                new State(
                    new State("Idle",
                        new TimedTransition(2500, "Spin")
                    ),
                    new State("Spin",
                        new TimedTransition(2000, "Pause"),
                        new State("Quadforce1",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 0, coolDown: 300),
                            new TimedTransition(125, "Quadforce2")
                        ),
                        new State("Quadforce2",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 15, coolDown: 300),
                            new TimedTransition(125, "Quadforce3")
                        ),
                        new State("Quadforce3",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 30, coolDown: 300),
                            new TimedTransition(125, "Quadforce4")
                        ),
                        new State("Quadforce4",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 45, coolDown: 300),
                            new TimedTransition(125, "Quadforce5")
                        ),
                        new State("Quadforce5",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 60, coolDown: 300),
                            new TimedTransition(125, "Quadforce6")
                        ),
                        new State("Quadforce6",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 75, coolDown: 300),
                            new TimedTransition(125, "Quadforce7")
                        ),
                        new State("Quadforce7",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 90, coolDown: 300),
                            new TimedTransition(125, "Quadforce8")
                        ),
                        new State("Quadforce8",
                            new Shoot(0, projectileIndex: 0, count: 6, shootAngle: 60, fixedAngle: 105, coolDown: 300),
                            new TimedTransition(125, "Quadforce1")
                        )
                    ),
                    new State("Pause",
                       new TimedTransition(3000, "Spin")
                    )
                )
            )
            .Init("shtrs Bridge Sentinel",
                new State(
                    new SetLootState("obelisk"),
                    new CopyLootState("shtrs encounterchestspawner", 20),
                    new HpLessTransition(0.1, "Death"),
                    new CopyDamageOnDeath("shtrs Encounter Chest"),
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable)
                    ),
                    //not correct
                    new State("Close Bridge",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new CallWorldMethod("TheShatters", "CloseBridge1", null),
                        new TimedTransition(7000, "Start")
                    ),
                    new State("Start",
                        new Shoot(15, 10, 15, 5, 90, coolDown: 1000),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntitiesNotExistsTransition(9999, "Wake", "shtrs Bridge Obelisk A", "shtrs Bridge Obelisk B", "shtrs Bridge Obelisk C", "shtrs Bridge Obelisk D", "shtrs Bridge Obelisk E", "shtrs Bridge Obelisk F")
                        ),
                        new State("Wake",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt("Who has woken me...? Leave this place."),
                        new Timed(2100, new Shoot(15, 15, 12, projectileIndex: 0, fixedAngle: 180, coolDown: 700, coolDownOffset: 3000)),
                        new TimedTransition(8000, "Swirl Shot")
                        ),
                        new State("Swirl Shot",
                            new Taunt("Go."),
                            new TimedTransition(7500, "Blobomb"),
                            new State("Swirl1",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 180, coolDown: 200),
                            new TimedTransition(50, "Swirl2")
                            ),
                            new State("Swirl2",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 192, coolDown: 200),
                            new TimedTransition(50, "Swirl3")
                            ),
                            new State("Swirl3",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 204, coolDown: 200),
                            new TimedTransition(50, "Swirl4")
                            ),
                            new State("Swirl4",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 216, coolDown: 200),
                            new TimedTransition(50, "Swirl5")
                            ),
                            new State("Swirl5",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 228, coolDown: 200),
                            new TimedTransition(50, "Swirl6")
                            ),
                            new State("Swirl6",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 240, coolDown: 200),
                            new TimedTransition(50, "Swirl7")
                            ),
                            new State("Swirl7",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 252, coolDown: 200),
                            new TimedTransition(50, "Swirl8")
                            ),
                            new State("Swirl8",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 264, coolDown: 200),
                            new TimedTransition(50, "Swirl9")
                            ),
                            new State("Swirl9",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 276, coolDown: 200),
                            new TimedTransition(50, "Swirl10")
                            ),
                            new State("Swirl10",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 288, coolDown: 200),
                            new TimedTransition(50, "Swirl11")
                            ),
                            new State("Swirl11",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 300, coolDown: 200),
                            new TimedTransition(50, "Swirl12")
                            ),
                            new State("Swirl12",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 312, coolDown: 200),
                            new TimedTransition(50, "Swirl13")
                            ),
                            new State("Swirl13",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 324, coolDown: 200),

                            new TimedTransition(50, "Swirl14")
                            ),
                            new State("Swirl14",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 336, coolDown: 200),
                            new TimedTransition(50, "Swirl15")
                            ),
                            new State("Swirl15",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 336, coolDown: 200),
                            new TimedTransition(50, "Swirl16")
                            ),
                            new State("Swirl16",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 324, coolDown: 200),

                            new TimedTransition(50, "Swirl17")
                            ),
                            new State("Swirl17",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 312, coolDown: 200),
                            new TimedTransition(50, "Swirl18")
                            ),
                            new State("Swirl18",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 300, coolDown: 200),
                            new TimedTransition(50, "Swirl19")
                            ),
                            new State("Swirl19",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 288, coolDown: 200),
                            new TimedTransition(50, "Swirl20")
                            ),
                            new State("Swirl20",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 276, coolDown: 200),
                            new TimedTransition(50, "Swirl21")
                            ),
                            new State("Swirl21",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 264, coolDown: 200),
                            new TimedTransition(50, "Swirl22")
                            ),
                            new State("Swirl22",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 252, coolDown: 200),
                            new TimedTransition(50, "Swirl23")
                            ),
                            new State("Swirl23",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 240, coolDown: 200),
                            new TimedTransition(50, "Swirl24")
                            ),
                            new State("Swirl24",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 228, coolDown: 200),
                            new TimedTransition(50, "Swirl25")
                            ),
                            new State("Swirl25",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 216, coolDown: 200),
                            new TimedTransition(50, "Swirl26")
                            ),
                            new State("Swirl26",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 204, coolDown: 200),
                            new TimedTransition(50, "Swirl27")
                            ),
                            new State("Swirl27",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 192, coolDown: 200),
                            new TimedTransition(50, "Swirl28")
                            ),
                            new State("Swirl28",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 180, coolDown: 200),
                            new TimedTransition(50, "Swirl1")
                            )
                            ),
                            new State("Blobomb",
                            new Taunt("You live still? DO NOT TEMPT FATE!"),
                            new Taunt("CONSUME!"),
                            new Order(20, "shtrs blobomb maker", "Spawn"),
                            new EntityNotExistsTransition("shtrs Blobomb", 30, "SwirlAndShoot")
                                ),
                                new State("SwirlAndShoot",
                                    new TimedTransition(10000, "Blobomb"),
                                    new Taunt("FOOLS! YOU DO NOT UNDERSTAND!"),
                                    new ChangeSize(20, 130),
                            new Shoot(15, 15, 11, projectileIndex: 0, fixedAngle: 180, coolDown: 700, coolDownOffset: 700),
                                   new State("Swirl11",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 180, coolDown: 200),
                            new TimedTransition(50, "Swirl21")
                            ),
                            new State("Swirl21",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 192, coolDown: 200),
                            new TimedTransition(50, "Swirl31")
                            ),
                            new State("Swirl31",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 204, coolDown: 200),
                            new TimedTransition(50, "Swirl41")
                            ),
                            new State("Swirl41",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 216, coolDown: 200),
                            new TimedTransition(50, "Swirl51")
                            ),
                            new State("Swirl51",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 228, coolDown: 200),
                            new TimedTransition(50, "Swirl61")
                            ),
                            new State("Swirl61",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 240, coolDown: 200),
                            new TimedTransition(50, "Swirl71")
                            ),
                            new State("Swirl71",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 252, coolDown: 200),
                            new TimedTransition(50, "Swirl81")
                            ),
                            new State("Swirl81",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 264, coolDown: 200),
                            new TimedTransition(50, "Swirl91")
                            ),
                            new State("Swirl91",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 276, coolDown: 200),
                            new TimedTransition(50, "Swirl101")
                            ),
                            new State("Swirl101",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 288, coolDown: 200),
                            new TimedTransition(50, "Swirl111")
                            ),
                            new State("Swirl111",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 300, coolDown: 200),
                            new TimedTransition(50, "Swirl121")
                            ),
                            new State("Swirl121",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 312, coolDown: 200),
                            new TimedTransition(50, "Swirl131")
                            ),
                            new State("Swirl131",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 324, coolDown: 200),

                            new TimedTransition(50, "Swirl141")
                            ),
                            new State("Swirl141",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 336, coolDown: 200),
                            new TimedTransition(50, "Swirl151")
                            ),
                            new State("Swirl151",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 336, coolDown: 200),
                            new TimedTransition(50, "Swirl161")
                            ),
                            new State("Swirl161",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 324, coolDown: 200),

                            new TimedTransition(50, "Swirl171")
                            ),
                            new State("Swirl171",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 312, coolDown: 200),
                            new TimedTransition(50, "Swirl181")
                            ),
                            new State("Swirl181",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 300, coolDown: 200),
                            new TimedTransition(50, "Swirl191")
                            ),
                            new State("Swirl191",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 288, coolDown: 200),
                            new TimedTransition(50, "Swirl201")
                            ),
                            new State("Swirl201",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 276, coolDown: 200),
                            new TimedTransition(50, "Swirl211")
                            ),
                            new State("Swirl211",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 264, coolDown: 200),
                            new TimedTransition(50, "Swirl221")
                            ),
                            new State("Swirl221",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 252, coolDown: 200),
                            new TimedTransition(50, "Swirl231")
                            ),
                            new State("Swirl231",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 240, coolDown: 200),
                            new TimedTransition(50, "Swirl241")
                            ),
                            new State("Swirl241",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 228, coolDown: 200),
                            new TimedTransition(50, "Swirl251")
                            ),
                            new State("Swirl251",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 216, coolDown: 200),
                            new TimedTransition(50, "Swirl261")
                            ),
                            new State("Swirl261",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 204, coolDown: 200),
                            new TimedTransition(50, "Swirl271")
                            ),
                            new State("Swirl271",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 192, coolDown: 200),
                            new TimedTransition(50, "Swirl281")
                            ),
                            new State("Swirl281",
                            new Shoot(15, 1, projectileIndex: 0, fixedAngle: 180, coolDown: 200),
                            new TimedTransition(50, "Swirl11")
                            )
                            ),
                                    new State("Death",
                                        new InvisiToss("shtrs Encounter Chest", 8, angle: 180, coolDown: 9999),
                                        new CallWorldMethod("Shatters", "OpenBridge1Behind"),
                                        new ChangeSize(20, 130),
                                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                                        new Taunt("I tried to protect you...I have failed. You release a great evil upon this realm..."),
                                        new Shoot(15, 12, projectileIndex: 0, coolDown: 100000, coolDownOffset: 3000),
                                        new CopyDamageOnDeath("shtrs Encounter Chest"),
                                        
                                        new Suicide()
                                        )
                        )
            )

            .Init("shtrs Twilight Archmage",
                new State(
                    new SetLootState("archmage"),
                    new CopyLootState("shtrs encounterchestspawner", 20),
                    new HpLessTransition(.1, "Death"),
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition2("shtrs Glassier Archmage", "shtrs Archmage of Flame", 15, "Wake")
                    ),
                    new State("Wake",
                        new State("Comment1",
                            new SetAltTexture(1),
                            new Taunt("Ha...ha........hahahahahaha! You will make a fine sacrifice!"),
                            new TimedTransition(3000, "Comment2")
                        ),
                        new SetAltTexture(1),
                        new State("Comment2",
                            new Taunt("You will find that it was...unwise...to wake me."),
                            new TimedTransition(1000, "Comment3")
                        ),
                        new State("Comment3",
                            new SetAltTexture(1),
                            new Taunt("Let us see what can conjure up!"),
                            new TimedTransition(1000, "Comment4")
                        ),
                        new State("Comment4",
                            new SetAltTexture(1),
                            new Taunt("I will freeze the life from you!"),
                            new TimedTransition(1000, "Blue1")
                        )
                    ),
                    new State("Blue1",
                        new SetAltTexture(2),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TossObject("shtrs Ice Portal", 4, 90, 100000000),
                        new Spawn("shtrs Ice Shield", 1, 1, 1000000000),
                        new TimedTransition(2000, "checkSphere")
                    ),
                    new State("checkSphere",
                        new EntityNotExistsTransition("shtrs Ice Shield", 15, "Spawn Birds")
                    ),
                    new State("Spawn Birds",
                        new Taunt("You leave me no choice...Inferno! Blizzard!"),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new InvisiToss("shtrs Inferno", 3, 0, 1000000000, 7000),
                        new InvisiToss("shtrs Blizzard", 3, 180, 1000000000, 7000),
                        new Order(15, "shtrs MagiGenerators", "Hide"),
                        new TimedTransition(8000, "wait")
                    ),
                    new State("wait",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityNotExistsTransition2("shtrs Inferno", "shtrs Blizzard", 15, "Change")
                    ),
                    new State("Change",
                        new SetAltTexture(2),
                        new ChangeSize(100, 200),
                        new Taunt("Your souls feed my King."),
                        new TimedTransition(3000, "Active 1")
                    ),
                    new State("Active 1",
                        new Taunt("Darkness give me strength!"),
                        new MoveTo(6, 0),
                        new Order(1, "shtrs MagiGenerators", "Despawn"),
                        new TimedTransition(4000, "Active2")
                    ),
                    new State("Active2",
                        new MoveTo(0, 4, 1.5),
                        new Order(1, "shtrs MagiGenerators", "Despawn"),
                        new Taunt("THE POWER...IT CONSUMES ME!"),
                        new Shoot(15, 20, projectileIndex:2, coolDown:100000000, coolDownOffset:5000),
                        new Shoot(15, 20, projectileIndex: 3, coolDown: 100000000, coolDownOffset: 5000),
                        new Shoot(15, 20, projectileIndex: 4, coolDown: 100000000, coolDownOffset: 5100),
                        new Shoot(15, 20, projectileIndex: 2, coolDown: 100000000, coolDownOffset: 5200),
                        new Shoot(15, 20, projectileIndex: 5, coolDown: 100000000, coolDownOffset: 5350),
                        new Shoot(15, 20, projectileIndex: 6, coolDown: 100000000, coolDownOffset: 5400),
                        new TimedTransition(6000, "Active3")
                    ),
                    new State("Active3",
                        new MoveTo(8, 0, 1.5),
                        new Order(1, "shtrs MagiGenerators", "Despawn"),
                        new Taunt("THE POWER...IT CONSUMES ME!"),
                        new Shoot(15, 20, projectileIndex: 2, coolDown: 100000000, coolDownOffset: 5000),
                        new Shoot(15, 20, projectileIndex: 3, coolDown: 100000000, coolDownOffset: 5000),
                        new Shoot(15, 20, projectileIndex: 4, coolDown: 100000000, coolDownOffset: 5100),
                        new Shoot(15, 20, projectileIndex: 2, coolDown: 100000000, coolDownOffset: 5200),
                        new Shoot(15, 20, projectileIndex: 5, coolDown: 100000000, coolDownOffset: 5350),
                        new Shoot(15, 20, projectileIndex: 6, coolDown: 100000000, coolDownOffset: 5400)
                    ),
                    new State("Death",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt("I...will........retuuurr...n...n....."),
                        new Shoot(15, 12, projectileIndex:5, coolDown:1000000, coolDownOffset:30000),
                        new CopyDamageOnDeath("shtrs Encounter Chest"),
                        new Order(10, "shtrs encounterchestspawner", "Spawn"),
                        new Suicide()
                    )
                )
            )
            .Init("shtrs The Forgotten King",
                new State(
                    new SetLootState("forgottenKing"),
                    new CopyLootState("shtrs encounterchestspawner", 20),

                    new State("Idle",
                        new HpLessTransition(0.1, "Death")
                    ),

                    new State("Death",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new CopyDamageOnDeath("shtrs Encounter Chest"),
                        new Order(10, "shtrs encounterchestspawner", "Spawn"),
                        new Suicide()
                    )
                )
            )
            .Init("shtrs blobomb maker",
                new State(
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invincible)
                    ),
                    new State("Spawn",
                        new Spawn("shtrs Blobomb", coolDown: 1000),
                        new TimedTransition(6000, "Idle")
                    )
                )
            )
            .Init("shtrs encounterchestspawner",
                new State(
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invincible, true)
                    ),
                    new State("Spawn",
                        new Spawn("shtrs Encounter Chest", 1, 1),
                        new CopyLootState("shtrs Encounter Chest", 10),
                        new TimedTransition(5000, "Idle")
                    )
                )
            )
         
            .Init("shtrs Encounter Chest",
                new State(
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(5000, "UnsetEffect")
                    ),
                    new State("UnsetEffect")
                ),
                new Threshold(0.15,
                    new TierLoot(11, ItemType.Weapon, 0.06),
                    new TierLoot(12, ItemType.Weapon, 0.05),
                    new TierLoot(6, ItemType.Ability, 0.05),
                    new TierLoot(12, ItemType.Armor, 0.06),
                    new TierLoot(13, ItemType.Armor, 0.05),
                    new TierLoot(6, ItemType.Ring, 0.06),
                    new ItemLoot("Potion of Attack", 1),
                    new ItemLoot("Potion of Defense", 0.5),
                    new ItemLoot("The Tower Tarot Card", 0.04),
                    new ItemLoot("Bracer of the Guardian", 0.01) //jacob
                )
            )

            .Init("shtrs Inferno",
                new State(
                    new Follow(1, range: 1, coolDown: 1000),
                    new Orbit(1, 4, 15, "shtrs Blizzard")
                )
            )

            .Init("shtrs Blizzard",
                new State(
                    new Follow(1, range: 1, coolDown: 1000),
                    new Orbit(1, 4, 15, "shtrs Inferno")
                )
            )
        .Init("shtrs Bridge Obelisk C",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose"
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ),
                new State("active",
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Paladin", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            )
        .Init("shtrs Bridge Obelisk F",
             new State(
                 new State(
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                new State("waittillclose"
                    ),
                new State("activeghost",
                    new TimedTransition(20000, "active")
                        ),
                new State("active",
                    new Flash(0xFF0000, 2, 2),
                    new TimedTransition(8000, "rekt")
                        )
                     ),
                 new State("rekt",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                    new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                     new TimedTransition(4000, "rekt1")
                    ),
                 new State("rekt1",
                     new ConditionalEffect(ConditionEffectIndex.Armored),
                      new TimedTransition(10000, "rekt3"),
                     new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1),
                     new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1),
                 new State("SpawnOff",
                     new PlayerWithinTransition(5, "SpawnOn")
                     ),
                 new State("SpawnOn",
                     new Spawn("shtrs Stone Paladin", 1, 1, coolDown: 99999),
                     new NoPlayerWithinTransition(5, "SpawnOff")
                     )
                    ),
                 new State("rekt3",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new TimedTransition(6000, "rekt4")
                   ),
                 new State("rekt4",
                     new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                     new Flash(0xFF0000, 2, 2),
                     new TimedTransition(6000, "rekt")
                   )
                )
            );
    }
}
