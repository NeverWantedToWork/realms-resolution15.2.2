using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ PorkHut = () => Behav()
            .Init("Hornkers",
                new State(
                    new State("default",
                        new PlayerWithinTransition(8, "taunt")
                        ),
                    new State("taunt",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new Taunt("Oink Oink!"),
                        new TimedTransition(2500, "Fight")
                        ),
                    new State("Fight",
                        new Prioritize(
                            new Follow(1, 8, 1),
                            new Wander(1)
                            ),
                        new Shoot(8, count: 6, shootAngle: 16, projectileIndex: 0, coolDown: 3600),
                        new Shoot(8, count: 3, shootAngle: 28, projectileIndex: 1, coolDown: 8000, coolDownOffset: 2000),
                        new Shoot(8, count: 1, projectileIndex: 2, coolDown: 1000, coolDownOffset: 2000),
                        new TimedTransition(8000, "Fight1")
                        ),
                    new State("Fight1",
                        new Taunt("OINK!"),
                        new Flash(0xFF0000, 1, 2),
                        new Wander(0.5),
                        new ConditionalEffect(ConditionEffectIndex.Armored),
                        new TossObject("Hornkers Minion", 10, coolDown: 1000),
                        new Shoot(8, count: 10, projectileIndex: 3, coolDown: 2000),
                        new TimedTransition(8000, "Fight")
                        )
                    ),
                new MostDamagers(3,
                    new ItemLoot("Potion of Vitality", 1.0)
                ),
                new MostDamagers(1,
                    new ItemLoot("Potion of Wisdom", 1.0)
                ),
                new Threshold(0.025,
                    new TierLoot(10, ItemType.Weapon, 0.1),
                    new TierLoot(4, ItemType.Ability, 0.1),
                    new TierLoot(9, ItemType.Armor, 0.1),
                    new TierLoot(3, ItemType.Ring, 0.05),
                    new TierLoot(10, ItemType.Armor, 0.05),
                    new TierLoot(10, ItemType.Weapon, 0.05),
                    new TierLoot(4, ItemType.Ring, 0.025),
                    new ItemLoot("Pork Sword", 0.045),
                    new ItemLoot("Pork Killer", 0.045)
                )
            )
            .Init("Hornkers Minion",
                new State(
                    new Prioritize(
                        new Follow(1, 8, 1),
                        new Wander(0.25)
                        ),
                    new Shoot(8, 5, shootAngle: 10, coolDown: 800)
                    )
            )
            ;
    }
}