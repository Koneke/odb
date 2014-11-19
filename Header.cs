using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
/*using System.Linq;
using System.Text;*/

namespace ODB
{
    #region structure
    class Tile
    {
        public Color bg, fg;
        public string tile;

        public Tile(Color bg, Color fg, string tile)
        {
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
        }
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

    class Room
    {
        public List<Rect> rects;

        public Room()
        {
            rects = new List<Rect>();
        }

        public bool ContainsPoint(Point p)
        {
            foreach (Rect r in rects)
            {
                if (r.ContainsPoint(p)) return true;
            }
            return false;
        }
    }

    public class gObject
    {
        public Point xy;
        public Color? bg;
        public Color fg;
        public string tile;
        public string name;

        public gObject(
            Point xy, Color? bg, Color fg, string tile, string name
        ) {
            this.xy = xy;
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
            this.name = name;
        }
    }

    public enum dollSlot
    {
        Head,
        Eyes,
        Face,
        Neck,
        Torso,
        Hand,
        Offhand,
        Legs,
        Feet
    }

    public class Actor : gObject
    {
        public List<Item> inventory;

        public int strength, dexterity, intelligence;

        public Dictionary<dollSlot, Item> paperDoll;

        public Actor(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            inventory = new List<Item>();
            paperDoll = new Dictionary<dollSlot, Item>();
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

    public class Item : gObject
    {
        int count;
        public List<dollSlot> equipSlots;

        public Item(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            count = 1;
            equipSlots = new List<dollSlot>();
        }
    }
    #endregion
}
