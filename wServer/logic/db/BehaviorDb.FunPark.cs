using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ FunPark = () => Behav()
          .Init("Evil Clown 1",
                new State(
                    new Wander(0.575),
                    new Shoot(8.4, count: 3, shootAngle: 16, projectileIndex: 0, coolDown: 500)
                    ),
                new ItemLoot("Health Potion", 0.1),
                new ItemLoot("Magic Potion", 0.1)
            )
        .Init("Evil Clown 2",
                new State(
                    new Wander(0.675),
                    new Shoot(8.4, count: 1, projectileIndex: 0, coolDown: 750)
                    )
            )
        .Init("Ticket Master",
                new State(
                    new Prioritize(
                        new Follow(0.25, 8, 1),
                        new Wander(0.6)
                        ),
                    new Shoot(8.4, count: 10, projectileIndex: 0, coolDown: 1550)
                    )
            )
        .Init("Evil Balloon",
                new State(
                    new Prioritize(
                        new StayBack(0.35, 4),
                        new Wander(0.6)
                        ),
                    new Shoot(8.4, count: 1, projectileIndex: 0, coolDown: new Cooldown(3450, 1250))
                    )
            )
        .Init("Fun Ferris Wheel",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible)
                    )
            )
         .Init("Jeffery the Booty Clown",
                new State(
                    new EntitiesNotExistsTransition(9999, "nowheels", "Fun Ferris Wheel"),
                    new RealmPortalDrop(),
                    new State("default",
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new PlayerWithinTransition(8, "fight")
                        ),
                    new State(
                        new ConditionalEffect(ConditionEffectIndex.Invulnerable),
                        new EntityExistsTransition("Fun Ferris Wheel", 6, "orbitthewheel"),
                    new State("fight",
                        new Wander(0.06),
                        new Taunt(1.00, "Wanna hear a joke! You are a good player at RotMG! Hahahaha!", "I'm a funny guy!", "I'm just a JOKESTER!", "It's Comedy GOLD!", "Pull my finger!", "I'm going to slice up the cheese!"),
                        new Shoot(8.4, count: 8, shootAngle: 20, projectileIndex: 1, coolDown: 550),
                        new Shoot(8.4, count: 3, shootAngle: 20, projectileIndex: 2, coolDown: 850),
                        new TimedTransition(5000, "fight2")
                        ),
                    new State("fight2",
                        new Wander(0.1),
                        new Flash(0xFF0000, 2, 2),
                        new Taunt(1.00, "I'm the BEES KNEES!", "Why did the chicken cross the road! BECAUSE YOU ARE BAD! HA!", "There is always fun in the fun park!"),
                        new Shoot(8.4, count: 9, shootAngle: 10, projectileIndex: 0, coolDown: 30),
                        new TimedTransition(3250, "rush")
                        ),
                    new State("rush",
                        new Taunt(1.00, "Let's be BESTIES", "COME HERE! Let me give you a balloon animal!", "TICKLES BICKLES!"),
                        new Prioritize(
                             new Follow(1.3, 8, 1),
                             new Wander(0.06)
                            ),
                        new Shoot(8.4, count: 5, projectileIndex: 2, coolDown: 1000),
                        new Shoot(8.4, count: 7, projectileIndex: 0, coolDown: 3000),
                        new Shoot(8.4, count: 8, shootAngle: 10, projectileIndex: 3, coolDown: 750),
                        new TimedTransition(12000, "fight")
                        )
                      ),
                    new State("orbitthewheel",
                        new Orbit(0.3, 3, 20, "Fun Ferris Wheel"),
                        new Shoot(8.4, count: 7, shootAngle: 18, projectileIndex: 0, coolDown: 850),
                        new Shoot(10, count: 9, shootAngle: 40, projectileIndex: 1, predictive: 1, coolDown: 1000),
                        new TimedTransition(2500, "kill")
                        ),
                    new State("kill",
                         new Shoot(10, count: 16, projectileIndex: 0, coolDown: 5000),
                        new RemoveEntity(6, "Fun Ferris Wheel"),
                        new TimedTransition(2000, "fight")
                        ),
                    new State("nowheels",
                        new Taunt(1.00, "Eek!"),
                        new Shoot(10, count: 6, shootAngle: 26, projectileIndex: 2, predictive: 2, coolDown: 1000),
                        new Shoot(10, count: 6, shootAngle: 26, projectileIndex: 2, coolDown: 800),
                        new Shoot(10, count: 6, projectileIndex: 1, predictive: 1, coolDown: 500)
                        )
                     ),
                new MostDamagers(3,
                    new ItemLoot("Greater Potion of Speed", 1.0),
                    new ItemLoot("Scroll Identifier", 1.0),
                    new ItemLoot("Unidentified Scroll", 1.0)
                ),
                new Threshold(0.025,
                    new TierLoot(8, ItemType.Weapon, 0.1),
                    new TierLoot(3, ItemType.Ability, 0.1),
                    new TierLoot(8, ItemType.Armor, 0.1),
                    new TierLoot(3, ItemType.Ring, 0.05),
                    new TierLoot(9, ItemType.Armor, 0.05),
                    new TierLoot(9, ItemType.Weapon, 0.05),
                    new ItemLoot("Fun Hat", 0.048),
                    new ItemLoot("Springed Boxing Glove", 0.055)
                )
            );
    }
}
      