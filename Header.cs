using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;

namespace ODB
{
    public enum NoiseType
    {
        FootSteps,
        Combat,
        Door,
        Burp
    }

    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
        Down,
        Up
    }

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

        public void Nudge(int offsetX, int offsetY, int? offsetZ = null)
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
            return
                a.x == b.x &&
                a.y == b.y;
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

        public static Point FromCardinal(Direction cardinal)
        {
            switch (cardinal)
            {
                case Direction.North: return new Point(0, -1);
                case Direction.NorthEast: return new Point(1, -1);
                case Direction.East: return new Point(1, 0);
                case Direction.SouthEast: return new Point(1, 1);
                case Direction.South: return new Point(0, 1);
                case Direction.SouthWest: return new Point(-1, 1);
                case Direction.West: return new Point(-1, 0);
                case Direction.NorthWest: return new Point(-1, -1);
                default: throw new ArgumentException();
            }
        }
        //todo: temporary hacky implementation, assuming a normalized p
        public static Direction ToCardinal(Point p)
        {
            switch(p.y)
            {
                case -1:
                    switch (p.x)
                    {
                        case -1: return Direction.NorthWest;
                        case 0: return Direction.North;
                        case 1: return Direction.NorthEast;
                        default: throw new ArgumentException();
                    }
                case 0:
                    switch (p.x)
                    {
                        case -1: return Direction.West;
                        case 0: throw new ArgumentException();
                        case 1: return Direction.East;
                        default: throw new ArgumentException();
                    }
                case 1:
                    switch (p.x)
                    {
                        case -1: return Direction.SouthWest;
                        case 0: return Direction.South;
                        case 1: return Direction.SouthEast;
                        default: throw new ArgumentException();
                    }
                default: throw new ArgumentException();
            }
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

    [DataContract]
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
                .First(kvp => kvp.Value.ToLower()  == s.ToLower())
                .Key;
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

        [DataMember] public DollSlot Type;
        [DataMember] private int? _item;

        public Item Item
        {
            get
            {
                return _item.HasValue
                    ? Util.GetItemByID(_item.Value)
                    : null;
            }
            set
            {
                _item = value == null
                    ? (int?)null
                    : value.ID;
            }
        }

        public BodyPart() { }
        public BodyPart(DollSlot type, Item item = null)
        {
            Type = type;
            Item = item;
        }
    }

    [DataContract]
    public class Hand : BodyPart
    {
        [DataMember] public bool Wielding;

        public Hand() { }
        public Hand(
            DollSlot type,
            Item item = null
        ) : base(type, item) {
            Wielding = false;
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

            ColorPoints = newColorPoints.OrderBy(c => c.Item1).ToList();
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

    public class Dice
    {
        public int Number;
        public int Faces;
        public int Modifier;

        public Dice(int number, int faces, int mod = 0)
        {
            Number = number;
            Faces = faces;
            Modifier = mod;
        }

        public Dice(string s)
        {
            s = s.ToLower();
            int number = int.Parse(s.Split('d')[0]);
            int sides = int.Parse(s.Split('d')[1]
                .Split(new[]{'-', '+'})[0]
            );

            int mod = 0;
            if(s.Contains("+") || s.Contains("-"))
                mod = s.Contains("+") ?
                    int.Parse(s.Split('+')[1]) :
                    -int.Parse(s.Split('-')[1])
                ;

            Number = number;
            Faces = sides;
            Modifier = mod;
        }

        public int Roll(bool max = false)
        {
            return Util.Roll(Number, Faces, Modifier, max);
        }

        public override string ToString()
        {
            string format =
                "{0}d{1}" +
                (Modifier == 0
                    ? ""
                    : "{2:+#;-#;+0}"
                );

            return string.Format(
                format,
                Number > 1 ? Number + "" : "",
                Faces,
                Modifier
            );
        }
    }

    public class DiceRoll
    {
        public Dice Dice;
        public Dictionary<string, int> Bonus; 
        public Dictionary<string, int> Malus; 

        public DiceRoll()
        {
            Bonus = new Dictionary<string, int>();
            Malus = new Dictionary<string, int>();
        }

        public RollInfo Roll(bool max = false)
        {
            int bonus = Bonus.Sum(kvp => kvp.Value);
            int malus = Malus.Sum(kvp => kvp.Value);
            int roll = Dice.Roll(max);

            return new RollInfo
            {
                Roll = roll,
                Result = roll + bonus - malus,
                DiceRoll = this
            };
        }
    }

    public class RollInfo
    {
        public int Roll;
        public int Result;
        public DiceRoll DiceRoll;

        public void Log(bool verbose = false, Action<string> log = null)
        {
            if (log == null) log = Game.UI.Log;

            string message =
                String.Format("{0}", DiceRoll.Dice
            );

            foreach (KeyValuePair<string, int> kvp in DiceRoll.Bonus)
            {
                if (kvp.Value == 0) continue;

                string format = verbose
                    ? "+({1}:{2:+#;-#;+0})"
                    : "{2:+#;-#;+0}";

                message += string.Format(
                    format,
                    "",
                    kvp.Key,
                    kvp.Value
                );
            }

            foreach (KeyValuePair<string, int> kvp in DiceRoll.Malus)
            {
                if (kvp.Value == 0) continue;

                string format = verbose
                    ? "-({1}:{2:+#;-#;+0})"
                    : "{2:+#;-#;+0}";

                message += string.Format(
                    format,
                    "", //because UI.Log starts at 1
                    kvp.Key,
                    kvp.Value
                );
            }

            message += string.Format(" = {0}. ", Result);

            log(message);
        }
    }
}
