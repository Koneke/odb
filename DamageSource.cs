using System.Collections.Generic;

namespace ODB
{
    //HOW the damage is dealt
    public enum AttackType
    {
        Slash,
        Pierce,
        Bash,
        Bite,
        Magic
    }

    //WHAT KIND of damage is dealt
    public enum DamageType
    {
        Physical,
        Ratking
    }

    public class DamageSource
    {
        public DamageSource(string killMessage = null)
        {
            KillMessage = killMessage ??
                "R.I.P. {0}, killed by {1} on dungeon level {2}.";
        }

        public Point Position;
        public string KillMessage;
        public int Damage;
        public AttackType AttackType;
        public DamageType DamageType;
        public Actor Source, Target;

        public string GenerateKillMessage()
        {
            return string.Format(
                KillMessage,
                Target == null ? "" : Target.GetName("Name", true),
                Source == null ? "" : Source.GetName("a"),
                Position.z.HasValue ? (Position.z.Value + "") : "X"
            );
        }
    }

    public class AttackMessage
    {
        public static Dictionary<AttackType, List<AttackMessage>> AttackMessages
            = new Dictionary<AttackType, List<AttackMessage>>
        {
            { AttackType.Bash,
                new List<AttackMessage> {
                    new AttackMessage(
                        "#actor #verb #target with #gen #weapon",
                        "bash"),
                    new AttackMessage(
                        "#actor #verb all over #target " +
                        "with #gen #weapon",
                        "wail"),
                    new AttackMessage(
                        "#target get#pass-s #verb-pass with #genname #weapon",
                        "smack")
                }
            },
            { AttackType.Slash,
                new List<AttackMessage> {
                    new AttackMessage(
                        "#actor #verb #target with #gen #weapon", "slash")
                }
            },
            { AttackType.Pierce,
                new List<AttackMessage> {
                    new AttackMessage(
                        "#actor #verb #target with #gen #weapon", "pierce"),
                    new AttackMessage(
                        "#actor #verb #gen #weapon right into #target", "jam")
                }
            },
            { AttackType.Bite,
                new List<AttackMessage> {
                    new AttackMessage(
                        "#actor #verb #target", "bite"),
                    new AttackMessage(
                        "#actor #verb on #target", "chew")
                }
            },
        };

        public string Format;
        public string Verb;

        public AttackMessage(string format, string verb)
        {
            Format = format;
            Verb = verb;
        }

        public string Instantiate(
            Actor actor,
            Actor target,
            Item weapon
        ) {
            string result = Format
                .Replace("#actor", actor.GetName("name"))
                .Replace("#target", target.GetName("name"))
                .Replace("#weapon", weapon == null
                    ? "fists"
                    : weapon.GetName("name"))
                .Replace("#verb-pass", actor.Verb(Verb, Actor.Tempus.Passive))
                .Replace("#verb", actor.Verb(Verb))
                .Replace("#genname", actor.Genitive("name"))
                .Replace("#gen", actor.Genitive())
                .Replace("#pass-s", target == Game.Player ? "" : "s")
                .Replace("#s", actor == Game.Player ? "" : "s");

            return Util.Capitalize(result);
        }
    }
}