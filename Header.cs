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

        public gObject(string s)
        {
            ReadGOBject(s);
        }

        public string WriteGOBject()
        {
            string s = "";
            s += IO.Write(xy);
            s += IO.Write(bg);
            s += IO.Write(fg);
            s += tile;
            s += name + ";";
            return s;
        }

        //return how many characters we read
        //so subclasses know where to start
        public int ReadGOBject(string s)
        {
            int read = 0;
            this.xy = IO.ReadPoint(s, ref read, 0);
            this.bg = IO.ReadNullableColor(s, ref read, read);
            this.fg = IO.ReadColor(s, ref read, read);
            this.tile = s.Substring(read++, 1);
            this.name = IO.ReadString(s, ref read, read);
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

    public class Actor : gObject
    {
        public static int IDCounter = 0;
        public int id;

        public List<Item> inventory;

        public int strength, dexterity, intelligence;
        public int hpMax, hpCurrent;

        public List<BodyPart> PaperDoll;

        public Actor(
            Point xy, Color? bg, Color fg, string tile, string name
        ) :
            base(xy, bg, fg, tile, name)
        {
            id = IDCounter++;
            inventory = new List<Item>();
            PaperDoll = new List<BodyPart>();
        }

        public bool HasFree(DollSlot slot)
        {
            return PaperDoll.Any(
                x => x.Type == slot &&
                x.Item == null
            );
        }

        public void Equip(Item it)
        {
            foreach (DollSlot ds in it.equipSlots)
            {
                foreach(BodyPart bp in PaperDoll)
                    if (bp.Type == ds && bp.Item == null)
                    {
                        bp.Item = it;
                        break;
                    }
            }
        }

        public bool IsEquipped(Item it)
        {
            return PaperDoll.Any(x => x.Item == it);
        }

        public int GetAC()
        {
            int ac = 8;
            List<Item> equipped = new List<Item>();
            foreach (
                BodyPart bp in PaperDoll.FindAll(
                    x =>
                        //might seem dumb, but ds.Hand is currently for
                        //eh, like, the grip, more than the hand itself
                        //glove-hands currently do not exist..?
                        //idk, we'll get to it
                        x.Type != DollSlot.Hand &&
                        x.Item != null
                    )
                )
                if(!equipped.Contains(bp.Item))
                    equipped.Add(bp.Item);

            foreach(Item it in equipped)
                ac += it.AC;

            return ac;
        }

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + dexterity;
            int dodgeRoll = target.GetAC();

            if (hitRoll >= dodgeRoll) {
                int damageRoll = strength;

                foreach (
                    BodyPart bp in PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand && x.Item != null)
                    )
                    if (bp.Item.Damage != "")
                        damageRoll += Util.Roll(bp.Item.Damage);
                    else
                        //barehanded/bash damage
                        damageRoll += Util.Roll("1d4");

                target.hpCurrent -= damageRoll;

                Game.log.Add(name + " strikes " + target.name +
                    " (" + hitRoll + " vs AC" + dodgeRoll + ")" +
                    " (-" + damageRoll + "hp)"
                );

                if (target.hpCurrent <= 0)
                {
                    Game.log.Add(target.name + " dies!");
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
        public static int IDCounter = 0;
        public int id;

        //int count;
        public int AC;
        public string Damage;
        public List<DollSlot> equipSlots;

        public Item(
            Point xy, Color? bg, Color fg, string tile, string name,
            string Damage = "", int AC = 0
        ) :
            base(xy, bg, fg, tile, name)
        {
            id = IDCounter++;
            //count = 1;
            this.AC = AC;
            this.Damage = Damage;
            equipSlots = new List<DollSlot>();
        }

        public Item(string s) : base(s)
        {
            ReadItem(s);
        }

        public string WriteItem()
        {
            string s = base.WriteGOBject();
            s += IO.WriteHex(id, 4);
            s += IO.WriteHex(AC, 2);
            s += IO.Write(Damage);
            foreach (DollSlot ds in equipSlots)
                s += (int)ds + ",";
            s += ";";

            return s;
        }

        public int ReadItem(string s)
        {
            int read = base.ReadGOBject(s);
            id = IO.ReadHex(s, 4, ref read, read);
            AC = IO.ReadHex(s, 2, ref read, read);
            Damage = IO.ReadString(s, ref read, read);

            string slots = IO.ReadString(s, ref read, read);
            equipSlots = new List<DollSlot>();
            foreach (string ss in slots.Split(','))
                if(ss != "")
                    equipSlots.Add((DollSlot)int.Parse(ss));

            return read;
        }
    }
    #endregion
}
