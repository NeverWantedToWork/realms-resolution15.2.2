﻿using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Crypt = () => Behav()
            .Init("TF The Fallen",
                new State(
                    new State(
                        new HpLessTransition(0.13, "rip1"),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new State("idle",
                        new EntitiesNotExistsTransition(9999, "taunt", "TF Creature Wizard", "TF Warrior", "TF Knight 1", "TF KNight 2")
                        ),
                    new State("taunt",
                        new MoveTo(58, 12, speed: 0.8, isMapPosition: true, once: true),
                        new Taunt(true, "Come. You shall be destroyed by the hands of me.", "We do not have mercy on your souls nor does our leader. Die.", "Our time of awakening has come. Join us or be slayed."),
                        new TimedTransition(10000, "waitforplayers")
                        ),
                    new State("waitforplayers",
                        new PlayerWithinTransition(8, "plantspookstart")
                        ),
                    new State("plantspookstart",
                        new Taunt("Ah. The time has come. The time for the end of the human race. Consuming your spirits will make me more powerful than ever!"),
                        new InvisiToss("TF Sector", 8, 0, coolDown: 9999999),
                        new InvisiToss("TF Sector", 8, 180, coolDown: 9999999),
                        new TimedTransition(5000, "fight1")
                        )
                      ),
                    new State("fight1",
                        new Shoot(8.4, count: 1, projectileIndex: 3, coolDown: 1),
                        new Shoot(8.4, count: 5, projectileIndex: 0, coolDown: 2000),
                        new TimedTransition(8000, "fight2")
                        ),
                    new State("fight2",
                        new Shoot(8.4, count: 8, shootAngle: 16, projectileIndex: 2, coolDown: 2000),
                        new Shoot(8.4, count: 4, shootAngle: 16, predictive: 4, projectileIndex: 0, coolDown: 2000),
                        new TimedTransition(8000, "fight3")
                        ),
                    new State("fight3",
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new Shoot(8.4, count: 5, projectileIndex: 3, coolDown: 2000),
                        new Shoot(8.4, count: 8, shootAngle: 12, predictive: 2, projectileIndex: 2, coolDown: 1),
                        new TimedTransition(10000, "fight1"),
                        new State("Quadforce1",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 0, coolDown: 100),
                            new TimedTransition(200, "Quadforce2")
                        ),
                        new State("Quadforce2",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 15, coolDown: 100),
                            new TimedTransition(200, "Quadforce3")
                        ),
                        new State("Quadforce3",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 30, coolDown: 100),
                            new TimedTransition(200, "Quadforce4")
                        ),
                        new State("Quadforce4",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 45, coolDown: 100),
                            new TimedTransition(200, "Quadforce5")
                        ),
                        new State("Quadforce5",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 45, coolDown: 100),
                            new TimedTransition(200, "Quadforce6")
                        ),
                        new State("Quadforce6",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 30, coolDown: 100),
                            new TimedTransition(200, "Quadforce7")
                        ),
                        new State("Quadforce7",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 15, coolDown: 100),
                            new TimedTransition(200, "Quadforce8")
                        ),
                        new State("Quadforce8",
                            new Shoot(0, projectileIndex: 4, count: 5, shootAngle: 60, fixedAngle: 0, coolDown: 100),
                            new TimedTransition(200, "Quadforce1")
                        )
                       ),
                    new State("rip1",
                        new RemoveEntity(9999, "TF Sector"),
                        new Taunt("I NEVER THOUGHT I WOULD SEE THE END OF ME!", "THE REST OF MY POWER...IT FADES AWAY!", "GAAAAAAR AAAAGH!"),
                        new Flash(0xFFFF00, 2, 4),
                        new TimedTransition(4000, "rip2")
                        ),
                    new State("rip2",
                        new Suicide()
                        )
                    ),
                new MostDamagers(3,
                    new ItemLoot("Greater Potion of Life", 1.0),
                    new ItemLoot("Greater Potion of Dexterity", 1.0)
                ),
                new MostDamagers(1,
                    new ItemLoot("Greater Potion of Wisdom", 1.0)
                ),
                new Threshold(0.025,
                    new TierLoot(10, ItemType.Weapon, 0.07),
                    new TierLoot(11, ItemType.Weapon, 0.06),
                    new TierLoot(12, ItemType.Weapon, 0.05),
                    new TierLoot(5, ItemType.Ability, 0.07),
                    new TierLoot(6, ItemType.Ability, 0.05),
                    new TierLoot(11, ItemType.Armor, 0.07),
                    new TierLoot(12, ItemType.Armor, 0.06),
                    new TierLoot(13, ItemType.Armor, 0.05),
                    new TierLoot(6, ItemType.Ring, 0.06),
                    new ItemLoot("Menacing Sword of No Escape", 0.05),
                    new ItemLoot("Battleplate of Sacred Warlords", 0.05),
                    new ItemLoot("Gold Cache", 0.05),
                    new ItemLoot("Revulsion Sphere", 0.10),
                    new ItemLoot("Onrane", 0.50),
                    new ItemLoot("Dark Matter", 0.10)
                )
            )
            .Init("TF Knight 1",
                new State(
                    new Prioritize(
                        new Follow(0.8, 8, 1),
                        new Wander(0.25)
                        ),
                    new Shoot(8, 1, shootAngle: 10, coolDown: 1000),
                    new Shoot(8, 1, predictive: 3, projectileIndex: 1, coolDown: 2000)
                    ),
                new ItemLoot("Health Potion", 0.1),
                new Threshold(0.1,
                    new ItemLoot("Obsidian Dagger", 0.02),
                    new ItemLoot("Mithril Shield", 0.015)
                    )
            )
        .Init("TF Knight 2",
                new State(
                    new Prioritize(
                        new Follow(0.8, 8, 1),
                        new Wander(0.25)
                        ),
                    new Shoot(8, 1, shootAngle: 10, coolDown: 1000),
                    new Shoot(8, 1, predictive: 3, projectileIndex: 1, coolDown: 2000)
                    ),
                new ItemLoot("Health Potion", 0.1)
            )
        .Init("TF Warrior",
                new State(
                    new Prioritize(
                        new Follow(1.5, 8, 1),
                        new Wander(0.25)
                        ),
                    new Shoot(8, 1, shootAngle: 10, coolDown: 500),
                    new Shoot(8, 6, projectileIndex: 1, coolDown: 4000)
                    ),
                new ItemLoot("Magic Potion", 0.1)
            )
            .Init("TF Creature Wizard",
            new State(
                new State("Shoot1",
                     new Wander(1),
                     new Shoot(8.4, count: 2, projectileIndex: 0, coolDown: 600),
                     new TimedTransition(5000, "Shoot2")
                    ),
                new State("Shoot2",
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.25)
                        ),
                         new Shoot(8.4, count: 8, projectileIndex: 1, coolDown: 2500),
                         new Shoot(8.4, count: 1, projectileIndex: 2, coolDown: 1000),
                     new TimedTransition(5000, "Shoot1")
                    )
                )
            )
        .Init("TF Sector",
            new State(
                new Orbit(0.35, 8, 20, "TF The Fallen"),
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("recker",
                    new TimedTransition(12000, "GoDumb"),
                        new State("Quadforce1",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 0, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce2")
                        ),
                        new State("Quadforce2",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 15, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce3")
                        ),
                        new State("Quadforce3",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 30, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce4")
                        ),
                        new State("Quadforce4",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 45, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce5")
                        ),
                        new State("Quadforce5",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 45, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce6")
                        ),
                        new State("Quadforce6",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 30, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce7")
                        ),
                        new State("Quadforce7",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 15, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce8")
                        ),
                        new State("Quadforce8",
                            new Shoot(0, projectileIndex: 0, count: 5, shootAngle: 60, fixedAngle: 0, coolDown: 1400),
                            new TimedTransition(1400, "Quadforce1")
                        )
                    ),
                new State("GoDumb",
                         new Shoot(8.4, count: 8, shootAngle: 24, projectileIndex: 0, coolDown: 2500),
                         new Grenade(3, 75, range: 8, coolDown: 4000),
                         new TimedTransition(8000, "recker")
                    )
                )
            )
            ;
    }
}