using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ODB
{
    public struct Point
    {
        public bool Equals(Point other)
        {
            return x == other.x && y == other.y;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point && Equals((Point) obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (x*397) ^ y;
            }
        }

        //Keeping these lowercase simply because they are used so frequently
        //ReSharper disable once InconsistentNaming
        public int x;
        //ReSharper disable once InconsistentNaming
        public int y;
        
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Nudge(int offsetX, int offsetY)
        {
            x += offsetX;
            y += offsetY;
        }
        public void Nudge(Point p)
        {
            this += p;
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y);
        }
        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y);
        }
        public static Point operator /(Point a, int b)
        {
            return new Point(a.x / b, a.y / b);
        }
        public static Point operator /(Point a, Point b)
        {
            return new Point(a.x / b.x, a.y / b.y);
        }
    }

    public struct Rect
    {
        //disabled for same reason as in point, used so frequently
        //ReSharper disable once InconsistentNaming
        public Point xy;
        //ReSharper disable once InconsistentNaming
        public Point wh;

        public Rect(Point xy, Point wh)
        {
            this.xy = xy;
            this.wh = wh;
        }

        public bool ContainsPoint(Point p)
        {
            return
                p.x >= xy.x &&
                p.y >= xy.y &&
                p.x < xy.x + wh.x &&
                p.y < xy.y + wh.y;
        }

        public bool Interects(Rect other, int border = 0)
        {
            if (other.xy.x > xy.x + wh.x - border) return false;
            if (other.xy.y > xy.y + wh.y - border) return false;
            if (other.xy.x + other.wh.x < xy.x + border) return false;
            if (other.xy.y + other.wh.y < xy.y + border) return false;
            return true;
        }

        public bool Contains(Rect other)
        {
            return
                other.xy.x >= xy.x &&
                other.xy.x + other.wh.x <= xy.x + wh.x &&
                other.xy.y >= xy.y &&
                other.xy.y + other.wh.y <= xy.y + wh.y;
        }
    }

    public class Room
    {
        public List<Rect> Rects;
        public Level Level;

        //should not be saved to file
        public List<Room> Linked;

        public Room()
        {
            Rects = new List<Rect>();
            Linked = new List<Room>();
        }

        public Room(string s)
        {
            ReadRoom(s);
            Linked = new List<Room>();
        }

        public bool ContainsPoint(Point p)
        {
            return Rects.Any(r => r.ContainsPoint(p));
        }

        public List<Tile> GetTiles()
        {
            List<Tile> tiles = new List<Tile>();
            foreach (Rect rect in Rects)
                for (int x = 0; x < rect.wh.x; x++)
                for (int y = 0; y < rect.wh.y; y++)
                tiles.Add(Level.At(rect.xy + new Point(x, y)).Tile);
            return tiles;
        }

        //should probably switch to stream for ease
        public string WriteRoom()
        {
            string output = "";
            output += IO.WriteHex(Rects.Count, 2);
            foreach (Rect r in Rects)
            {
                output += IO.Write(r.xy);
                output += IO.Write(r.wh);
            }
            return output;
        }
        public Stream ReadRoom(string s)
        {
            Stream stream = new Stream(s);
            int numRects = stream.ReadHex(2);
            Rects = new List<Rect>();
            for (int i = 0; i < numRects; i++)
            {
                Rects.Add(new Rect(
                    stream.ReadPoint(),
                    stream.ReadPoint()
                    )
                );
            }
            return stream;
        }
    }

    public enum DollSlot
    {
        Head,
        //Eyes,
        //Face,
        //Neck,
        Torso,
        Back,
        //Gloves, //maybe?
        Hand,
        Legs,
        Feet,
    }

    public class BodyPart
    {
        public static Dictionary<DollSlot, string> BodyPartNames =
            new Dictionary<DollSlot, string>
            {
                { DollSlot.Head, "Head" },
                { DollSlot.Torso, "Torso" },
                { DollSlot.Back, "Back" },
                { DollSlot.Hand, "Hand" },
                { DollSlot.Legs, "Legs" },
                { DollSlot.Feet, "Feet" },
            };
        public static DollSlot ReadDollSlot(string s)
        {
            return BodyPartNames
                .First(kvp => kvp.Value.ToLower()  == s.ToLower()).Key;
        }
        public static string WriteDollSlot(DollSlot ds)
            { return BodyPartNames[ds]; }

        protected bool Equals(BodyPart other)
        {
            return Type == other.Type && Equals(Item, other.Item);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type*397) ^
                       (Item != null ? Item.GetHashCode() : 0);
            }
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BodyPart)obj);
        }

        public DollSlot Type;
        public Item Item;
        public BodyPart(DollSlot type, Item item = null)
        {
            Type = type;
            Item = item;
        }
    }

    public enum ModType
    {
        Strength,
        Dexterity,
        Intelligence,
        Speed,
        Quickness,
        HpMax,
        PoisonRes
    }

    public class Mod {
        public ModType Type;
        public int RawValue;
        public Mod(ModType type, int rawValue)
        {
            Type = type;
            RawValue = rawValue;
        }

        public int GetValue()
        {
            //decimal 64 = 0
            //lower values are negative mods
            //higher positive ones
            return RawValue - 0x40;
        }
    }

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
        public Action Effect;

        //LH-021214: Since we're using the standard question system we will at
        //           times need to populate the accepted input, so we need an
        //           action for that as well.

        public Action SetupAcceptedInput;

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

        public void Cast()
        {
            Actor caster = Util.Game.Caster;
            Util.Game.Caster.MpCurrent -= Cost;
            if (Util.Roll("1d20") + caster.Get(Stat.Intelligence) >=
                CastDifficulty)
                Effect();
            else
                Util.Game.UI.Log(
                    caster.GetName("Name") + " " +
                    caster.Verb("whiff") + " " +
                    "the spell!"
                );
            Util.Game.Caster.Pass();
            Util.Game.Caster = null;
        }
    }

    //should probably make this a gObj?
    public class Projectile
    {
        public int Range;
        public int Moved;
        public bool Die;
        //ReSharper disable once InconsistentNaming
        public Point xy;
        public Point Origin;
        public Point Delta;
        public List<Action<Actor, Point>> Effects;
        public Actor Caster;

        private void CalculatePosition()
        {
            float deltaRatio;
            int offs;

            int x, y;

            if(Delta.x == 0) {
                xy = new Point(
                    Origin.x,
                    Origin.y + Math.Sign(Delta.y) * Moved
                );
                return;
            }
            //ReSharper disable once RedundantIfElseBlock
            //LH-01214: but it isn't redundant..?
            else if(Delta.y == 0) {
                xy = new Point(
                    Origin.x + Math.Sign(Delta.x) * Moved,
                    Origin.y
                );
                return;
            }

            if (Delta.x > Delta.y)
            {
                deltaRatio = (float)Math.Abs(Delta.x) / Math.Abs(Delta.y);
                offs = (int)Math.Floor(deltaRatio / 2);

                x = Moved + offs;
                y = (int)((x - (x % deltaRatio)) / deltaRatio);

                xy = Origin + new Point(
                    Math.Sign(Delta.x) * Moved,
                    Math.Sign(Delta.y) * y
                );
            }
            else if (Delta.y > Delta.x)
            {
                deltaRatio = (float)Math.Abs(Delta.y) / Math.Abs(Delta.x);
                offs = (int)Math.Floor(deltaRatio / 2);

                y = Moved + offs;
                x = (int)((y - (y % deltaRatio)) / deltaRatio);

                xy = Origin + new Point(
                    Math.Sign(Delta.x) * x,
                    Math.Sign(Delta.y) * Moved
                );
            }
            else
            {
                xy = Origin + new Point(
                    Math.Sign(Delta.x) * Moved,
                    Math.Sign(Delta.y) * Moved
                );
            }
        }

        public void Move()
        {
            CalculatePosition();

            if (Moved < Range) {
                Moved++;
                CalculatePosition();
            }
            if (World.Level.At(xy) == null)
            {
                Moved--;
                CalculatePosition();
                Die = true;
            }
            else if (World.Level.At(xy).Solid)
            {
                //unmove
                Moved--;
                CalculatePosition();
                Die = true;
            }
            if (Moved >= Range) Die = true;

            if(!Die)
                if (World.Level.ActorOnTile(xy) != null)
                    Die = true; //explode without unmoving

            if (Die)
                foreach(Action<Actor, Point> effect in Effects)
                    effect(Caster, xy);

            //LH-011214: Intended recursion. Kills itself when it either hits
            //           a wall or an actor (or runs out of range).
            //           Since we're not bothering with projectile speed atm,
            //           we just go 'til we can't.
            if (!Die) Move();
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
                .Replace("#pass-s", target == Util.Game.Player ? "" : "s")
                .Replace("#s", actor == Util.Game.Player ? "" : "s")
            ;

            return Util.Capitalize(result);
        }
    }

    public enum AttackType
    {
        Slash,
        Pierce,
        Bash,
        Bite
    }

    public enum DamageType
    {
        Physical,
        Ratking
    }

    public class Monster
    {
        public static Dictionary<int, List<ActorDefinition>>
            MonstersByDifficulty = new Dictionary<int, List<ActorDefinition>>();
    }

    public class World
    {
        public static Level Level;
        public static List<Level> Levels = new List<Level>(); 
        public static List<Item> AllItems = new List<Item>();
        public static List<Item> WorldItems = new List<Item>();
        public static List<Actor> WorldActors = new List<Actor>();
    }

    public class ColorString
    {
        public string String;
        public List<Tuple<int, Color>> ColorPoints;

        private ColorString()
        {
        }

        public ColorString(string s)
        {
            ColorPoints = new List<Tuple<int, Color>>();

            int p;
            while ((p = s.IndexOf('#')) != -1)
            {
                Color color = IO.ReadColor(s.Substring(p+1, 6));
                s = s.Remove(p, 7);
                ColorPoints.Add(new Tuple<int, Color>(p, color));
            }

            String = s;
        }

        public void SubString(int index, int length)
        {
            if (index + length > String.Length)
                length = String.Length - index;

            String = String.Substring(index, length);
            ColorPoints.RemoveAll(
                cp =>
                    cp.Item1 < index ||
                    cp.Item1 >= index + length
            );

            List<Tuple<int, Color>> newColorPoints =
                new List<Tuple<int, Color>>();

            foreach (Tuple<int, Color> cp in ColorPoints)
            {
                newColorPoints.Add(new Tuple<int, Color>(
                    cp.Item1 - index, cp.Item2));
            }

            ColorPoints = newColorPoints;
        }

        public ColorString Clone()
        {
            return new ColorString
            {
                String = String,
                ColorPoints = new List<Tuple<int, Color>>(ColorPoints)
            };
        }
    }

    public class DamageSource
    {
        public int Damage;
        public AttackType AttackType;
        public DamageType DamageType;
        public Actor Source, Target;
    }
}
