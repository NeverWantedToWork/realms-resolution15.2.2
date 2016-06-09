using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ Crimson = () => Behav()
            .Init("Hades",
                new State(
                     new TransformOnDeath("Hades Test Chest", 1, 1, 1),
                    new HpLessTransition(0.14, "Dead1"),
                    new State("default",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new PlayerWithinTransition(8, "talktransition")
                        ),
                     new State("talktransition",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt(1.00, "Hm?"),
                        new TimedTransition(4500, "talktransition2")
                        ),
                   new State("talktransition2",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt(1.00, "This is quite suprising, didn't expect guest to show up."),
                        new TimedTransition(6500, "talktransition3")
                        ),
                   new State("talktransition3",
                        new Flash(0xFF0000, 2, 2),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt(1.00, "Well, you have chose this route. Death."),
                        new TimedTransition(5500, "FranticFlamePhase")
                        ),
                    new State("FranticFlamePhase",
                        new Taunt(1.00, "Feel that? Yes, that is fear."),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Shoot(10, count: 8, shootAngle: 16, projectileIndex: 2, coolDown: 40),
                        new Shoot(10, count: 12, projectileIndex: 0, coolDown: 1000),
                        new TimedTransition(5000, "FranticFlamePhase2")
                        ),
                    new State("FranticFlamePhase2",
                        new Shoot(10, count: 9, shootAngle: 16, projectileIndex: 2, coolDown: 40),
                        new Shoot(10, count: 13, projectileIndex: 0, coolDown: 700),
                        new TimedTransition(5000, "PerishingPhase")
                        ),
                    new State("PerishingPhase",
                        new Taunt(1.00, "I don't believe you will survive very long in here. The longer the last the closer to you being perished."),
                        new Wander(0.1),
                        new Shoot(10, count: 6, projectileIndex: 1, coolDown: 2750),
                        new Shoot(10, count: 4, shootAngle: 20, predictive: 2, projectileIndex: 0, coolDown: 400),
                        new TimedTransition(8000, "LockingTargetPhaseStarting")
                        ),
                    new State("LockingTargetPhaseStarting",
                        new Flash(0xFF0000, 2, 2),
                        new Shoot(10, count: 12, projectileIndex: 3, coolDown: 775),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new RemoveEntity(9999, "HadesTarget"),
                        new Taunt(1.00, "I must charge my staff! Stand back fiend!"),
                        new TimedTransition(6750, "LockingTargetPhase")
                        ),
                    new State("LockingTargetPhase",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt(1.00, "My staff is now charged! Nothing can save you now!"),
                        new TossObject("HadesTarget", angle: null, range: 10, coolDown: 350),
                        new TimedTransition(7850, "RushingShotgunWarn")
                        ),
                    new State("RushingShotgunWarn",
                        new Flash(0xFF00FF, 2, 2),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(1700, "RushingShotgunPhase")
                        ),
                    new State("RushingShotgunPhase",
                        new Follow(0.4, 8, 1),
                        new Shoot(10, count: 3, shootAngle: 16, projectileIndex: 2, coolDown: 100),
                        new Shoot(10, count: 4, shootAngle: 8, projectileIndex: 3, coolDown: 1750, fixedAngle: 135),
                        new Shoot(10, count: 4, shootAngle: 8, projectileIndex: 3, coolDown: 1750, fixedAngle: 45),
                        new Shoot(10, count: 4, shootAngle: 8, projectileIndex: 3, coolDown: 1750, fixedAngle: 225),
                        new Shoot(10, count: 4, shootAngle: 8, projectileIndex: 3, coolDown: 1750, fixedAngle: 315),
                        new TimedTransition(8000, "RushingShotgun2Phase")
                        ),
                   new State("RushingShotgun2Phase",
                        new Taunt(1.00, "Ahahaha! Feel the powerful force of the underworld!"),
                        new Follow(0.65, 8, 1),
                        new Shoot(10, count: 3, shootAngle: 30, projectileIndex: 1, coolDown: 2350),
                        new Shoot(10, count: 12, shootAngle: 4, projectileIndex: 4, coolDown: 1000),
                        new TimedTransition(8000, "RushingShotgun3Phase")
                        ),
                   new State("RushingShotgun3Phase",
                        new Taunt(1.00, "ADMIT YOUR DOOM!"),
                        new Follow(0.15, 8, 1),
                        new Shoot(10, count: 1, projectileIndex: 1, coolDown: 1),
                        new TimedTransition(4000, "GetReadyMoveToCenter1")
                        ),
                    new State("GetReadyMoveToCenter1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "MoveToCenter1")
                        ),
                    new State("MoveToCenter1",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new MoveTo(15, 12, speed: 1, isMapPosition: true, once: true),
                        new TimedTransition(2000, "GetReadyMoveToSpawn")
                        ),
                    new State("GetReadyMoveToSpawn",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(3000, "SpawnFollowerPhase")
                        ),
                    new State("SpawnFollowerPhase",
                        new Taunt(1.00, "My defenders will fight for me!"),
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 0),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 180),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 270),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 45),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 90),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 315),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 215),
                        new InvisiToss("HadesFollower", range: 4, coolDown: 99999, angle: 135),
                        new TimedTransition(3000, "WaitingForDeads")
                        ),
                   new State("WaitingForDeads",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),                        
                        new Shoot(10, count: 12, projectileIndex: 4, coolDown: 400),
                        new EntitiesNotExistsTransition(9999, "FranticFlamePhase", "HadesFollower")
                        ),
                   new State("Dead1",
                        new Flash(0xFFFFF, 2, 2),
                        new Taunt(1.00, "What!? The SOULS that GIVE ME ENERGY ARE GONE! ARAAAGAAAGH!"),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new TimedTransition(3000, "ded")
                        ),
                   new State("ded",
                        new Shoot(10, count: 10, projectileIndex: 2, coolDown: 99999),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Suicide()
                        )
                    )
            )

            .Init("HadesFollower",
                new State(
                    new HpLessTransition(0.1, "BlowUp"),
                    new State("Start",
                        new Flash(0xFF0000, 2, 2),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new ChangeSize(40, 140),
                        new TimedTransition(1500, "Follow")
                        ),
                    new State("Follow",
                        new Prioritize(
                            new Follow(0.5, 8, 1),
                            new Wander(0.45)
                            ),
                        new Shoot(10, count: 8, projectileIndex: 1, coolDown: 900),
                        new Shoot(8.4, count: 4, projectileIndex: 0, coolDown: 1800)
                        ),
                  new State("BlowUp",
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(10, count: 16, projectileIndex: 0, coolDown: 9999),
                        new Suicide()
                        )
                  )
            )

                                    .Init("HadesTarget",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new State("Seek",
                               new Sequence(
                            new Timed(2000,
                                new Prioritize(
                                    new Follow(0.5, 8, 1),
                                    new Wander(0.7)
                                    )),
                            new Timed(2000,
                                new Prioritize(
                                    new Charge(1.4, 6, coolDown: 1150),
                                    new Swirl(1, 4, targeted: false)
                                    )),
                            new Timed(1000,
                                new Prioritize(
                                    new Orbit(0.55, 5),
                                    new Wander(0.8)
                                    )
                                )
                            ),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new PlayerWithinTransition(1, "Countdown")
                        ),
                    new State("Countdown",
                        new SetAltTexture(1),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(10, count: 6, projectileIndex: 0, coolDown: 750),
                        new TimedTransition(2850, "Blam")
                        ),
                   new State("Blam",
                        new SetAltTexture(1),
                        new ConditionalEffect(ConditionEffectIndex.Invincible),
                        new Shoot(10, count: 11, projectileIndex: 1, coolDown: 9999),
                        new Suicide()
                        )
                  )
            )
                      .Init("Hades Test Chest",
                new State(
                    new State("Idle",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new TimedTransition(5000, "UnsetEffect")
                    ),
                    new State("UnsetEffect")
                ),
                new Threshold(0.15,
                new TierLoot(12, ItemType.Weapon, 0.045),
                new TierLoot(11, ItemType.Weapon, 0.05),
                new TierLoot(6, ItemType.Ability, 0.045),
                new TierLoot(12, ItemType.Armor, 0.05),
                new ItemLoot("Crimson Steel Lockbox", 0.048),
                new ItemLoot("Greater Potion of Vitality", 1),
                new ItemLoot("Greater Potion of Life", 0.6),
                new ItemLoot("Truncheon of Immortal Demons", 0.01),
                new ItemLoot("Coat of the Devil", 0.01),
                new ItemLoot("Skull of Hades", 0.01),
                new ItemLoot("Hellslicer", 0.03),
                new ItemLoot("The Eye of Peril", 0.01)
                )
            );

        
    }
}