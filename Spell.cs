using System;
using System.Collections.Generic;
using System.Linq;

namespace ODB
{
    public enum SpellID
    {
        Spell_PotionOfHealing,
        Spell_Forcebolt,
        Spell_Identify
    }

    public class Spell
    {
        public static Dictionary<SpellID, Spell> SpellDict =
            new Dictionary<SpellID, Spell>(); 

        public string Name;
        public SpellID SpellID;
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
        public Spell(SpellID id)
        {
            SpellID = id;
            SpellDict.Add(id, this);
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
            new Spell(SpellID.Spell_PotionOfHealing)
            {
                Name = "potion of healing",
                CastType = InputType.None,
                Effect = (caster, target) =>
                {
                    Game.UI.Log(
                        "{1} {2} better!",
                        caster.GetName("Name"),
                        caster.Verb("#feel")
                    );
                    caster.Heal(Util.Roll("3d3"));
                }
            };

            //ReSharper disable once ObjectCreationAsStatement
            new Spell(SpellID.Spell_Forcebolt)
            {
                Name = "forcebolt",
                CastType = InputType.Targeting,
                Effect = (caster, target) =>
                {
                    Actor targetActor = World.Level.ActorOnTile((Point)target);

                    if (targetActor == null)
                    {
                        Game.UI.Log("The bolt fizzles in the air.");
                        return;
                    }
                    Game.UI.Log("The forcebolt hits {1}.",
                        targetActor.GetName("the")
                    );

                    DamageSource ds = new DamageSource
                    {
                        Damage = Util.Roll("2d4"),
                        AttackType = AttackType.Magic,
                        DamageType = DamageType.Physical,
                        Source = caster,
                        Target = targetActor
                    };
                    targetActor.Damage(ds);
                },
                CastDifficulty = 10,
                Cost = 3,
                Range = 5
            };

            new Spell(SpellID.Spell_Identify)
            {
                Name = "identify",
                CastType = InputType.QuestionPromptSingle,
                SetupAcceptedInput = (caster) =>
                {
                    Item readItem = null;
                    if(IO.CurrentCommand.Has("item"))
                        readItem = (Item)IO.CurrentCommand.Get("item");

                    IO.AcceptedInput.Clear();
                    IO.AcceptedInput.AddRange(
                        caster.Inventory
                            .Where(it => !it.Known)
                            //no casting from a scroll unto the scroll itself
                            .Where(it => it != readItem)
                            .Select(item => caster.Inventory.IndexOf(item))
                            .Select(index => IO.Indexes[index])
                    );

                },
                Effect = (caster, target) =>
                {
                    string answer = (string)target;
                    int index = IO.Indexes.IndexOf(answer[0]);
                    Item item = caster.Inventory[index];
                    item.Identify();
                },
                CastDifficulty = 15,
                Cost = 7
            };
        }
    }
}