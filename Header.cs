using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

namespace ODB
{
    public class Tile
    {
        public Color bg, fg;
        public string tile;

        public bool solid;
        public Door doorState;

        public Tile(
            Color bg,
            Color fg,
            string tile,
            bool solid = false,
            Door doorState = Door.None
        ) {
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
            this.solid = solid;
            this.doorState = doorState;
        }

        public Tile(string s)
        {
            readTile(s);
        }

        public string writeTile()
        {
            string s = "";
            s += IO.Write(bg);
            s += IO.Write(fg);
            s += tile;
            s += solid ? "1" : "0";
            s += doorState == Door.None ?
                "0" : (doorState == Door.Open ?
                    "1" : "2"
            );
            return s;
        }

        public void readTile(string s)
        {
            bg = new Color(
                Int32.Parse(s.Substring(0, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(2, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(4, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            fg = new Color(
                Int32.Parse(s.Substring(6, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(8, 2),
                    System.Globalization.NumberStyles.HexNumber),
                Int32.Parse(s.Substring(10, 2),
                    System.Globalization.NumberStyles.HexNumber)
            );
            tile = s.Substring(12, 1);
            solid = s.Substring(13, 1) == "1";
            switch (s.Substring(14, 1))
            {
                case "0":
                    doorState = Door.None; break;
                case "1":
                    doorState = Door.Open; break;
                case "2":
                    doorState = Door.Closed; break;
                default:
                    throw new Exception("Badly formatted tile.");
            }
            return;
        }
    }

    public enum Door
    {
        None,
        Open,
        Closed
    }

    public struct Point
    {
        public int x, y;
        
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Nudge(int x, int y)
        {
            this.x += x;
            this.y += y;
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
        public Point xy, wh;

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
        public List<Rect> rects;

        public Room()
        {
            rects = new List<Rect>();
        }

        public Room(string s)
        {
            ReadRoom(s);
        }

        public bool ContainsPoint(Point p)
        {
            foreach (Rect r in rects)
            {
                if (r.ContainsPoint(p)) return true;
            }
            return false;
        }

        public string WriteRoom()
        {
            string output = "";
            output += IO.WriteHex(rects.Count, 2);
            foreach (Rect r in rects)
            {
                output += IO.Write(r.xy);
                output += IO.Write(r.wh);
            }
            return output;
        }

        public int ReadRoom(string s)
        {
            int read = 0;
            int numRects = IO.ReadHex(s, 2, ref read, read);
            rects = new List<Rect>();
            for (int i = 0; i < numRects; i++)
            {
                rects.Add(new Rect(
                    IO.ReadPoint(s, ref read, read),
                    IO.ReadPoint(s, ref read, read)
                    )
                );
            }
            return read;
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
        Feet
    }

    public class BodyPart
    {
        public DollSlot Type;
        public Item Item;
        public BodyPart(DollSlot Type, Item Item = null)
        {
            this.Type = Type;
            this.Item = Item;
        }
    }

    class Pair<T, S>
    {
        T first;
        S second;

        public Pair(T a, S b)
        {
            first = a;
            second = b;
        }
    }

    public enum ModType
    {
        AddStr, DecStr,
        AddDex, DecDex,
        AddInt, DecInt,
        AddSpd, DecSpd,
        AddQck, DecQck,
        AddHPm, DecHPm
    }

    public class Mod {
        public ModType Type;
        public int Value;
        public Mod(ModType Type, int Value)
        {
            this.Type = Type;
            this.Value = Value;
        }
    }

    public class Spell
    {
        //0 should mean self-cast..?
        //projectile should explode without moving, so should be on self
        public int Range;
        //note: low = better here, amt of ticks, not amt of reduction
        //0 SHOULD mean instant
        public int Speed;
        public int CastDifficulty;
        public List<Action<Point>> Effects;

        public Spell()
        {
            Range = 3;
            Speed = 0;
            CastDifficulty = 0;

            Effects = new List<Action<Point>>();
            Effects.Add(
                delegate(Point p) {
                    //like with the player attack code, should probably
                    //not be foreach. in its defense though, there should only
                    //ever be one actor per tile, atleast atm
                    foreach (Actor a in Util.ActorsOnTile(p))
                    {
                        Util.Game.log.Add(a.Definition.name +
                            " is hit by the bolt!"
                        );
                        a.Damage(Util.Roll("1d4"));
                    }
                }
            );
        }

        public Projectile Cast(Actor caster, Point target)
        {
            Projectile p = new Projectile();
            p.Effects = new List<Action<Point>>(Effects);
            p.origin = caster.xy;
            p.Delta = target - caster.xy;
            //don't try to go further than we targeted
            p.Range = Math.Max(
                Math.Abs(p.Delta.x),
                Math.Abs(p.Delta.y)
            );
            return p;
        }
    }

    //should probably make this a gObj?
    public class Projectile
    {
        public int Range;
        public int Moved;
        public bool Die;
        public Point xy;
        public Point origin;
        public Point Delta;
        public List<Action<Point>> Effects;

        void calculatePosition()
        {
            float deltaRatio;
            int offs;

            int x, y;

            if(Delta.x == 0) {
                xy = new Point(origin.x, origin.y + Math.Sign(Delta.y) * Moved);
                return;
            }
            else if(Delta.y == 0) {
                xy = new Point(origin.x + Math.Sign(Delta.x) * Moved, origin.y);
                return;
            }

            if (Delta.x > Delta.y)
            {
                deltaRatio = (float)Math.Abs(Delta.x) / Math.Abs(Delta.y);
                offs = (int)Math.Floor(deltaRatio / 2);

                x = Moved + offs;
                y = (int)((x - (x % deltaRatio)) / deltaRatio);

                xy = origin + new Point(
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

                xy = origin + new Point(
                    Math.Sign(Delta.x) * x,
                    Math.Sign(Delta.y) * Moved
                );
            }
            else
            {
                xy = origin + new Point(
                    Math.Sign(Delta.x) * Moved,
                    Math.Sign(Delta.y) * Moved
                );
            }
        }

        public void Move()
        {
            calculatePosition();

            if (Moved < Range) {
                Moved++;
                calculatePosition();
            }
            if (Util.Game.map[xy.x, xy.y] == null)
            {
                Moved--;
                calculatePosition();
                Die = true;
            }
            else if (Util.Game.map[xy.x, xy.y].solid)
            {
                //unmove
                Moved--;
                calculatePosition();
                Die = true;
            }
            if (Moved >= Range) Die = true;

            if(!Die)
                if (Util.ActorsOnTile(xy).Count > 0)
                    Die = true; //explode without unmoving

            if (Die)
                foreach (Action<Point> effect in Effects)
                    effect(xy);

            if (!Die) Move();
        }
    }
}
