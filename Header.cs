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

    //gobj, act, item beneat

    public class gObjectDefinition
    {
        //afaik, atm we won't have anything that's /just/
        // a game object, it'll always be an item/actor
        //but in case.
        public static gObjectDefinition[] Definitions =
            new gObjectDefinition[0xFFFF];
        public static int TypeCounter = 0;

        public Color? bg;
        public Color fg;
        public string tile;
        public string name;
        public int type;

        public gObjectDefinition(
            Color? bg, Color fg, string tile, string name
        ) {
            this.bg = bg;
            this.fg = fg;
            this.tile = tile;
            this.name = name;
            this.type = TypeCounter++;
            Definitions[this.type] = this;
        }

        public gObjectDefinition(string s)
        {
            ReadGObjectDefinition(s);
        }

        public int ReadGObjectDefinition(string s)
        {
            int read = 0;
            type = IO.ReadHex(s, 4, ref read, read);
            bg = IO.ReadNullableColor(s, ref read, read);
            fg = IO.ReadColor(s, ref read, read);
            tile = s.Substring(read++, 1);
            name = IO.ReadString(s, ref read, read);
            Definitions[this.type] = this;
            return read;
        }

        public string WriteGObjectDefinition()
        {
            string output = "";
            output += IO.WriteHex(type, 4);
            output += IO.Write(bg);
            output += IO.Write(fg);
            output += tile;
            output += IO.Write(name);
            return output;
        }
    }

    public class gObject
    {
        public static Game1 Game;

        public Point xy;
        public gObjectDefinition Definition;

        public gObject(
            Point xy, gObjectDefinition def
        ) {
            this.xy = xy;
            this.Definition = def;
        }

        public gObject(string s)
        {
            ReadGOBject(s);
        }

        public string WriteGOBject()
        {
            string s = "";
            s += IO.WriteHex(Definition.type, 4);
            s += IO.Write(xy);
            return s;
        }

        //return how many characters we read
        //so subclasses know where to start
        public int ReadGOBject(string s)
        {
            int read = 0;
            this.Definition = gObjectDefinition.Definitions[
                IO.ReadHex(s, 4, ref read, read)
            ];
            this.xy = IO.ReadPoint(s, ref read, read);
            return read;
        }
    }

    public class ActorDefinition : gObjectDefinition
    //public class ActorDefinition
    {
        public static ActorDefinition[] ActorDefinitions =
            new ActorDefinition[0xFFFF];

        public int strength, dexterity, intelligence, hpMax;
        public List<DollSlot> BodyParts;
        //public ItemDefinition Corpse;
        public int CorpseType;

        public ActorDefinition(
            Color? bg, Color fg,
            string tile, string name,
            int strength, int dexterity, int intelligence, int hp,
            List<DollSlot> BodyParts
        )
        : base(bg, fg, tile, name) {
            this.strength = strength;
            this.dexterity = dexterity;
            this.intelligence = intelligence;
            this.hpMax = hp;
            this.BodyParts = BodyParts;
            ActorDefinitions[this.type] = this;
            ItemDefinition Corpse = new ItemDefinition(
                null, Color.Red, "%", name + " corpse");
            CorpseType = Corpse.type;
        }

        public ActorDefinition(string s) : base(s)
        {
            ReadActorDefinition(s);
        }

        public string WriteActorDefinition()
        {
            string output = WriteGObjectDefinition();
            output += IO.WriteHex(strength, 2);
            output += IO.WriteHex(dexterity, 2);
            output += IO.WriteHex(intelligence, 2);
            output += IO.WriteHex(hpMax, 2);
            foreach (DollSlot ds in BodyParts)
                output += (int)ds + ",";
            output += ";";
            output += IO.WriteHex(CorpseType, 4);
            return output;
        }

        public int ReadActorDefinition(string s)
        {
            int read = ReadGObjectDefinition(s);
            strength = IO.ReadHex(s, 2, ref read, read);
            dexterity = IO.ReadHex(s, 2, ref read, read);
            intelligence = IO.ReadHex(s, 2, ref read, read);
            hpMax = IO.ReadHex(s, 2, ref read, read);

            BodyParts = new List<DollSlot>();
            foreach (string ss in IO.ReadString(s, ref read, read).Split(','))
                if(ss != "")
                    BodyParts.Add((DollSlot)int.Parse(ss));

            CorpseType = IO.ReadHex(s, 4, ref read, read);

            ActorDefinitions[type] = this;
            return read;
        }
    }

    public class Actor : gObject
    {
        public static int IDCounter = 0;
        public int id;

        public new ActorDefinition Definition;

        public int hpCurrent;

        public int Cooldown;

        public List<BodyPart> PaperDoll;
        public List<Item> inventory;

        public Actor(
            Point xy, ActorDefinition def
        )
            : base(xy, def)
        {
            Definition = def;
            id = IDCounter++;
            this.hpCurrent = def.hpMax;
            inventory = new List<Item>();
            PaperDoll = new List<BodyPart>();
            foreach (DollSlot ds in def.BodyParts)
                PaperDoll.Add(new BodyPart(ds));
            Cooldown = 0;
        }

        public Actor(string s)
            : base(s)
        {
            ReadActor(s);
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
            foreach (DollSlot ds in it.Definition.equipSlots)
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
                ac += it.Definition.AC;

            return ac;
        }

        public void Attack(Actor target)
        {
            int hitRoll = Util.Roll("1d6") + Definition.dexterity;
            int dodgeRoll = target.GetAC();

            if (hitRoll >= dodgeRoll) {
                int damageRoll = Definition.strength;

                foreach (
                    BodyPart bp in PaperDoll.FindAll(
                        x => x.Type == DollSlot.Hand && x.Item != null)
                    )
                    if (bp.Item.Definition.Damage != "")
                        damageRoll += Util.Roll(bp.Item.Definition.Damage);
                    else
                        //barehanded/bash damage
                        damageRoll += Util.Roll("1d4");

                target.hpCurrent -= damageRoll;

                Game.log.Add(
                    Definition.name + " strikes " +target.Definition.name +
                    " (" + hitRoll + " vs AC" + dodgeRoll + ")" +
                    " (-" + damageRoll + "hp)"
                );

                if (target.hpCurrent <= 0)
                {
                    Game.log.Add(target.Definition.name + " dies!");
                    Item corpse = new Item(
                        target.xy,
                        ItemDefinition.ItemDefinitions[
                            target.Definition.CorpseType]
                    );
                    Game.worldItems.Add(corpse);
                    Game.allItems.Add(corpse);
                    Game.worldActors.Remove(target);
                }
            }
            else
            {
                Game.log.Add(Definition.name + " swings in the air." +
                    " (" + hitRoll + " vs " + dodgeRoll + ")"
                );
            }
        }

        public string WriteActor()
        {
            string output = base.WriteGOBject();
            output += IO.WriteHex(Definition.type, 4);
            output += IO.WriteHex(id, 4);
            output += IO.WriteHex(hpCurrent, 2);
            output += IO.WriteHex(Cooldown, 2);

            foreach (BodyPart bp in PaperDoll)
            {
                output += IO.WriteHex((int)bp.Type, 2) + ":";

                if (bp.Item == null) output += "XXXX";
                else output += IO.WriteHex(bp.Item.id, 4);

                output += ",";
            }
            output += ";";

            foreach (Item it in inventory)
                output += IO.WriteHex(it.id, 4) + ",";
            output += ";";

            return output;
        }

        public int ReadActor(string s)
        {
            int read = base.ReadGOBject(s);
            Definition =
                ActorDefinition.ActorDefinitions[
                    IO.ReadHex(s, 4, ref read, read)
                ];
            id = IO.ReadHex(s, 4, ref read, read);
            hpCurrent = IO.ReadHex(s, 2, ref read, read);
            Cooldown = IO.ReadHex(s, 2, ref read, read);

            PaperDoll = new List<BodyPart>();
            foreach (string ss in
                IO.ReadString(s, ref read, read).Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                DollSlot type =
                        (DollSlot)IO.ReadHex(ss.Split(':')[0]);
                Item item = 
                        ss.Split(':')[1].Contains("X") ?
                            null :
                            Util.GetItemByID(IO.ReadHex(ss.Split(':')[1]));
                PaperDoll.Add(new BodyPart(type, item));
            }

            inventory = new List<Item>();
            foreach (string ss in
                IO.ReadString(s, ref read, read).Split(
                    new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList()
            ) {
                Item it = Util.GetItemByID(IO.ReadHex(ss));
                inventory.Add(it);
                Game.worldItems.Remove(it);
            }

            return read;
        }
    }

    //definition should hold all data non-specific to the item instance
    //instance specific data is e.g. position, instance id, stack-count
    //definitions should not be saved per level, but are global, so to speak
    //instances, in contrast, might be per level rather than global
    public class ItemDefinition : gObjectDefinition
    {
        public static ItemDefinition[] ItemDefinitions =
            new ItemDefinition[0xFFFF];

        public int AC;
        public string Damage;
        public bool stacking;
        public List<DollSlot> equipSlots;

        //creating a NEW definition
        public ItemDefinition(
            Color? bg, Color fg,
            string tile, string name,
            //item def specific stuff
            string Damage = "", int AC = 0,
            bool stacking = false, List<DollSlot> equipSlots = null)
        : base(bg, fg, tile, name) {
            this.Damage = Damage;
            this.AC = AC;
            this.stacking = stacking;
            this.equipSlots = equipSlots ?? new List<DollSlot>();
            ItemDefinitions[this.type] = this;
        }

        public ItemDefinition(string s) : base(s)
        {
            ReadItemDefinition(s);
        }

        public int ReadItemDefinition(string s)
        {
            int read = ReadGObjectDefinition(s);
            Damage = IO.ReadString(s, ref read, read);
            AC = IO.ReadHex(s, 2, ref read, read);
            stacking = IO.ReadBool(s, ref read, read);

            string slots = IO.ReadString(s, ref read, read);
            equipSlots = new List<DollSlot>();
            foreach (string ss in slots.Split(','))
                if(ss != "")
                    equipSlots.Add((DollSlot)int.Parse(ss));

            ItemDefinitions[type] = this;
            return read;
        }

        public string WriteItemDefinition()
        {
            string output = WriteGObjectDefinition();
            output += IO.Write(Damage);
            output += IO.WriteHex(AC, 2);
            output += IO.Write(stacking);
            foreach (DollSlot ds in equipSlots)
                output += (int)ds + ",";
            return output;
        }
    }

    public class Item : gObject
    {
        public static int IDCounter = 0;

        //instance specifics
        public int id;
        public int count;
        public new ItemDefinition Definition;

        //SPAWNING a NEW item
        public Item(
            Point xy, ItemDefinition def, int count = 1
        ) : base(xy, def) {
            id = IDCounter++;
            this.count = count;
            this.Definition = def;
        }

        //LOADING an OLD item
        public Item(string s) : base(s)
        {
            ReadItem(s);
        }

        public string WriteItem()
        {
            //utilize our definition instead
            string s = base.WriteGOBject();
            s += IO.WriteHex(Definition.type, 4);
            s += IO.WriteHex(id, 4);
            s += IO.WriteHex(count, 2);

            return s;
        }

        public int ReadItem(string s)
        {
            int read = base.ReadGOBject(s);
            Definition =
                ItemDefinition.ItemDefinitions[
                    IO.ReadHex(s, 4, ref read, read)
                ];
            id = IO.ReadHex(s, 4, ref read, read);
            count = IO.ReadHex(s, 2, ref read, read);

            return read;
        }
    }

    #endregion
}
