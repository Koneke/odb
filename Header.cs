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
        //ReSharper disable once InconsistentNaming
        public int? z;
        
        public Point(int x, int y, int? z = null) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Nudge(int offsetX, int offsetY, int? offsetZ = null)
        {
            x += offsetX;
            y += offsetY;
            if (z.HasValue && offsetZ.HasValue)
                z += offsetZ;
        }
        public void Nudge(Point p)
        {
            this += p;
        }

        public static bool operator ==(Point a, Point b)
        {
            return
                a.x == b.x &&
                a.y == b.y &&
                a.z == b.z;
        }
        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Point operator /(Point a, int b)
        {
            return new Point(a.x / b, a.y / b, a.z / b);
        }
        public static Point operator /(Point a, Point b)
        {
            return new Point(a.x / b.x, a.y / b.y, a.z / b.z);
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

    public class Monster
    {
        public enum GenerationType
        {
            Normal,
            Unique
        }

        public static GenerationType ReadGenerationType(string s)
        {
            switch (s.ToLower())
            {
                case "g_normal": return GenerationType.Normal;
                case "g_unique": return GenerationType.Unique;
                default: throw new ArgumentException();
            }
        }

        public static string WriteGenerationType(GenerationType gt)
        {
            switch (gt)
            {
                case GenerationType.Normal: return "g_normal";
                case GenerationType.Unique: return "g_unique";
                default: throw new ArgumentException();
            }
        }

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

        private ColorString() { }

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

    public class Command
    {
        public string Type;
        private readonly Dictionary<string, object> _data;

        public string Answer;
        public Point Target;

        public Command(string type)
        {
            Type = type.ToLower();
            _data = new Dictionary<string, object>();
        }

        public Command Add(string key, object value)
        {
            //restricted
            if (key.ToLower() == "target" || key.ToLower() == "answer")
                throw new Exception();

            _data.Add(key.ToLower(), value);

            //so we can do nice inlining stuff for readability
            return this;
        }

        public object Get(string key)
        {
            key = key.ToLower();

            switch (key)
            {
                case "target":
                    return Target;
                case "answer":
                    return Answer;
                default:
                    return _data[key];
            }
        }

        public bool Has(string key)
        {
            return _data.ContainsKey(key.ToLower());
        }
    }
}
