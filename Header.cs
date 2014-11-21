using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
/*using System.Linq;
using System.Text;*/

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
            s += String.Format("{0:X2}", bg.R);
            s += String.Format("{0:X2}", bg.G);
            s += String.Format("{0:X2}", bg.B);
            s += String.Format("{0:X2}", fg.R);
            s += String.Format("{0:X2}", fg.G);
            s += String.Format("{0:X2}", fg.B);
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
        public static Game1 Game;

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
        public int hpMax, hpCurrent;

        public Dictionary<dollSlot, Item> paperDoll;

        public Actor(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            inventory = new List<Item>();
            paperDoll = new Dictionary<dollSlot, Item>();
        }

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + dexterity;
            int dodgeRoll = Util.Roll("1d6") + dexterity;
            if (hitRoll >= dodgeRoll) {
                //1d4, hardcoded current bare-hands damage
                int damageRoll = Util.Roll("1d4") + strength;
                target.hpCurrent -= damageRoll;

                Game.log.Add(name + " strikes " + target.name +
                    " (" + hitRoll + " vs " + dodgeRoll + ")" +
                    " (-" + damageRoll + "hp)"
                );

                if (target.hpCurrent <= 0)
                {
                    Item corpse = new Item(
                        target.xy, null, target.fg, "%",
                        target.name + " corpse"
                    );
                    Game.items.Add(corpse);
                    Game.actors.Remove(target);
                }
            }
            else
            {
                Game.log.Add(name + " swings in the air." +
                    " (" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }
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
