using System;
using System.Linq;

namespace ODB
{
    public class Spell
    {
        public static Spell[] Spells = new Spell[0xFFFF];
        public static int IDCounter = 0;
        public int ID;

        public string Name;
        //0 should mean self-cast..? (or just non-targetted)
        //projectile should explode without moving, so should be on self
        public int Range;
        public int Cost;
        public int CastDifficulty;

        //LH-021214: Spelleffects use the QpStack like other question-reactions.
        //           Also means that we can have spells going through several
        //           questions, like multi-targetting and similar, the same way
        //           dropping a certain number of things works right now.
        public Action<Actor, object> Effect;

        //LH-021214: Since we're using the standard question system we will at
        //           times need to populate the accepted input, so we need an
        //           action for that as well.

        public Action<Actor> SetupAcceptedInput;

        //LH-021214: Add variable to keep the questio string as well?
        //           Like, identify might have "Identify what?", instead of
        //           the automatic "Casting identify".
        public InputType CastType;

        //LH-011214: (Almost) empty constructor to be used with initalizer
        //           blocks. Using them simply because it is easier to skim
        //           quickly if you have both the value and what it actually is
        //           (i.e. castcost or what not).
        public Spell(string name)
        {
            Name = name;

            ID = IDCounter++;
            Spells[ID] = this;
        }

        public void Cast(Actor caster, object target)
        {
            //target is currently usually either a string or a point,
            //but if we need to, we really could pass an entire Command in.
            
            Effect(caster, target);
        }

        public static void SetupMagic()
        {
            //ReSharper disable once ObjectCreationAsStatement
            new Spell("potion of healing")
            {
                CastType = InputType.None,
                Effect = (c, t) =>
                {
                    ODBGame.Game.UI.Log(
                        ODBGame.Game.Caster.GetName("Name") + " " +
                        ODBGame.Game.Caster.Verb("#feel") + " " +
                        "better!"
                    );
                    Util.Game.Caster.Heal(Util.Roll("3d3"));
                }
            };

            //ReSharper disable once ObjectCreationAsStatement
            new Spell("forcebolt")
            {
                CastType = InputType.Targeting,
                Effect = (caster, target) =>
                {
                    //Actor target = World.Level.ActorOnTile(Game.Target);
                    Actor targetActor = World.Level.ActorOnTile(
                        (Point)target
                    );

                    if (targetActor == null)
                    {
                        ODBGame.Game.UI.Log("The bolt fizzles in the air.");
                        return;
                    }
                    ODBGame.Game.UI.Log("The forcebolt hits {1}.",
                        targetActor.GetName("the"));
                    DamageSource ds = new DamageSource
                    {
                        Damage = Util.Roll("2d4"),
                        AttackType = AttackType.Magic,
                        DamageType = DamageType.Physical,
                        Source = ODBGame.Game.Caster,
                        Target = targetActor
                    };
                    targetActor.Damage(ds);
                },
                CastDifficulty = 10,
                Cost = 3,
                Range = 5
            };

            new Spell("identify")
            {
                CastType = InputType.QuestionPromptSingle,
                SetupAcceptedInput = (caster) =>
                {
                    IO.AcceptedInput.Clear();
                    IO.AcceptedInput.AddRange(
                        ODBGame.Game.Caster.Inventory
                            .Where(it => !it.Known)
                            .Select(item => ODBGame.Game.Caster.Inventory
                                .IndexOf(item))
                            .Select(index => IO.Indexes[index])
                    );

                },
                Effect = (caster, target) =>
                {
                    string answer = (string)target;
                    int index = IO.Indexes.IndexOf(answer[0]);
                    Item item = ODBGame.Game.Caster.Inventory[index];
                    item.Identify();
                },
                CastDifficulty = 15,
                Cost = 7
            };
        }
    }
}