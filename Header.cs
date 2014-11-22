using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

namespace ODB
{
    #region structure
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

    #endregion
}
