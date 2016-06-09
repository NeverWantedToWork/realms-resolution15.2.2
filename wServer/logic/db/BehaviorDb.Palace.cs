using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm;
using wServer.logic.behaviors;
using wServer.logic.loot;
using wServer.logic.transitions;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        _ Palace = () => Behav()
        //TRoom Boss
          .Init("Construct of the Storm",
            new State(
                    new State("GetClose",
                        new PlayerWithinTransition(8, "state1")
                        ),
                    new State("state1",
                         new ConditionalEffect(ConditionEffectIndex.Armored),
                     new Taunt(1.00, "Fine. I must keep myself concealed!"),
                     new Shoot(6.3, count: 4, projectileIndex: 3, coolDown: 1700),
                     new Shoot(6.3, count: 6, projectileIndex: 2, coolDown: 3750),
                     new Grenade(6, 100, range: 8, fixedAngle: 90, coolDown: 3000),
                      new Grenade(6, 100, range: 8, fixedAngle: 0, coolDown: 3000),
                       new Grenade(6, 100, range: 8, fixedAngle: 180, coolDown: 3000),
                       new Grenade(6, 100, range: 8, fixedAngle: 270, coolDown: 3000),
                       new TimedTransition(10000, "state2"),
                       new HpLessTransition(0.30, "state2"),
                        new SetAltTexture(1),
                         new Shoot(6.3, count: 8, shootAngle: 60, projectileIndex: 2, coolDown: 1000)
                        ),
                    new State("state2",
                           new Taunt(1.00, "This form is sure to take you down!"),
                           new SetAltTexture(2),
                         new Wander(0.2),
                         new Shoot(6.3, count: 1, projectileIndex: 3, coolDown: 1700),
                         new Shoot(6.3, count: 10, projectileIndex: 0, coolDown: 5000),
                         new Shoot(6.3, count: 10, projectileIndex: 1, coolDown: 6000),
                        new ChangeSize(15, 140)
                        )
                ),
                new Threshold(0.18,
                    new ItemLoot("Potion of Speed", 0.8)
                )
            )
          .Init("Hideout Thunder Walker",
                new State(
                      new Wander(0.35),
                     new Shoot(8.4, count: 4, shootAngle: 10, projectileIndex: 0, predictive: 6, coolDown: 2500),
                     new Shoot(8.4, count: 2, projectileIndex: 0, coolDown: 1000),
                     new Grenade(1.5, 150, 4, coolDown: 3000)
                    )
            )
          .Init("Hideout Shocker Knight",
                new State(
                     new Follow(1, 8, 1),
                     new Shoot(8.4, count: 1, shootAngle: 5, projectileIndex: 0, coolDown: 1800)
                    )
            )
         .Init("Hideout Electric Overseer",
                        new State(
                    new State("Electrical",
                        new Swirl(0.65, 4, targeted: false),
                        new Shoot(8.4, count: 1, fixedAngle: 0, projectileIndex: 0, coolDown: 1000),
                        new Shoot(8.4, count: 1, fixedAngle: 90, projectileIndex: 0, coolDown: 1000),
                        new Shoot(8.4, count: 1, fixedAngle: 180, projectileIndex: 0, coolDown: 1000),
                        new Shoot(8.4, count: 1, fixedAngle: 270, projectileIndex: 0, coolDown: 1000),
                    new HpLessTransition(0.35, "GetHelp")
                        ),
                    new State("GetHelp",
                        new Taunt(0.90, "The eye of the storm will consume you!"),
                         new Taunt(0.25, "I call upon the great Weather God to aid me!"),
                       new Spawn("Hideout Thunder Walker", initialSpawn: 1, maxChildren: 2, coolDown: 2000),
                       new Shoot(8.4, count: 2, projectileIndex: 0, coolDown: 1000),
                       new Follow(0.3, 8, 1)
                    )))
          .Init("Hideout Wraith",
                        new State(
                              new HpLessTransition(0.35, "WraithSmall"),
                    new State("WraithWalk",
                       new Follow(0.5, 8, 1),
                       new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                       new Shoot(8.4, count: 1, projectileIndex: 0, coolDown: 10),
                       new TimedTransition(6000, "WraithRun")

                        ),
                    new State("WraithRun",
                       new StayBack(0.8, 4),
                       new Shoot(8.4, count: 10, projectileIndex: 1, shootAngle: 60, coolDown: 1700),
                            new TimedTransition(6000, "WraithWalk")
                        ),
                       new State("WraithSmall",
                        new ChangeSize(-15, 20),
                        new Flash(0xFFFFFF, 0.2, 5),
                        new Shoot(8.4, count: 8, projectileIndex: 1, coolDown: 745),
                        new Follow(0.92, 8, 1)
                        )
                ),
                new Threshold(0.6,
                    new ItemLoot("Cloudflash Scepter", 0.01),
                     new ItemLoot("Mithril Armor", 0.01),
                     new ItemLoot("Wand of Death", 0.01)
                    )
            )
        //Cloud Defenders and Attackers


        //Defender
         .Init("Hideout Cloud Shield 2",
                    new State(
                        new Orbit(0.4, 3, 20, "Iegon the Weather God"),
                    new State("Defender1",
                        
                    new TimedTransition(4500, "Defender2")
                        ),
                    new State("Defender2",
                       new TimedTransition(3000, "Defender1"),
                       new ConditionalEffect(ConditionEffectIndex.Invulnerable)
                    )))
        //Attacker
                      .Init("Hideout Cloud Shield",
                new State(
                     new Follow(0.6, 8, 1),
                     new Shoot(8.4, count: 1, projectileIndex: 0, coolDown: 1000)
                    ))
            
                  .Init("Iegon the Weather God",
                        new State(
                            new HpLessTransition(0.25, "rage"),
                    new State("Initiate",
                    new PlayerWithinTransition(8, "Defending")
                        ),
                    new State("Defending",
                        new Taunt(1.00, "Infiltrating my palace? You should be ashamed of yourself!"),
                        new Spawn("Hideout Cloud Shield 2", initialSpawn: 1, maxChildren: 3, coolDown: 99999),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntitiesNotExistsTransition(9999, "Fight1", "Hideout Cloud Shield 2")
                        ),
                       new State("Fight1",
                           new Wander(0.3),
                        new Taunt(1.00, "Fighting me must be pure sport for you? You aren't getting anywhere."),
                        new Flash(0x0000FF, 0.2, 5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Shoot(8.4, count: 2, fixedAngle: 90, projectileIndex: 1, coolDown: 1000),
                         new Shoot(8.4, count: 2, fixedAngle: 180, projectileIndex: 1, coolDown: 2000),
                          new Shoot(8.4, count: 2, fixedAngle: 270, projectileIndex: 1, coolDown: 3000),
                           new Shoot(8.4, count: 2, fixedAngle: 0, projectileIndex: 1, coolDown: 4000),
                           new Shoot(8.4, count: 1, fixedAngle: 45, projectileIndex: 2, coolDown: 500),
                           new Shoot(8.4, count: 1, fixedAngle: 125, projectileIndex: 2, coolDown: 1000),
                           new Shoot(8.4, count: 1, fixedAngle: 210, projectileIndex: 2, coolDown: 1500),
                           new Shoot(8.4, count: 1, fixedAngle: 300, projectileIndex: 2, coolDown: 2000),
                            new TimedTransition(8500, "Fight2")
                           ),
                        new State("Fight2",
                            new Wander(0.3),
                             new Shoot(10, count: 4, projectileIndex: 3, predictive: 1, coolDown: 2970),
                               new Shoot(10, count: 6, projectileIndex: 4, predictive: 8, coolDown: 2700),
                                new Shoot(10, count: 2, projectileIndex: 3, coolDown: 600),
                                new Shoot(10, projectileIndex: 0, coolDown: 700),
                        new TimedTransition(8500, "Attacking")
                            ),
                                            new State("Attacking",
                        new Taunt(1.00, "This will get ya! "),
                        new Spawn("Hideout Cloud Shield", initialSpawn: 1, maxChildren: 3, coolDown: 99999),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntitiesNotExistsTransition(9999, "Fight3", "Hideout Cloud Shield")
                                                ),
                      new State("Fight3",
                          new Wander(0.4),
                      new Taunt(1.00, "Time to get completely obliterated!"),
                      new Shoot(8.4, count: 2, projectileIndex: 2, coolDown: 2400),
                       new Shoot(8.4, count: 4, projectileIndex: 4, shootAngle: 30, coolDown: 2000),
                      new TimedTransition(5250, "Fight4")
                            ),
                          new State("Fight4",
                              new Wander(0.3),
                      new Taunt(1.00, "Is it just me or are you about to get destroyed?!"),
                       new Shoot(8.4, count: 2, projectileIndex: 3, shootAngle: 30, coolDown: 1000),
                       new Shoot(8.4, count: 3, projectileIndex: 1, shootAngle: 30, coolDown: 1250),
                      new TimedTransition(6750, "Defending")
                            ),
                        new State("rage",
                             new Shoot(10, count: 3, projectileIndex: 2,  coolDown: 1800),
                            new Shoot(10, count: 4, projectileIndex: 2, coolDown: 1500),
                             new Shoot(10, projectileIndex: 0, coolDown: 700),
                             new Follow(0.65, 8, 1)
                            
                        )
                ),
                new Threshold(0.18,
                new TierLoot(8, ItemType.Weapon, 0.04),
                new TierLoot(10, ItemType.Weapon, 0.02),
                new TierLoot(8, ItemType.Weapon, 0.01),
                new TierLoot(10, ItemType.Armor, 0.04),
                new TierLoot(11, ItemType.Armor, 0.02),
                new TierLoot(10, ItemType.Armor, 0.01),
                new TierLoot(4, ItemType.Ring, 0.005),
                    new ItemLoot("Potion of Attack", 1),
                    new ItemLoot("Potion of Speed", 0.8),
                    new ItemLoot("Dagger of Unearthly Storms", 0.014),
                    new ItemLoot("Stormbreaker", 0.01)
                    )
            )
        ;
    }
}