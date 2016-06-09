using wServer.logic.behaviors;
using wServer.logic.transitions;
using wServer.logic.loot;

namespace wServer.logic
{
    partial class BehaviorDb
    {
        private _ FHUB = () => Behav()
            .Init("AH Ozar, the Destined",
                new State(
                    new ConditionalEffect(ConditionEffectIndex.Invincible),
                    new ChatTransition("talk5", "skip", "Skip"),
                    new State("default",
                        new PlayerWithinTransition(6, "talker")
                        ),
                    new State("talker",
                        new Taunt("You wish to take a journey?"),
                        new ChatTransition("talk", "Yes", "yes")
                        ),
                    new State("talk",
                        new Taunt("The Stronghold imprisoned 4 stone golems in its walls."),
                        new TimedTransition(6400, "talk1")
                        ),
                    new State("talk1",
                        new Taunt("3 Stone golems were servants to their almighty king, Aldragine. They guarded the treasure and would kill anyone that meant to take their power."),
                        new TimedTransition(7000, "talk2")
                        ),
                     new State("talk2",
                        new Taunt("They were no threat to us..until the reason why they existed became corrupted by a dark force called the Zol."),
                        new TimedTransition(7200, "talk3")
                        ),
                     new State("talk3",
                        new Taunt("Aldragine was the main host of this dark force and the Zol devoured his judgement and turned it into hate. The servants were also effected but wasn't as destructive as Aldragine's consumption by the Zol."),
                        new TimedTransition(7200, "talk4")
                        ),
                     new State("talk4",
                        new Taunt("Now, Aldragine schemes in his hideout with plans to spread this parasite to the world."),
                        new TimedTransition(7200, "talk5")
                        ),
                     new State("talk5",
                        new Flash(0xFFFFFF, 1, 1),
                        new Taunt("You must stop this before it is too late...", "Destroy him before he reigns terror across our land!"),
                        new TimedTransition(7200, "bye")
                        ),
                    new State("bye",
                        new Suicide()
                        )
                    )
            )    
        
        .Init("FHUB AH Taskmaster",
            new State(
                new DropPortalOnDeath("Aldragine's Hideout Portal", 100, PortalDespawnTimeSec: 180),
                new ConditionalEffect(ConditionEffectIndex.Invincible),
                new State("idle",
                     new EntitiesNotExistsTransition(9999, "activate", "AH Ozar, the Destined")
                    ),
                new State("activate",
                     new Suicide()
                    )
                )
            )
        ;
    }
}