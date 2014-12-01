using System;
using System.Collections.Generic;
using System.Linq;

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
    }

    public class Room
    {
        public List<Rect> Rects;

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
        //Gloves, //maybe?
        Hand,
        Legs,
        Feet,
        //Quiver
    }

    public class BodyPart
    {
        public DollSlot Type;
        public Item Item;
        public BodyPart(DollSlot type, Item item = null)
        {
            Type = type;
            Item = item;
        }
    }

    class Pair<T, TS>
    {
        T _first;
        TS _second;

        public Pair(T a, TS b)
        {
            _first = a;
            _second = b;
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
        public List<Action<Actor, Point>> Effects;

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

        public Spell(
            string name,
            List<Action<Actor, Point>> effects,
            int cost = 0,
            int castDifficulty = 0,
            int range = 0
        ) {
            ID = IDCounter++;
            Name = name;
            Range = range;
            Cost = cost;
            CastDifficulty = castDifficulty;
            Effects = effects;

            Spells[ID] = this;
        }

        public Projectile Cast(Actor caster, Point target)
        {
            Projectile p = new Projectile();
            if(Effects != null)
                p.Effects = new List<Action<Actor, Point>>(Effects);
            p.Caster = caster;
            p.Origin = caster.xy;
            p.Delta = target - caster.xy;
            //don't try to go further than we targeted
            p.Range = Math.Max(
                Math.Abs(p.Delta.x),
                Math.Abs(p.Delta.y)
            );
            //and don't go longer than allowed
            //that should probably be handled somewhere else though
            if (p.Range > Range) p.Range = Range;
            return p;
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
            if (Util.Game.Level.Map[xy.x, xy.y] == null)
            {
                Moved--;
                CalculatePosition();
                Die = true;
            }
            else if (Util.Game.Level.Map[xy.x, xy.y].solid)
            {
                //unmove
                Moved--;
                CalculatePosition();
                Die = true;
            }
            if (Moved >= Range) Die = true;

            if(!Die)
                if (Util.Game.Level.ActorOnTile(xy) != null)
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

    public enum StatusType
    {
        Stun,
        Confusion,
        Sleep
    }

    public class LastingEffect
    {
        public static Dictionary<StatusType, string> EffectNames =
            new Dictionary<StatusType, string>
        {
            { StatusType.Stun, "Stun" },
            { StatusType.Confusion, "Confusion" }
        };

        public StatusType Type;
        //public int Value;
        public int Life;

        public LastingEffect(
            StatusType type,
            int life
        ) {
            Type = type;
            Life = life;
        }

        public LastingEffect(string s)
        {
            ReadLastingEffect(s);
        }

        public void Tick()
        {
            Life--;
        }

        public Stream WriteLastingEffect()
        {
            Stream stream = new Stream();
            stream.Write((int)Type, 4);
            stream.Write(Life, 4);
            return stream;
        }

        public void ReadLastingEffect(string s)
        {
            Stream stream = new Stream(s);
            Type = (StatusType)stream.ReadHex(4);
            Life = stream.ReadHex(4);
        }
    }
}
